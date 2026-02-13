namespace GlassBridge.Internal.Recording;

using GlassBridge.Internal.HID;
using System.Diagnostics;

/// <summary>
/// HIDストリームをラップして生データをJSONで記録する（VitureLuma非依存）
/// 使用例: var recordingStream = new RecordingHidStream(innerStream, filePath)
/// </summary>
internal sealed class RecordingHidStream : IHidStream
{
    private readonly IHidStream _innerStream;
    private readonly StreamWriter _recordingWriter;
    private readonly Stopwatch _stopwatch;
    private int _frameCount;
    private bool _disposed;
    private bool _headerWritten;

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
    /// <param name="recordingPath">記録ファイルのパス（.jsonl）</param>
    public RecordingHidStream(IHidStream innerStream, string recordingPath)
    {
        _innerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
        
        // ファイルのディレクトリを作成
        var directory = Path.GetDirectoryName(recordingPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // 記録ファイルを作成
        _recordingWriter = new StreamWriter(recordingPath, false)
        {
            AutoFlush = true
        };
        _stopwatch = Stopwatch.StartNew();
        _frameCount = 0;
        _headerWritten = false;
    }


    public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RecordingHidStream));

        // 初回読み込み時にメタデータヘッダーを書き込む
        if (!_headerWritten)
        {
            var metadata = HidRecordingMetadata.Create(frameCount: 0);
            await _recordingWriter.WriteLineAsync(metadata.ToJson());
            _headerWritten = true;
        }

        // 基盤ストリームから読み込み
        int bytesRead = await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);

        // 読み込んだデータを記録（パース不要、生データのみ）
        if (bytesRead > 0)
        {
            try
            {
                var rawData = buffer.AsSpan(offset, bytesRead).ToArray();
                var timestamp = _stopwatch.ElapsedMilliseconds;
                var frameRecord = HidFrameRecord.Create(rawData, timestamp);
                
                await _recordingWriter.WriteLineAsync(frameRecord.ToJson());
                _frameCount++;
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

    /// <summary>
    /// 記録セッションを完了してメタデータを更新
    /// </summary>
    public async Task FinalizeAsync()
    {
        if (_disposed || !_headerWritten)
            return;

        await _recordingWriter.FlushAsync();
        _stopwatch.Stop();
    }

    /// <summary>
    /// 記録されたフレーム数を取得
    /// </summary>
    public int FrameCount => _frameCount;

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        try
        {
            await FinalizeAsync();
            
            if (_recordingWriter != null)
            {
                await _recordingWriter.FlushAsync();
                await _recordingWriter.DisposeAsync();
            }
        }
        finally
        {
            await _innerStream.DisposeAsync();
            _disposed = true;
        }
    }

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }
}

