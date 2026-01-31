namespace GlassBridge.Internal.HID;

using HidSharp;

/// <summary>
/// HidSharp�� HidStream �����b�v���������i�񓯊��Ή��j
/// </summary>
internal sealed class RealHidStream : IHidStream
{
    private readonly HidStream _hidStream;
    private readonly HidDevice _device;
    private bool _disposed;

    public bool IsOpen => !_disposed;

    /// <summary>
    /// �ő���̓��|�[�g���iReport ID ���܂ށj
    /// </summary>
    public int MaxInputReportLength => _device.GetMaxInputReportLength();

    /// <summary>
    /// �ő�o�̓��|�[�g���iReport ID ���܂ށj
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

        // HidSharp �ł� BeginRead/EndRead �� APM �p�^�[�����g�p
        return await Task.Factory.FromAsync(
            (callback, state) => _hidStream.BeginRead(buffer, offset, count, callback, state),
            ar => _hidStream.EndRead(ar),
            state: null);
    }

    public async Task WriteAsync(byte[] buffer, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RealHidStream));

        // HidSharp �ł� BeginWrite/EndWrite �� APM �p�^�[�����g�p
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

