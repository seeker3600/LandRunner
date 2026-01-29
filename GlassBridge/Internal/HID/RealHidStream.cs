namespace GlassBridge.Internal.HID;

using HidSharp;

/// <summary>
/// HidSharpの HidStream をラップした実装（非同期対応）
/// </summary>
internal sealed class RealHidStream : IHidStream
{
    private readonly HidStream _hidStream;
    private readonly HidDevice _device;
    private bool _disposed;

    public bool IsOpen => !_disposed;

    /// <summary>
    /// 最大入力レポート長（Report ID を含む）
    /// </summary>
    public int MaxInputReportLength => _device.GetMaxInputReportLength();

    /// <summary>
    /// 最大出力レポート長（Report ID を含む）
    /// </summary>
    public int MaxOutputReportLength => _device.GetMaxOutputReportLength();

    public RealHidStream(HidStream hidStream, HidDevice device)
    {
        _hidStream = hidStream ?? throw new ArgumentNullException(nameof(hidStream));
        _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RealHidStream));

        // HidSharp では BeginRead/EndRead の APM パターンを使用
        return await Task.Factory.FromAsync(
            (callback, state) => _hidStream.BeginRead(buffer, offset, count, callback, state),
            ar => _hidStream.EndRead(ar),
            state: null);
    }

    public async Task WriteAsync(byte[] buffer, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RealHidStream));

        // HidSharp では BeginWrite/EndWrite の APM パターンを使用
        await Task.Factory.FromAsync(
            (callback, state) => _hidStream.BeginWrite(buffer, 0, buffer.Length, callback, state),
            ar => _hidStream.EndWrite(ar),
            state: null);
    }

    public async ValueTask DisposeAsync()
    {
        Dispose();
        await ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _hidStream?.Dispose();
        _disposed = true;
    }
}

