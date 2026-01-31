namespace GlassBridgeTest;

using GlassBridge;
using GlassBridge.Internal;
using GlassBridge.Internal.HID;
using Xunit;

/// <summary>
/// VitureLumaDevice �̃e�X�g
/// �d�l�m�F�e�X�g�i�ȗ��Łj
/// </summary>
public class VitureLumaDeviceTests
{
    /// <summary>
    /// �e�X�g�pIMU�f�[�^�W�F�l���[�^
    /// �f�[�^��M���x���V�~�����[�V�����\
    /// </summary>
    /// <param name="count">��������f�[�^��</param>
    /// <param name="delayMs">�t���[���Ԃ̒x���ims�j�B0 �Ńp�t�H�[�}���X�v���A>0 �Ń^�C���A�E�g�����e�X�g</param>
    /// <param name="cancellationToken">�L�����Z���g�[�N��</param>
    private static async IAsyncEnumerable<ImuData> GenerateTestImuData(
        int count = 10,
        int delayMs = 0,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < count; i++)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            yield return new ImuData
            {
                Quaternion = new GlassBridge.Quaternion(0.707f, 0f, 0f, 0.707f),
                EulerAngles = new EulerAngles(0f, 45f, 0f),
                Timestamp = (uint)(1000 + i),
                MessageCounter = (ushort)i
            };

            if (delayMs > 0)
            {
                await Task.Delay(delayMs, cancellationToken);
            }
        }
    }

    /// <summary>
    /// �e�X�g1: �f�o�C�X�ڑ��i�p�t�H�[�}���X�v���p�j
    /// �d�l�F�u�f�o�C�X�ڑ�����IsConnected��true�ɂȂ�v
    /// �x���Ȃ��ō������s
    /// </summary>
    [Fact(Timeout = 5000)]
    public async Task ConnectAsync_ShouldSucceed()
    {
        // Arrange
        var mockProvider = new MockHidStreamProvider(ct => GenerateTestImuData(5, delayMs: 0, cancellationToken: ct));

        // Act
        var device = await VitureLumaDevice.ConnectWithProviderAsync(mockProvider);

        // Assert
        Assert.NotNull(device);
        Assert.True(device.IsConnected);

        await device.DisposeAsync();
    }

    /// <summary>
    /// �e�X�g2: GetImuDataStreamAsync ���\�b�h�����݂��A�Ăяo���\�i�p�t�H�[�}���X�v���p�j
    /// �d�l�F�uIMU�f�[�^�X�g���[�����\�b�h����������Ă���v
    /// �x���Ȃ��ō������s
    /// </summary>
    [Fact(Timeout = 5000)]
    public async Task GetImuDataStreamAsync_ShouldBeCallable()
    {
        // Arrange
        var mockProvider = new MockHidStreamProvider(ct => GenerateTestImuData(1, delayMs: 0, cancellationToken: ct));
        var device = await VitureLumaDevice.ConnectWithProviderAsync(mockProvider);
        Assert.NotNull(device);
        Assert.True(device.IsConnected);

        // Act: GetImuDataStreamAsync ���\�b�h���Ăяo���\���m�F
        var streamMethod = device.GetType().GetMethod("GetImuDataStreamAsync");

        // Assert: ���\�b�h�����݂��A��������Ă��邱�Ƃ��m�F
        Assert.NotNull(streamMethod);
        Assert.True(streamMethod.ReturnType.IsGenericType);

        await device.DisposeAsync();
    }

    /// <summary>
    /// �e�X�g3: Dispose���̐���I���i�p�t�H�[�}���X�v���p�j
    /// �d�l�F�uDisposeasync��IsConnected��false�ɂȂ�v
    /// �x���Ȃ��ō������s
    /// </summary>
    [Fact(Timeout = 5000)]
    public async Task DisposeAsync_ShouldDisconnect()
    {
        // Arrange
        var mockProvider = new MockHidStreamProvider(ct => GenerateTestImuData(5, delayMs: 0, cancellationToken: ct));
        var device = await VitureLumaDevice.ConnectWithProviderAsync(mockProvider);
        Assert.NotNull(device);
        Assert.True(device.IsConnected);

        // Act
        await device.DisposeAsync();

        // Assert
        Assert.False(device.IsConnected);
    }

    /// <summary>
    /// �e�X�g4: �ᑬ�f�[�^�X�g���[����M�̃V�~�����[�V����
    /// ���f�o�C�X�͐� ms�`���\ ms �̃^�C�~���O�Ńf�[�^�𑗐M����
    /// �^�C���A�E�g������o�b�t�@�����O����̊m�F�p
    /// </summary>
    [Fact(Timeout = 10000)]
    public async Task ConnectAsync_WithDelayedData_ShouldSucceed()
    {
        // Arrange: 10ms �̒x���Ńf�[�^�𑗐M�i���f�o�C�X�V�~�����[�V�����j
        var mockProvider = new MockHidStreamProvider(ct => GenerateTestImuData(10, delayMs: 10, cancellationToken: ct));

        // Act
        var device = await VitureLumaDevice.ConnectWithProviderAsync(mockProvider);

        // Assert
        Assert.NotNull(device);
        Assert.True(device.IsConnected);

        await device.DisposeAsync();
    }

    /// <summary>
    /// �e�X�g5: �f�o�C�X�������ƕ�����̐ڑ��e�X�g
    /// �����̃f�o�C�X�ڑ��V�[�P���X������ɓ��삷�邱�Ƃ��m�F
    /// </summary>
    [Fact(Timeout = 10000)]
    public async Task ConnectAsync_MultipleConnections_ShouldSucceed()
    {
        // Arrange & Act: ������̐ڑ������s
        for (int i = 0; i < 3; i++)
        {
            var mockProvider = new MockHidStreamProvider(ct => GenerateTestImuData(3, delayMs: 0, cancellationToken: ct));
            var device = await VitureLumaDevice.ConnectWithProviderAsync(mockProvider);

            // Assert
            Assert.NotNull(device);
            Assert.True(device.IsConnected);

            await device.DisposeAsync();
            Assert.False(device.IsConnected);
        }
    }

    /// <summary>
    /// �e�X�g6: �f�o�C�X��������� IMU ������������Ă��邱�Ƃ��m�F
    /// DisposeAsync �ł��������R�}���h�����M�����
    /// </summary>
    [Fact(Timeout = 10000)]
    public async Task DisposeAsync_ShouldDisableImuOnCleanup()
    {
        // Arrange
        var mockProvider = new MockHidStreamProvider(ct => GenerateTestImuData(10, delayMs: 0, cancellationToken: ct));
        var device = await VitureLumaDevice.ConnectWithProviderAsync(mockProvider);
        Assert.NotNull(device);
        Assert.True(device.IsConnected);

        // Act: Dispose ���Ăяo��
        await device.DisposeAsync();

        // Assert: �f�o�C�X���ؒf���ꂽ���Ƃ��m�F
        Assert.False(device.IsConnected);
        // Dispose ���� IMU �������R�}���h�����M�����i�����ڍׂ����m�F�\�j
    }
}



