namespace GlassBridge.Internal.Recording;

using GlassBridge.Internal.HID;

/// <summary>
/// HID�X�g���[���v���o�C�_�[�����b�v���ċL�^�@�\��ǉ�
/// �g�p��: var recordingProvider = new RecordingHidStreamProvider(baseProvider, outputDir)
/// </summary>
internal sealed class RecordingHidStreamProvider : IHidStreamProvider
{
    private readonly IHidStreamProvider _baseProvider;
    private readonly string _outputDirectory;
    private readonly Dictionary<IHidStream, RecordingHidStream> _recordingStreams;
    private bool _disposed;

    public RecordingHidStreamProvider(IHidStreamProvider baseProvider, string outputDirectory)
    {
        _baseProvider = baseProvider ?? throw new ArgumentNullException(nameof(baseProvider));
        _outputDirectory = outputDirectory ?? throw new ArgumentNullException(nameof(outputDirectory));
        _recordingStreams = new Dictionary<IHidStream, RecordingHidStream>();

        // �o�̓f�B���N�g�����쐬
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

        // �e�X�g���[����RecordingHidStream�Ń��b�v
        var recordingStreams = new List<IHidStream>();
        for (int i = 0; i < baseStreams.Count; i++)
        {
            var baseStream = baseStreams[i];
            var framesPath = Path.Combine(_outputDirectory, $"frames_{i}.jsonl");
            var recordingStream = new RecordingHidStream(baseStream, framesPath);
            
            _recordingStreams[baseStream] = recordingStream;
            recordingStreams.Add(recordingStream);
        }

        return recordingStreams.AsReadOnly();
    }

    /// <summary>
    /// �L�^�Z�b�V�������������ă��^�f�[�^��ۑ�
    /// </summary>
    public async Task FinalizeRecordingAsync()
    {
        foreach (var (index, recordingStream) in _recordingStreams.Values.Select((s, i) => (i, s)))
        {
            var metadataPath = Path.Combine(_outputDirectory, $"metadata_{index}.json");
            await recordingStream.FinalizeAsync(metadataPath);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        // �����I�Ƀ��^�f�[�^��ۑ�
        await FinalizeRecordingAsync();

        foreach (var recordingStream in _recordingStreams.Values)
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
