namespace GlassBridge;

using GlassBridge.Internal;
using GlassBridge.Internal.HID;
using GlassBridge.Internal.Recording;
using Microsoft.Extensions.Logging;

/// <summary>
/// IMU�f�o�C�X�}�l�[�W���[�̎���
/// �f�o�C�X�ڑ��A�L�^�A�Đ��@�\���
/// </summary>
public sealed class ImuDeviceManager : IImuDeviceManager
{
    private static readonly ILogger<ImuDeviceManager> _logger 
        = LoggerFactoryProvider.Instance.CreateLogger<ImuDeviceManager>();

    private bool _disposed;
    private RecordingHidStreamProvider? _recordingProvider;

    /// <summary>
    /// VITURE Luma�f�o�C�X�ɒʏ�ڑ�
    /// </summary>
    public async Task<IImuDevice?> ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ImuDeviceManager));

        _logger.LogDebug("Starting device connection (normal mode)");
        var device = await VitureLumaDevice.ConnectAsync(cancellationToken);
        
        if (device != null)
        {
            _logger.LogInformation("Device connected successfully");
        }
        else
        {
            _logger.LogWarning("Failed to connect to device");
        }
        
        return device;
    }

    /// <summary>
    /// �f�o�C�X�ɐڑ����� IMU �f�[�^���L�^
    /// �擾�����f�o�C�X���� GetImuDataStreamAsync() �Ŏ擾�����f�[�^�͎����I�ɋL�^�����
    /// device.DisposeAsync() ���Ɏ����I�Ƀ��^�f�[�^���ۑ������
    /// </summary>
    public async Task<IImuDevice?> ConnectAndRecordAsync(
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ImuDeviceManager));

        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new ArgumentException("Output directory must not be null or empty", nameof(outputDirectory));

        _logger.LogDebug("Starting device connection with recording to: {OutputDirectory}", outputDirectory);

        // �O��̋L�^�Z�b�V�������I��
        if (_recordingProvider != null)
        {
            _logger.LogDebug("Disposing previous recording session");
            await _recordingProvider.DisposeAsync();
        }

        // ��{�I��HID�X�g���[���v���o�C�_�[���쐬
        var baseProvider = new HidStreamProvider();

        // �L�^�@�\�Ń��b�v
        _recordingProvider = new RecordingHidStreamProvider(baseProvider, outputDirectory);

        // �f�o�C�X�ɐڑ�
        var device = await VitureLumaDevice.ConnectWithProviderAsync(_recordingProvider, cancellationToken);
        
        if (device != null)
        {
            _logger.LogInformation("Device connected successfully with recording enabled");
        }
        else
        {
            _logger.LogError("Failed to connect to device for recording");
        }
        
        return device;
    }

    /// <summary>
    /// �L�^���ꂽ�f�[�^�t�@�C������ IMU �f�o�C�X���Đ�
    /// ���ۂ̃f�o�C�X�̑���ɁA�L�^���ꂽ�f�[�^���X�g���[���z�M���� Mock �f�o�C�X��Ԃ�
    /// </summary>
    public async Task<IImuDevice?> ConnectFromRecordingAsync(
        string recordingDirectory,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ImuDeviceManager));

        if (string.IsNullOrWhiteSpace(recordingDirectory))
            throw new ArgumentException("Recording directory must not be null or empty", nameof(recordingDirectory));

        if (!Directory.Exists(recordingDirectory))
            throw new DirectoryNotFoundException($"Recording directory not found: {recordingDirectory}");

        _logger.LogDebug("Starting device connection from recording: {RecordingDirectory}", recordingDirectory);

        // �Đ��v���o�C�_�[���쐬
        var replayProvider = new ReplayHidStreamProvider(recordingDirectory);

        // Mock �f�o�C�X�Ƃ��čĐ�
        var device = await VitureLumaDevice.ConnectWithProviderAsync(replayProvider, cancellationToken);

        if (device != null)
        {
            _logger.LogInformation("Device connected successfully from recording");
        }
        else
        {
            _logger.LogError("Failed to connect to device from recording");
        }

        return device;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_recordingProvider != null)
        {
            try
            {
                _recordingProvider.DisposeAsync().GetAwaiter().GetResult();
            }
            catch
            {
                // �f�B�X�|�[�Y���̃G���[�͖���
            }
        }

        _disposed = true;
    }
}
