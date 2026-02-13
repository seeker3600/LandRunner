namespace GlassBridge.Internal.Recording;

using GlassBridge.Internal.HID;
using System.Diagnostics;

/// <summary>
/// HIDストリームプロバイダーをラップして記録機能を追加
/// 複数ストリームを単一ファイルに記録
/// 使用例: var recordingProvider = new RecordingHidStreamProvider(baseProvider, "recording.jsonl")
/// </summary>
internal sealed class RecordingHidStreamProvider : IHidStreamProvider
{
    private readonly IHidStreamProvider _baseProvider;
    private readonly string _recordingFilePath;
    private readonly List<RecordingHidStream> _recordingStreams;
    private readonly StreamWriter _sharedWriter;
    private readonly Stopwatch _sharedStopwatch;
    private readonly SemaphoreSlim _writeLock;
    private bool _disposed;
    private bool _headerWritten;

    public RecordingHidStreamProvider(IHidStreamProvider baseProvider, string recordingFilePath)
    {
        _baseProvider = baseProvider ?? throw new ArgumentNullException(nameof(baseProvider));
        _recordingFilePath = recordingFilePath ?? throw new ArgumentNullException(nameof(recordingFilePath));
        _recordingStreams = new List<RecordingHidStream>();

        // ファイルのディレクトリを作成
        var directory = Path.GetDirectoryName(_recordingFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // 共有リソースを初期化
        _sharedWriter = new StreamWriter(_recordingFilePath, false)
        {
            AutoFlush = true
        };
        _sharedStopwatch = Stopwatch.StartNew();
        _writeLock = new SemaphoreSlim(1, 1);
        _headerWritten = false;
    }

    public async Task<IReadOnlyList<IHidStream>> GetStreamsAsync(
        int vendorId,
        int[] productIds,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RecordingHidStreamProvider));

        var baseStreams = await _baseProvider.GetStreamsAsync(vendorId, productIds, cancellationToken);

        // 初回呼び出し時にメタデータヘッダーを書き込む
        if (!_headerWritten)
        {
            var metadata = HidRecordingMetadata.Create(streamCount: baseStreams.Count);
            await _sharedWriter.WriteLineAsync(metadata.ToJson());
            _headerWritten = true;
        }

        // 各ストリームをRecordingHidStreamでラップ（共有リソースを使用）
        var recordingStreams = new List<IHidStream>();
        for (int i = 0; i < baseStreams.Count; i++)
        {
            var baseStream = baseStreams[i];
            var recordingStream = new RecordingHidStream(
                baseStream, 
                _sharedWriter, 
                streamId: i,
                _sharedStopwatch,
                _writeLock);
            
            _recordingStreams.Add(recordingStream);
            recordingStreams.Add(recordingStream);
        }

        return recordingStreams.AsReadOnly();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        // ストリームを破棄
        foreach (var recordingStream in _recordingStreams)
        {
            await recordingStream.DisposeAsync();
        }

        _recordingStreams.Clear();

        // 共有リソースを破棄
        _sharedStopwatch.Stop();
        await _sharedWriter.FlushAsync();
        await _sharedWriter.DisposeAsync();
        _writeLock.Dispose();

        await _baseProvider.DisposeAsync();
        _disposed = true;
    }

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }
}

