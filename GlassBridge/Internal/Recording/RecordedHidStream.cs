namespace GlassBridge.Internal.Recording;

using GlassBridge.Internal.HID;

/// <summary>
/// �L�^���ꂽJSON�t�@�C�����琶�f�[�^���Đ�����HID�X�g���[��
/// �g�p��: var replayStream = new RecordedHidStream(framesJsonPath, metadataJsonPath)
/// </summary>
internal sealed class RecordedHidStream : IHidStream
{
    /// <summary>
    /// �f�t�H���g�̃��|�[�g���iVITURE �f�o�C�X�ɍ��킹���l�j
    /// Report ID (1 byte) + Report Data (64 bytes) = 65 bytes
    /// </summary>
    public const int DefaultReportLength = 65;

    private readonly Queue<(long delayMs, byte[] data)> _frameQueue;
    private readonly DateTime _sessionStartTime;
    private DateTime _playbackStartTime;
    private bool _disposed;
    private IEnumerator<(long, byte[])>? _frameEnumerator;

    public bool IsOpen => !_disposed;

    /// <summary>
    /// �ő���̓��|�[�g���iReport ID ���܂ށj
    /// </summary>
    public int MaxInputReportLength { get; }

    /// <summary>
    /// �ő�o�̓��|�[�g���iReport ID ���܂ށj
    /// </summary>
    public int MaxOutputReportLength { get; }

    /// <summary>
    /// �L�^�t�@�C������Đ��X�g���[�����쐬
    /// </summary>
    /// <param name="framesJsonLinesPath">frames.jsonl�t�@�C���̃p�X</param>
    /// <param name="metadataJsonPath">metadata.json�t�@�C���̃p�X�i�I�v�V�����j</param>
    /// <param name="maxInputReportLength">�ő���̓��|�[�g���i�f�t�H���g: 65�j</param>
    /// <param name="maxOutputReportLength">�ő�o�̓��|�[�g���i�f�t�H���g: 65�j</param>
    public RecordedHidStream(
        string framesJsonLinesPath,
        string? metadataJsonPath = null,
        int maxInputReportLength = DefaultReportLength,
        int maxOutputReportLength = DefaultReportLength)
    {
        if (!File.Exists(framesJsonLinesPath))
            throw new FileNotFoundException($"Frames file not found: {framesJsonLinesPath}");

        _frameQueue = new Queue<(long, byte[])>();
        _sessionStartTime = DateTime.UtcNow;
        MaxInputReportLength = maxInputReportLength;
        MaxOutputReportLength = maxOutputReportLength;

        // ���^�f�[�^��ǂݍ���
        ImuRecordingSession? metadata = null;
        if (!string.IsNullOrEmpty(metadataJsonPath) && File.Exists(metadataJsonPath))
        {
            try
            {
                var json = File.ReadAllText(metadataJsonPath);
                metadata = ImuRecordingSession.FromJson(json);
            }
            catch
            {
                // ���^�f�[�^�ǂݍ��ݎ��s�͖���
            }
        }

        // �t���[����ǂݍ���ŃL���[�ɐς�
        LoadFramesFromJsonLines(framesJsonLinesPath);
    }

    private void LoadFramesFromJsonLines(string framesJsonLinesPath)
    {
        long previousTimestamp = 0;
        
        using var reader = new StreamReader(framesJsonLinesPath);
        string? line;
        
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                var frameRecord = ImuFrameRecord.FromJsonLine(line);
                var rawBytes = Convert.FromBase64String(frameRecord.RawBytes);
                
                // �^�C���X�^���v�̍������v�Z���ăf�B���C��ݒ�
                long delayMs = 0;
                if (previousTimestamp != 0)
                {
                    delayMs = (long)(frameRecord.Timestamp - previousTimestamp);
                }
                previousTimestamp = frameRecord.Timestamp;
                
                _frameQueue.Enqueue((delayMs, rawBytes));
            }
            catch
            {
                // �s���ȃt���[���͖���
            }
        }

        _frameEnumerator = _frameQueue.GetEnumerator();
    }

    public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RecordedHidStream));

        // ����Ăяo�����ɍĐ��J�n�������L�^
        if (_playbackStartTime == default)
        {
            _playbackStartTime = DateTime.UtcNow;
        }

        if (_frameEnumerator == null || !_frameEnumerator.MoveNext())
            return 0; // �X�g���[���I��

        var (delayMs, frameData) = _frameEnumerator.Current;

        // �^�C�~���O����F�t���[���Ԃ̃f�B���C��ҋ@
        if (delayMs > 0)
        {
            await Task.Delay((int)delayMs, cancellationToken);
        }

        // �o�b�t�@�ɃR�s�[
        int bytesToCopy = Math.Min(frameData.Length, count);
        Array.Copy(frameData, 0, buffer, offset, bytesToCopy);

        return bytesToCopy;
    }

    public async Task WriteAsync(byte[] buffer, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RecordedHidStream));

        // �Đ��X�g���[���ł͏������݂͉������Ȃ�
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
