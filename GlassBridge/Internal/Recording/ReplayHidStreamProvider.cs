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

    public async Task<IReadOnlyList<IHidStream>> GetStreamsAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ReplayHidStreamProvider));

        var replayStreams = new List<IHidStream>();

        // ディレクトリ内の全frames_*.jsonlファイルを探して再生ストリームを作成
        var framesFiles = Directory.GetFiles(_recordingDirectory, "frames_*.jsonl")
            .OrderBy(f => ExtractIndex(f))
            .ToList();

        foreach (var framesFile in framesFiles)
        {
            int index = ExtractIndex(framesFile);
            var metadataFile = Path.Combine(_recordingDirectory, $"metadata_{index}.json");

            var replayStream = new RecordedHidStream(
                framesFile,
                File.Exists(metadataFile) ? metadataFile : null
            );
            replayStreams.Add(replayStream);
        }

        return replayStreams.AsReadOnly();
    }

    private static int ExtractIndex(string filename)
    {
        var match = System.Text.RegularExpressions.Regex.Match(Path.GetFileName(filename), @"frames_(\d+)");
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
