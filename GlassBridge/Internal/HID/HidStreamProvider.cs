namespace GlassBridge.Internal.HID;

using GlassBridge;
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

        var result = new List<IHidStream>();

        foreach (var productId in productIds)
        {
            foreach (var device in DeviceList.Local.GetHidDevices(vendorId, productId))
            {
                try
                {
                    var stream = device.Open();
                    if (stream != null)
                    {
                        var hidStream = new RealHidStream(stream, device);
                        _streams.Add(hidStream);
                        result.Add(hidStream);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "HIDデバイスのオープンに失敗しました (VID: {VendorId:X4}, PID: {ProductId:X4})", 
                        vendorId, productId);
                }
            }
        }

        if (result.Count > 0)
            _logger.LogInformation("{StreamCount}個のHIDストリームを検出しました", result.Count);

        return result;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        foreach (var stream in _streams)
        {
            stream?.Dispose();
        }

        _streams.Clear();
        _disposed = true;
    }
}
