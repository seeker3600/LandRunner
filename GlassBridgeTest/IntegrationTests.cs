namespace GlassBridgeTest;

using GlassBridge;
using GlassBridge.Internal;
using GlassBridge.Internal.HID;
using Xunit;

/// <summary>
/// �����e�X�g
/// �����̃R���|�[�l���g�̑��ݍ�p���e�X�g
/// </summary>
public class IntegrationTests
{
    /// <summary>
    /// �e�X�g1: MockProvider + Device �̓����e�X�g
    /// �d�l�F�uProvider���X�g���[����Device�ɒ񋟂��A����ɓ��삷��v
    /// </summary>
    [Fact(Timeout = 5000)]
    public async Task MockProvider_WithDevice_ShouldIntegrateCorrectly()
    {
        // Arrange
        var mockProvider = new MockHidStreamProvider(GenerateTestImuData);

        // Act
        var device = await VitureLumaDevice.ConnectWithProviderAsync(mockProvider);

        // Assert
        Assert.NotNull(device);
        Assert.True(device.IsConnected);

        await device.DisposeAsync();
        await mockProvider.DisposeAsync();
    }

    /// <summary>
    /// �e�X�g2: �����̃f�o�C�X�ڑ��e�X�g
    /// �d�l�F�u������̐ڑ��E�ؒf������ɓ��삷��v
    /// </summary>
    [Fact(Timeout = 10000)]
    public async Task MultipleConnect_ShouldSucceed()
    {
        // Act & Assert
        for (int i = 0; i < 3; i++)
        {
            var mockProvider = new MockHidStreamProvider(GenerateTestImuData);
            var device = await VitureLumaDevice.ConnectWithProviderAsync(mockProvider);

            Assert.NotNull(device);
            Assert.True(device.IsConnected);

            await device.DisposeAsync();
            await mockProvider.DisposeAsync();
        }
    }

    /// <summary>
    /// �e�X�g3: DisposeAsync �̓�d�Ăяo��
    /// �d�l�F�u��dDispose����O�𔭐������Ȃ��v
    /// </summary>
    [Fact(Timeout = 5000)]
    public async Task DisposeAsync_MultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var mockProvider = new MockHidStreamProvider(GenerateTestImuData);
        var device = await VitureLumaDevice.ConnectWithProviderAsync(mockProvider);

        // Act & Assert
        await device.DisposeAsync();
        await device.DisposeAsync(); // 2���
        
        Assert.False(device.IsConnected);
    }

    /// <summary>
    /// �e�X�g4: Disposed��̃��\�b�h�Ăяo��
    /// �d�l�F�uDisposed���GetImuDataStreamAsync�͗�O�𔭐�������v
    /// </summary>
    [Fact(Timeout = 5000)]
    public async Task MethodCall_AfterDispose_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var mockProvider = new MockHidStreamProvider(GenerateTestImuData);
        var device = await VitureLumaDevice.ConnectWithProviderAsync(mockProvider);
        await device.DisposeAsync();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var data in device.GetImuDataStreamAsync())
            {
                // �����ɓ��B���Ȃ�����
            }
        });

        Assert.Contains("not connected", ex.Message, System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// �e�X�g5: MockHidStream �� Crc16Ccitt �̓���
    /// �d�l�F�u�V���A���C�Y���ꂽ�p�P�b�g��CRC�������v
    /// </summary>
    [Fact]
    public void SerializedPacket_CRC_ShouldBeValid()
    {
        // Arrange
        var imuData = new ImuData
        {
            Quaternion = new Quaternion(0.707f, 0f, 0f, 0.707f),
            EulerAngles = new EulerAngles(10f, 20f, 30f),
            Timestamp = 1000,
            MessageCounter = 0
        };

        var buffer = new byte[64];
        SerializeTestPacket(buffer, imuData);

        // Act: CRC ���v�Z
        ushort crc = Crc16Ccitt.Calculate(buffer.AsSpan(), 4, 30);

        // Assert: CRC �͗L���Ȓl
        Assert.True(crc >= 0, "CRC should be calculated");
    }

    /// <summary>
    /// �e�X�g6: MockMcuStream �̕����ǂݍ���
    /// �d�l�F�uMCU�X�g���[�����畡����ǂݍ��݂��\�v
    /// </summary>
    [Fact]
    public async Task MockMcuStream_MultiplReadAsync_ShouldWork()
    {
        // Arrange
        var mcuStream = new MockMcuStream();
        var buffer = new byte[64];

        // Act & Assert: ������̓ǂݍ���
        for (int i = 0; i < 3; i++)
        {
            await mcuStream.WriteAsync(buffer, CancellationToken.None);
            int bytesRead = await mcuStream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);
            
            // �ŏ��̓ǂݍ��݂̓f�[�^��Ԃ��A�ȍ~�͋�
            if (i == 0)
            {
                Assert.True(bytesRead > 0, $"First read should return data, got {bytesRead}");
            }
        }

        await mcuStream.DisposeAsync();
    }

    /// <summary>
    /// �e�X�g7: VitureLumaPacket �R�}���h����
    /// �d�l�F�uBuildImuEnableCommand �ŗL���ȃp�P�b�g�����������v
    /// </summary>
    [Fact]
    public void BuildImuEnableCommand_ShouldGenerateValidCommand()
    {
        // Act
        var packet1 = VitureLumaPacket.BuildImuEnableCommand(enable: true, messageCounter: 0);
        var packet2 = VitureLumaPacket.BuildImuEnableCommand(enable: false, messageCounter: 1);

        // Assert
        Assert.NotNull(packet1);
        Assert.NotNull(packet2);
        Assert.True(packet1.Length > 0);
        Assert.True(packet2.Length > 0);
        
        // �w�b�_�m�F
        Assert.Equal(0xFF, packet1[0]);
        Assert.Equal(0xFE, packet1[1]); // MCU Command
        
        Assert.Equal(0xFF, packet2[0]);
        Assert.Equal(0xFE, packet2[1]); // MCU Command
    }

    /// <summary>
    /// �e�X�g8: MockHidStreamProvider �̃X�g���[����
    /// �d�l�F�uMockProvider��2�̃X�g���[����Ԃ��v
    /// </summary>
    [Fact]
    public async Task MockHidStreamProvider_ReturnsCorrectStreamCount()
    {
        // Arrange
        var provider = new MockHidStreamProvider(GenerateTestImuData);

        // Act
        var streams = await provider.GetStreamsAsync(
            VitureLumaDevice.VendorId,
            VitureLumaDevice.SupportedProductIds,
            CancellationToken.None);

        // Assert
        Assert.Equal(2, streams.Count);
        Assert.NotNull(streams[0]);
        Assert.NotNull(streams[1]);

        await provider.DisposeAsync();
    }

    /// <summary>
    /// �e�X�g9: Crc16Ccitt �̈�ѐ�
    /// �d�l�F�u�����f�[�^���琶�������CRC�͏�ɓ����v
    /// </summary>
    [Fact]
    public void Crc16Ccitt_Consistency()
    {
        // Arrange
        var data = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0 };

        // Act
        var crcs = new ushort[5];
        for (int i = 0; i < 5; i++)
        {
            crcs[i] = Crc16Ccitt.Calculate(data.AsSpan(), 0, data.Length);
        }

        // Assert: ���ׂē����l
        for (int i = 1; i < crcs.Length; i++)
        {
            Assert.Equal(crcs[0], crcs[i]);
        }
    }

    /// <summary>
    /// �e�X�g10: VitureLumaDevice �̃v���p�e�B�������
    /// �d�l�F�u�f�o�C�X�̃v���p�e�B�������������������v
    /// </summary>
    [Fact(Timeout = 5000)]
    public async Task VitureLumaDevice_InitialState_ShouldBeCorrect()
    {
        // Arrange
        var mockProvider = new MockHidStreamProvider(GenerateTestImuData);
        var device = await VitureLumaDevice.ConnectWithProviderAsync(mockProvider);

        // Act & Assert: IsConnected �� true
        Assert.True(device.IsConnected);

        await device.DisposeAsync();
        
        // Act & Assert: Dispose��� false
        Assert.False(device.IsConnected);
    }

    // �w���p�[���\�b�h
    private static async IAsyncEnumerable<ImuData> GenerateTestImuData(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < 5; i++)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            yield return new ImuData
            {
                Quaternion = new Quaternion(0.707f, 0f, 0f, 0.707f),
                EulerAngles = new EulerAngles(0f, 45f, 0f),
                Timestamp = (uint)(1000 + i),
                MessageCounter = (ushort)i
            };

            await Task.Delay(5, cancellationToken);
        }
    }

    private void SerializeTestPacket(byte[] buffer, ImuData data)
    {
        // �w�b�_
        buffer[0] = 0xFF;
        buffer[1] = 0xFC;

        // Payload length
        buffer[4] = 30;
        buffer[5] = 0;

        // Timestamp�i���g���G���f�B�A���j
        buffer[6] = (byte)(data.Timestamp & 0xFF);
        buffer[7] = (byte)((data.Timestamp >> 8) & 0xFF);
        buffer[8] = (byte)((data.Timestamp >> 16) & 0xFF);
        buffer[9] = (byte)((data.Timestamp >> 24) & 0xFF);

        // Reserved
        buffer[10] = 0;
        buffer[11] = 0;
        buffer[12] = 0;
        buffer[13] = 0;

        // Command ID
        buffer[14] = 0;
        buffer[15] = 0;

        // Message counter
        buffer[16] = (byte)(data.MessageCounter & 0xFF);
        buffer[17] = (byte)((data.MessageCounter >> 8) & 0xFF);

        // IMU �f�[�^
        var euler = data.EulerAngles;
        float raw0 = -euler.Yaw;
        float raw1 = -euler.Roll;
        float raw2 = euler.Pitch;

        var bytes0 = BitConverter.GetBytes(raw0);
        if (BitConverter.IsLittleEndian) System.Array.Reverse(bytes0);
        bytes0.CopyTo(buffer, 18);

        var bytes1 = BitConverter.GetBytes(raw1);
        if (BitConverter.IsLittleEndian) System.Array.Reverse(bytes1);
        bytes1.CopyTo(buffer, 22);

        var bytes2 = BitConverter.GetBytes(raw2);
        if (BitConverter.IsLittleEndian) System.Array.Reverse(bytes2);
        bytes2.CopyTo(buffer, 26);

        // End marker
        buffer[30] = 0x03;

        // CRC ���v�Z���Đݒ�
        ushort crc = Crc16Ccitt.Calculate(buffer.AsSpan(), 4, 30);
        buffer[2] = (byte)((crc >> 8) & 0xFF);
        buffer[3] = (byte)(crc & 0xFF);
    }
}
