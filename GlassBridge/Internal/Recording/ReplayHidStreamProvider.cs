namespace GlassBridge.Internal.Recording;

using GlassBridge.Internal.HID;

/// <summary>
/// 記録された単一ファイルからストリームを再生するためのプロバイダー
/// 使用例: var replayProvider = new ReplayHidStreamProvider("recording.jsonl")
/// </summary>
internal sealed class ReplayHidStreamProvider : IHidStreamProvider
{
    private readonly string _recordingFilePath;
    private bool _disposed;

    public ReplayHidStreamProvider(string recordingFilePath)
    {
        _recordingFilePath = recordingFilePath ?? throw new ArgumentNullException(nameof(recordingFilePath));
        
        if (!File.Exists(_recordingFilePath))
            throw new FileNotFoundException($"Recording file not found: {_recordingFilePath}");
    }

    public async Task<IReadOnlyList<IHidStream>> GetStreamsAsync(
        int vendorId,
        int[] productIds,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ReplayHidStreamProvider));

        // ファイルを読み込んでストリーム数を取得
        var streamCount = await GetStreamCountAsync();

        // 各ストリームIDに対応する再生ストリームを作成
        var replayStreams = new List<IHidStream>();
        for (int streamId = 0; streamId < streamCount; streamId++)
        {
            var replayStream = new ReplayHidStream(_recordingFilePath, streamId);
            replayStreams.Add(replayStream);
        }

        return replayStreams.AsReadOnly();
    }

    private async Task<int> GetStreamCountAsync()
    {
        using var reader = new StreamReader(_recordingFilePath);
        var firstLine = await reader.ReadLineAsync();
        
        if (string.IsNullOrWhiteSpace(firstLine))
            throw new InvalidOperationException("Recording file is empty");

        var metadata = HidRecordingMetadata.FromJson(firstLine);
        return metadata.StreamCount;
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        await ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }
}

