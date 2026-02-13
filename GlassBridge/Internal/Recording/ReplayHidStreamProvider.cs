namespace GlassBridge.Internal.Recording;

using GlassBridge.Internal.HID;

/// <summary>
/// 記録されたストリームを再生するためのプロバイダー
/// 使用例: var replayProvider = new ReplayHidStreamProvider(recordingDirectory)
/// </summary>
internal sealed class ReplayHidStreamProvider : IHidStreamProvider
{
    private readonly string _recordingDirectory;
    private bool _disposed;

    public ReplayHidStreamProvider(string recordingDirectory)
    {
        _recordingDirectory = recordingDirectory ?? throw new ArgumentNullException(nameof(recordingDirectory));
        
        if (!Directory.Exists(_recordingDirectory))
            throw new DirectoryNotFoundException($"Recording directory not found: {_recordingDirectory}");
    }

    public async Task<IReadOnlyList<IHidStream>> GetStreamsAsync(
        int vendorId,
        int[] productIds,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ReplayHidStreamProvider));

        var replayStreams = new List<IHidStream>();

        // ディレクトリの全recording_*.jsonlファイルを探して再生ストリームを作成
        var recordingFiles = Directory.GetFiles(_recordingDirectory, "recording_*.jsonl")
            .OrderBy(f => ExtractIndex(f))
            .ToList();

        foreach (var recordingFile in recordingFiles)
        {
            var replayStream = new ReplayHidStream(recordingFile);
            replayStreams.Add(replayStream);
        }

        return await Task.FromResult(replayStreams.AsReadOnly());
    }

    private static int ExtractIndex(string filename)
    {
        var match = System.Text.RegularExpressions.Regex.Match(Path.GetFileName(filename), @"recording_(\d+)");
        return match.Success && int.TryParse(match.Groups[1].Value, out int index) ? index : 0;
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

