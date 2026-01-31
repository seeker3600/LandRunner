namespace GlassBridge;

/// <summary>
/// IMU�f�o�C�X�Ǘ��C���^�[�t�F�[�X�i�e�X�g�\�����l���j
/// </summary>
public interface IImuDevice : IAsyncDisposable
{
    /// <summary>
    /// IMU�f�[�^�X�g���[�����擾
    /// </summary>
    IAsyncEnumerable<ImuData> GetImuDataStreamAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// �f�o�C�X���ڑ�����Ă��邩���m�F
    /// </summary>
    bool IsConnected { get; }
}

/// <summary>
/// IMU�f�o�C�X�}�l�[�W���[�̃C���^�[�t�F�[�X
/// </summary>
public interface IImuDeviceManager : IDisposable
{
    /// <summary>
    /// VITURE Luma�f�o�C�X�����o���Đڑ�
    /// </summary>
    /// <returns>�ڑ����ꂽIMU�f�o�C�X�A�ڑ����s����null</returns>
    Task<IImuDevice?> ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// �f�o�C�X�ɐڑ����� IMU �f�[�^���L�^
    /// �擾�����f�o�C�X���� GetImuDataStreamAsync() �Ŏ擾�����f�[�^�͎����I�ɋL�^�����
    /// device.DisposeAsync() ���Ɏ����I�Ƀ��^�f�[�^���ۑ������
    /// </summary>
    /// <param name="outputDirectory">�L�^�t�@�C���̏o�͐�f�B���N�g��</param>
    /// <param name="cancellationToken">�L�����Z���g�[�N��</param>
    /// <returns>�L�^�t���Őڑ����ꂽIMU�f�o�C�X�A�ڑ����s����null</returns>
    Task<IImuDevice?> ConnectAndRecordAsync(
        string outputDirectory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// �L�^���ꂽ�f�[�^�t�@�C������ IMU �f�o�C�X���Đ�
    /// ���ۂ̃f�o�C�X�̑���ɁA�L�^���ꂽ�f�[�^���X�g���[���z�M���� Mock �f�o�C�X��Ԃ�
    /// </summary>
    /// <param name="recordingDirectory">�L�^�t�@�C�����ۑ�����Ă���f�B���N�g��</param>
    /// <param name="cancellationToken">�L�����Z���g�[�N��</param>
    /// <returns>�Đ��p�� Mock �f�o�C�X�A�t�@�C���Ȃ�����null</returns>
    Task<IImuDevice?> ConnectFromRecordingAsync(
        string recordingDirectory,
        CancellationToken cancellationToken = default);
}
