namespace GlassBridge.Internal.HID;

using HidSharp;
using Microsoft.Extensions.Logging;

/// <summary>
/// HidSharpの薄いラッパー（デバイス非依存、VID/PIDは呼び出し時に指定）
/// </summary>
internal sealed class HidStreamProvider : IHidStreamProvider
{
    private static readonly ILogger<HidStreamProvider> _logger 
        = LoggerFactoryProvider.Instance.CreateLogger<HidStreamProvider>();

    private readonly List<IHidStream> _streams = [];
    private bool _disposed;

    public HidStreamProvider()
    {
    }

    public async Task<IReadOnlyList<IHidStream>> GetStreamsAsync(
        int vendorId,
        int[] productIds,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(HidStreamProvider));

        if (productIds.Length == 0)
            throw new ArgumentException("At least one product ID must be specified.", nameof(productIds));

        _logger.LogDebug("Searching for HID devices: VID={VendorId:X4}, PIDs=[{ProductIds}]", 
            vendorId, string.Join(", ", productIds.Select(p => $"{p:X4}")));

        var result = new List<IHidStream>();

        foreach (var productId in productIds)
        {
            foreach (var device in DeviceList.Local.GetHidDevices(vendorId, productId))
            {
                try
                {
                    _logger.LogDebug("Found HID device: {DevicePath}, MaxInput={MaxInput}, MaxOutput={MaxOutput}", 
                        device.DevicePath,
                        device.GetMaxInputReportLength(),
                        device.GetMaxOutputReportLength());
                    var stream = device.Open();
                    if (stream != null)
                    {
                        var hidStream = new RealHidStream(stream, device);
                        _streams.Add(hidStream);
                        result.Add(hidStream);
                        _logger.LogDebug("Successfully opened HID device: {DevicePath}", device.DevicePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to open HID device (VID: {VendorId:X4}, PID: {ProductId:X4}): {ErrorMessage}", 
                        vendorId, productId, ex.Message);
                }
            }
        }

        _logger.LogInformation("Found {StreamCount} HID streams", result.Count);
        return result;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _logger.LogDebug("Disposing HidStreamProvider with {StreamCount} streams", _streams.Count);

        foreach (var stream in _streams)
        {
            stream?.Dispose();
        }

        _streams.Clear();
        _disposed = true;
        
        _logger.LogDebug("HidStreamProvider disposed");
    }
}
