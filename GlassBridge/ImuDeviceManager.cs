namespace GlassBridge;

/// <summary>
/// IMUデバイスマネージャーの実装
/// </summary>
public sealed class ImuDeviceManager : IImuDeviceManager
{
    private bool _disposed;

    /// <summary>
    /// VITURE Lumaデバイスに接続
    /// </summary>
    public async Task<IImuDevice?> ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ImuDeviceManager));

        return await VitureLumaDevice.ConnectAsync(cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
    }
}
