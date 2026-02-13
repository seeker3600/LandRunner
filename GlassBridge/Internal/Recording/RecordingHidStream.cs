namespace GlassBridge.Internal.Recording;

using GlassBridge.Internal.HID;
using System.Diagnostics;

/// <summary>
/// HIDストリームをラップして生データをJSONで記録する（VitureLuma非依存）
/// 使用例: var recordingStream = new RecordingHidStream(innerStream, sharedWriter, streamId, stopwatch, semaphore)
/// </summary>
internal sealed class RecordingHidStream : IHidStream
{
    private readonly IHidStream _innerStream;
    private readonly StreamWriter _recordingWriter;
    private readonly Stopwatch _stopwatch;
    private readonly int _streamId;
    private readonly SemaphoreSlim _writeLock;
    private int _frameCount;
    private bool _disposed;

    public bool IsOpen => !_disposed && _innerStream.IsOpen;

    /// <summary>
    /// 最大入力レポート長（Report ID を含む）
    /// 内部ストリームに委譲
    /// </summary>
    public int MaxInputReportLength => _innerStream.MaxInputReportLength;

    /// <summary>
    /// 最大出力レポート長（Report ID を含む）
    /// 内部ストリームに委譲
    /// </summary>
    public int MaxOutputReportLength => _innerStream.MaxOutputReportLength;

    /// <summary>
    /// 記録を伴うHIDストリームを作成
    /// </summary>
    /// <param name="innerStream">基盤となるHIDストリーム</param>
    /// <param name="recordingWriter">共有する StreamWriter</param>
    /// <param name="streamId">ストリーム識別子</param>
    /// <param name="stopwatch">共有する Stopwatch</param>
    /// <param name="writeLock">書き込み排他制御用の SemaphoreSlim</param>
    public RecordingHidStream(
        IHidStream innerStream, 
        StreamWriter recordingWriter,
        int streamId,
        Stopwatch stopwatch,
        SemaphoreSlim writeLock)
    {
        _innerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
        _recordingWriter = recordingWriter ?? throw new ArgumentNullException(nameof(recordingWriter));
        _stopwatch = stopwatch ?? throw new ArgumentNullException(nameof(stopwatch));
        _writeLock = writeLock ?? throw new ArgumentNullException(nameof(writeLock));
        _streamId = streamId;
        _frameCount = 0;
    }


    public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RecordingHidStream));

        // 基盤ストリームから読み込み
        int bytesRead = await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);

        // 読み込んだデータを記録（パース不要、生データのみ）
        if (bytesRead > 0)
        {
            try
            {
                var rawData = buffer.AsSpan(offset, bytesRead).ToArray();
                var timestamp = _stopwatch.ElapsedMilliseconds;
                var frameRecord = HidFrameRecord.Create(rawData, timestamp, _streamId);
                
                // 排他制御付きで書き込み
                await _writeLock.WaitAsync(cancellationToken);
                try
                {
                    await _recordingWriter.WriteLineAsync(frameRecord.ToJson());
                    _frameCount++;
                }
                finally
                {
                    _writeLock.Release();
                }
            }
            catch
            {
                // 記録エラーは無視（記録ができなくても処理を続ける）
            }
        }

        return bytesRead;
    }



    public async Task WriteAsync(byte[] buffer, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RecordingHidStream));

        await _innerStream.WriteAsync(buffer, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        try
        {
            await _innerStream.DisposeAsync();
        }
        finally
        {
            _disposed = true;
        }
    }

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }
}

