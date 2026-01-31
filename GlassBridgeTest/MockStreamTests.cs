namespace GlassBridgeTest;

using GlassBridge;
using GlassBridge.Internal;
using GlassBridge.Internal.HID;
using Xunit;

/// <summary>
/// MockHidStream �� MockMcuStream �̓���e�X�g
/// </summary>
public class MockStreamTests
{
    /// <summary>
    /// �e�X�g�pIMU�f�[�^�W�F�l���[�^
    /// </summary>
    private static async IAsyncEnumerable<ImuData> GenerateTestImuData(
        int count = 1,
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

            await Task.Delay(1, cancellationToken);
        }
    }

    /// <summary>
    /// �e�X�g1: MockMcuStream ���R�}���h�󂯎���� ACK ��Ԃ�
    /// </summary>
    [Fact]
    public async Task MockMcuStream_ShouldReturnAckAfterWrite()
    {
        // Arrange
        var mcuStream = new MockMcuStream();
        var buffer = new byte[64];

        // Act: Write
        await mcuStream.WriteAsync(buffer, CancellationToken.None);

        // Read
        int bytesRead = await mcuStream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);

        // Assert
        Assert.True(bytesRead > 0, "MCU stream should return ACK packet");
        Assert.Equal(0xFF, buffer[0]);
        Assert.Equal(0xFD, buffer[1]);

        await mcuStream.DisposeAsync();
    }

    /// <summary>
    /// �e�X�g2: MockHidStream �� IMU �f�[�^��Ԃ�
    /// </summary>
    [Fact]
    public async Task MockHidStream_ShouldReturnImuData()
    {
        // Arrange
        var imuStream = new MockHidStream(GenerateTestImuData(1, CancellationToken.None), CancellationToken.None);
        var buffer = new byte[64];

        // Act
        int bytesRead = await imuStream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);

        // Assert
        Assert.True(bytesRead > 0, "IMU stream should return data");
        Assert.Equal(0xFF, buffer[0]);
        // �w�b�_�m�F: IMU �f�[�^
        if (bytesRead >= 2)
        {
            Assert.Equal(0xFC, buffer[1]);
        }

        await imuStream.DisposeAsync();
    }

    /// <summary>
    /// �e�X�g3: MockHidStreamProvider �� 2 �̃X�g���[����Ԃ�
    /// </summary>
    [Fact]
     public async Task MockHidStreamProvider_ShouldReturnTwoStreams()
    {
        // Arrange
        var provider = new MockHidStreamProvider(ct => GenerateTestImuData(1, ct));

        // Act
        var streams = await provider.GetStreamsAsync(
            VitureLumaDevice.VendorId,
            VitureLumaDevice.SupportedProductIds,
            CancellationToken.None);

        // Assert
        Assert.Equal(2, streams.Count);
        
        // �ŏ���MCU�A����IMU
        var mcuStream = streams[0] as MockMcuStream;
        var imuStream = streams[1] as MockHidStream;

        Assert.NotNull(mcuStream);
        Assert.NotNull(imuStream);

        await provider.DisposeAsync();
    }

    /// <summary>
    /// �e�X�g4: MockHidStream ���畡����ǂݍ���
    /// </summary>
    [Fact]
    public async Task MockHidStream_ShouldSupportMultipleReads()
    {
        // Arrange
        var imuStream = new MockHidStream(GenerateTestImuData(2, CancellationToken.None), CancellationToken.None);
        var buffer = new byte[64];

        // Act: 1��ڂ̓ǂݍ���
        int bytesRead1 = await imuStream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);

        // 2��ڂ̓ǂݍ���
        int bytesRead2 = await imuStream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);

        // Assert
        Assert.True(bytesRead1 > 0, "First read should return data");
        Assert.True(bytesRead2 > 0, "Second read should return data");

        await imuStream.DisposeAsync();
    }

    /// <summary>
    /// �e�X�g5: MockHidStream �f�[�^�X�g���[���I����� 0 ��Ԃ�
    /// </summary>
    [Fact]
    public async Task MockHidStream_ShouldReturnZeroWhenDataExhausted()
    {
        // Arrange
        var imuStream = new MockHidStream(GenerateTestImuData(1, CancellationToken.None), CancellationToken.None);
        var buffer = new byte[64];

        // Act: 1��ڂ̓ǂݍ���
        int bytesRead1 = await imuStream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);

        // 2��ڈȍ~�̓ǂݍ���
        int bytesRead2 = await imuStream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);
        int bytesRead3 = await imuStream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);

        // Assert
        Assert.True(bytesRead1 > 0, "First read should return data");
        Assert.Equal(0, bytesRead2); // �f�[�^�I����
        Assert.Equal(0, bytesRead3); // �p������ 0

        await imuStream.DisposeAsync();
    }

    /// <summary>
    /// �e�X�g6: MockHidStream ���p�P�b�g���V���A���C�Y
    /// </summary>
    [Fact]
    public async Task MockHidStream_ShouldSerializePacketCorrectly()
    {
        // Arrange
        var imuStream = new MockHidStream(GenerateTestImuData(1, CancellationToken.None), CancellationToken.None);
        var buffer = new byte[64];

        // Act
        int bytesRead = await imuStream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);

        // Assert
        Assert.True(bytesRead >= 20, "Packet should be at least 20 bytes");
        Assert.Equal(0xFF, buffer[0]); // Header byte 0
        Assert.Equal(0xFC, buffer[1]); // Header byte 1 (IMU data)

        await imuStream.DisposeAsync();
    }
}
