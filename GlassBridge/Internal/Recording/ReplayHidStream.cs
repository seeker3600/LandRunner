namespace GlassBridge.Internal.Recording;

using GlassBridge.Internal.HID;

/// <summary>
/// 記録されたJSONファイルから生データを再生するHIDストリーム（VitureLuma非依存）
/// 使用例: var replayStream = new ReplayHidStream(recordingFilePath)
/// </summary>
internal sealed class ReplayHidStream : IHidStream
{
    /// <summary>
    /// デフォルトのレポート長（VITURE デバイスに合わせた値）
    /// Report ID (1 byte) + Report Data (64 bytes) = 65 bytes
    /// </summary>
    public const int DefaultReportLength = 65;

    private readonly Queue<(long delayMs, byte[] data)> _frameQueue;
    private DateTime _playbackStartTime;
    private bool _disposed;
    private IEnumerator<(long, byte[])>? _frameEnumerator;

    public bool IsOpen => !_disposed;

    /// <summary>
    /// 最大入力レポート長（Report ID を含む）
    /// </summary>
    public int MaxInputReportLength { get; }

    /// <summary>
    /// 最大出力レポート長（Report ID を含む）
    /// </summary>
    public int MaxOutputReportLength { get; }

    /// <summary>
    /// 記録ファイルから再生ストリームを作成
    /// </summary>
    /// <param name="recordingFilePath">記録ファイルのパス（.jsonl）</param>
    /// <param name="maxInputReportLength">最大入力レポート長（デフォルト: 65）</param>
    /// <param name="maxOutputReportLength">最大出力レポート長（デフォルト: 65）</param>
    public ReplayHidStream(
        string recordingFilePath,
        int maxInputReportLength = DefaultReportLength,
        int maxOutputReportLength = DefaultReportLength)
    {
        if (!File.Exists(recordingFilePath))
            throw new FileNotFoundException($"Recording file not found: {recordingFilePath}");

        _frameQueue = new Queue<(long, byte[])>();
        MaxInputReportLength = maxInputReportLength;
        MaxOutputReportLength = maxOutputReportLength;

        // フレームを読み込んでキューに積む
        LoadFramesFromJsonLines(recordingFilePath);
        _frameEnumerator = _frameQueue.GetEnumerator();
    }

    private void LoadFramesFromJsonLines(string recordingFilePath)
    {
        long previousTimestamp = 0;
        bool isFirstLine = true;
        
        using var reader = new StreamReader(recordingFilePath);
        string? line;
        
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                // 1行目はメタデータ、スキップ
                if (isFirstLine)
                {
                    isFirstLine = false;
                    var metadata = HidRecordingMetadata.FromJson(line);
                    continue;
                }

                // 2行目以降はフレームデータ
                var frameRecord = HidFrameRecord.FromJson(line);
                var rawBytes = frameRecord.DecodeRawBytes();
                
                // タイムスタンプの差分を計算してディレイを設定
                long delayMs = 0;
                if (previousTimestamp != 0)
                {
                    delayMs = frameRecord.Timestamp - previousTimestamp;
                }
                previousTimestamp = frameRecord.Timestamp;
                
                _frameQueue.Enqueue((delayMs, rawBytes));
            }
            catch
            {
                // 不正なフレームは無視
            }
        }
    }


    public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ReplayHidStream));

        // 初回呼び出し時に再生開始時刻を記録
        if (_playbackStartTime == default)
        {
            _playbackStartTime = DateTime.UtcNow;
        }

        if (_frameEnumerator == null || !_frameEnumerator.MoveNext())
            return 0; // ストリーム終了

        var (delayMs, frameData) = _frameEnumerator.Current;

        // タイミング制御：フレーム間のディレイを待機
        if (delayMs > 0)
        {
            await Task.Delay((int)delayMs, cancellationToken);
        }

        // バッファにコピー
        int bytesToCopy = Math.Min(frameData.Length, count);
        Array.Copy(frameData, 0, buffer, offset, bytesToCopy);

        return bytesToCopy;
    }

    public async Task WriteAsync(byte[] buffer, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ReplayHidStream));

        // 再生ストリームでは書き込みは何もしない
        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _frameEnumerator?.Dispose();
        _disposed = true;
        await ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }
}
