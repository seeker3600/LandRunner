namespace GlassBridge.Internal.Recording;

using GlassBridge.Internal.HID;
using Microsoft.Extensions.Logging;

/// <summary>
/// HID�X�g���[�������b�v���Đ��f�[�^��JSON�ŋL�^����
/// �g�p��: var recordingStream = new RecordingHidStream(innerStream, filePath)
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
    /// �ő���̓��|�[�g���iReport ID ���܂ށj
    /// �����X�g���[���ɈϏ�
    /// </summary>
    public int MaxInputReportLength => _innerStream.MaxInputReportLength;

    /// <summary>
    /// �ő�o�̓��|�[�g���iReport ID ���܂ށj
    /// �����X�g���[���ɈϏ�
    /// </summary>
    public int MaxOutputReportLength => _innerStream.MaxOutputReportLength;

    /// <summary>
    /// �L�^�𔺂�HID�X�g���[�����쐬
    /// </summary>
    /// <param name="innerStream">��ՂƂȂ�HID�X�g���[��</param>
    /// <param name="recordingPath">�L�^�t�@�C���̃p�X</param>
    public RecordingHidStream(IHidStream innerStream, string recordingPath)
    {
        _innerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
        
        // �t�@�C���̃f�B���N�g�����쐬
        var directory = Path.GetDirectoryName(recordingPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // �t���[���t�@�C�����쐬
        _recordingWriter = new StreamWriter(recordingPath, false)
        {
            AutoFlush = true
        };
        _frameCount = 0;
        
        _logger.LogDebug("Recording HID stream initialized: {RecordingPath}", recordingPath);
    }

    public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RecordingHidStream));

        // ��ՃX�g���[������ǂݍ���
        int bytesRead = await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);

        // �ǂݍ��񂾃f�[�^���L�^
        if (bytesRead > 0)
        {
            try
            {
                // HID�p�P�b�g����͂��ăt���[�����R�[�h�ɕϊ�
                var rawData = buffer.AsSpan(offset, bytesRead).ToArray();
                
                // VitureLumaPacket�����͂����݂�
                if (VitureLumaPacket.TryParseImuPacket(rawData, out var imuData) && imuData != null)
                {
                    var frameRecord = ImuFrameRecord.FromImuData(imuData, rawData);
                    await _recordingWriter.WriteLineAsync(frameRecord.ToJsonLine());
                    _frameCount++;
                    
                    if (_frameCount % 100 == 0)
                    {
                        _logger.LogDebug("Recorded {FrameCount} frames", _frameCount);
                    }
                }
                else
                {
                    // �p�[�X���s�ł����f�[�^�͋L�^�i�f�o�b�O�p�j
                    _logger.LogTrace("Failed to parse IMU packet from {BytesCount} bytes", bytesRead);
                }
            }
            catch (Exception ex)
            {
                // ��̓G���[�͖����i�L�^���ł��Ȃ��Ă������𑱂���j
                _logger.LogWarning(ex, "Error recording frame data");
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
    /// �L�^�Z�b�V�������������ă��^�f�[�^��ۑ�
    /// </summary>
    public async Task FinalizeAsync(string metadataPath)
    {
        if (_disposed)
            return;

        _logger.LogDebug("Finalizing recording session with {FrameCount} frames to: {MetadataPath}", _frameCount, metadataPath);

        await _recordingWriter.FlushAsync();

        var metadata = ImuRecordingSession.CreateNew(_frameCount);
        await File.WriteAllTextAsync(metadataPath, metadata.ToJson());
        
        _logger.LogInformation("Recording session finalized: {FrameCount} frames saved", _frameCount);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _logger.LogDebug("Disposing RecordingHidStream with {FrameCount} frames recorded", _frameCount);

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
