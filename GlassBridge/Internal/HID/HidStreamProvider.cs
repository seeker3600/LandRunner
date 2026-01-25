namespace GlassBridge.Internal.HID;

using HidSharp;

/// <summary>
/// HidSharpの薄いラッパー（デバイス非依存、VID/PIDは呼び出し時に指定）
/// </summary>
internal sealed class HidStreamProvider : IHidStreamProvider
{
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
                        var hidStream = new RealHidStream(stream);
                        _streams.Add(hidStream);
                        result.Add(hidStream);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Failed to open HID device (VID: {vendorId:X4}, PID: {productId:X4}): {ex.Message}");
                }
            }
        }

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
