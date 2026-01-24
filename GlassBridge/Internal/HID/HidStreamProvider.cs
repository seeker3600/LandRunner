namespace GlassBridge.Internal.HID;

using HidSharp;

/// <summary>
/// HidSharpの薄いラッパー（デバイス非依存、VID/PIDで指定可能）
/// </summary>
internal sealed class HidStreamProvider : IHidStreamProvider
{
    private readonly int _vendorId;
    private readonly int[] _productIds;
    private readonly List<IHidStream> _streams = [];
    private bool _disposed;

    public HidStreamProvider(int vendorId, params int[] productIds)
    {
        if (productIds.Length == 0)
            throw new ArgumentException("At least one product ID must be specified.", nameof(productIds));

        _vendorId = vendorId;
        _productIds = productIds;
    }

    public async Task<IReadOnlyList<IHidStream>> GetStreamsAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(HidStreamProvider));

        var result = new List<IHidStream>();

        foreach (var productId in _productIds)
        {
            foreach (var device in DeviceList.Local.GetHidDevices(_vendorId, productId))
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
                        $"Failed to open HID device (VID: {_vendorId:X4}, PID: {productId:X4}): {ex.Message}");
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
