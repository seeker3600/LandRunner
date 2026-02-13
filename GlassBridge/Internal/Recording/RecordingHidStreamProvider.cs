namespace GlassBridge.Internal.Recording;

using GlassBridge.Internal.HID;

/// <summary>
/// HIDストリームプロバイダーをラップして記録機能を追加
/// 使用例: var recordingProvider = new RecordingHidStreamProvider(baseProvider, outputDir)
/// </summary>
internal sealed class RecordingHidStreamProvider : IHidStreamProvider
{
    private readonly IHidStreamProvider _baseProvider;
    private readonly string _outputDirectory;
    private readonly List<RecordingHidStream> _recordingStreams;
    private bool _disposed;

    public RecordingHidStreamProvider(IHidStreamProvider baseProvider, string outputDirectory)
    {
        _baseProvider = baseProvider ?? throw new ArgumentNullException(nameof(baseProvider));
        _outputDirectory = outputDirectory ?? throw new ArgumentNullException(nameof(outputDirectory));
        _recordingStreams = new List<RecordingHidStream>();

        // 出力ディレクトリを作成
        Directory.CreateDirectory(_outputDirectory);
    }

    public async Task<IReadOnlyList<IHidStream>> GetStreamsAsync(
        int vendorId,
        int[] productIds,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RecordingHidStreamProvider));

        var baseStreams = await _baseProvider.GetStreamsAsync(vendorId, productIds, cancellationToken);

        // 各ストリームをRecordingHidStreamでラップ
        var recordingStreams = new List<IHidStream>();
        for (int i = 0; i < baseStreams.Count; i++)
        {
            var baseStream = baseStreams[i];
            var recordingPath = Path.Combine(_outputDirectory, $"recording_{i}.jsonl");
            var recordingStream = new RecordingHidStream(baseStream, recordingPath);
            
            _recordingStreams.Add(recordingStream);
            recordingStreams.Add(recordingStream);
        }

        return recordingStreams.AsReadOnly();
    }

    /// <summary>
    /// 記録セッションを完了
    /// </summary>
    public async Task FinalizeRecordingAsync()
    {
        foreach (var recordingStream in _recordingStreams)
        {
            await recordingStream.FinalizeAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        // 自動的に記録を完了
        await FinalizeRecordingAsync();

        foreach (var recordingStream in _recordingStreams)
        {
            await recordingStream.DisposeAsync();
        }

        _recordingStreams.Clear();
        await _baseProvider.DisposeAsync();
        _disposed = true;
    }

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }
}

