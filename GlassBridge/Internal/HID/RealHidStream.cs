namespace GlassBridge.Internal.HID;

using HidSharp;

/// <summary>
/// HidSharpの HidStream をラップした実装（非同期対応）
/// </summary>
internal sealed class RealHidStream : IHidStream
{
    private readonly HidStream _hidStream;
    private bool _disposed;

    public bool IsOpen => !_disposed;

    public RealHidStream(HidStream hidStream)
    {
        _hidStream = hidStream ?? throw new ArgumentNullException(nameof(hidStream));
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

