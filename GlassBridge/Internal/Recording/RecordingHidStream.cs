namespace GlassBridge.Internal.Recording;

using GlassBridge.Internal.HID;
using Microsoft.Extensions.Logging;

/// <summary>
/// HIDストリームをラップして生データをJSONで記録する
/// 使用例: var recordingStream = new RecordingHidStream(innerStream, filePath)
/// </summary>
internal sealed class RecordingHidStream : IHidStream
{
    private static readonly ILogger<RecordingHidStream> _logger 
        = LoggerFactoryProvider.Instance.CreateLogger<RecordingHidStream>();

    private readonly IHidStream _innerStream;
    private readonly StreamWriter _recordingWriter;
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
    /// <param name="recordingPath">記録ファイルのパス</param>
    public RecordingHidStream(IHidStream innerStream, string recordingPath)
    {
        _innerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
        
        // ファイルのディレクトリを作成
        var directory = Path.GetDirectoryName(recordingPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // フレームファイルを作成
        _recordingWriter = new StreamWriter(recordingPath, false)
        {
            AutoFlush = true
        };
        _frameCount = 0;
    }

    public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RecordingHidStream));

        // 基盤ストリームから読み込み
        int bytesRead = await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);

        // 読み込んだデータを記録
        if (bytesRead > 0)
        {
            try
            {
                // HIDパケットを解析してフレームレコードに変換
                var rawData = buffer.AsSpan(offset, bytesRead).ToArray();
                
                // VitureLumaPacketから解析を試みる
                if (VitureLumaPacket.TryParseImuPacket(rawData, out var imuData) && imuData != null)
                {
                    var frameRecord = ImuFrameRecord.FromImuData(imuData, rawData);
                    await _recordingWriter.WriteLineAsync(frameRecord.ToJsonLine());
                    _frameCount++;
                }
            }
            catch (Exception ex)
            {
                // 解析エラーは無視（記録ができなくても処理を続ける）
                _logger.LogWarning(ex, "フレームデータの記録中にエラーが発生しました");
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
    /// 記録セッションを完了してメタデータを保存
    /// </summary>
    public async Task FinalizeAsync(string metadataPath)
    {
        if (_disposed)
            return;

        await _recordingWriter.FlushAsync();

        var metadata = ImuRecordingSession.CreateNew(_frameCount);
        await File.WriteAllTextAsync(metadataPath, metadata.ToJson());
        
        _logger.LogInformation("記録セッションを完了しました（{FrameCount}フレーム）", _frameCount);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        try
        {
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
