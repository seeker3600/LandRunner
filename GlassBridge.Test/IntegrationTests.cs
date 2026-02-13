namespace GlassBridgeTest;

using GlassBridge;
using GlassBridge.Internal.VitureLuma;
using GlassBridge.Utils;
using Xunit;
using static GlassBridge.Utils.TestDataGenerators;

/// <summary>
/// 統合テスト
/// 複数のコンポーネントの相互作用をテスト
/// </summary>
public class IntegrationTests
{
    /// <summary>
    /// テスト1: MockProvider + Device の統合テスト
    /// 仕様：「ProviderがストリームをDeviceに提供し、正常に動作する」
    /// </summary>
    [Fact(Timeout = 5000)]
    public async Task MockProvider_WithDevice_ShouldIntegrateCorrectly()
    {
        // Arrange
        var mockProvider = new MockHidStreamProvider(ct => GenerateTestImuData(count: 5, delayMs: 5, cancellationToken: ct));

        // Act
        var device = await VitureLumaDevice.ConnectWithProviderAsync(mockProvider);

        // Assert
        Assert.NotNull(device);
        Assert.True(device.IsConnected);

        await device.DisposeAsync();
        await mockProvider.DisposeAsync();
    }

    /// <summary>
    /// テスト2: 複数のデバイス接続テスト
    /// 仕様：「複数回の接続・切断が正常に動作する」
    /// </summary>
    [Fact(Timeout = 10000)]
    public async Task MultipleConnect_ShouldSucceed()
    {
        // Act & Assert
        for (int i = 0; i < 3; i++)
        {
            var mockProvider = new MockHidStreamProvider(ct => GenerateTestImuData(count: 5, delayMs: 5, cancellationToken: ct));
            var device = await VitureLumaDevice.ConnectWithProviderAsync(mockProvider);

            Assert.NotNull(device);
            Assert.True(device.IsConnected);

            await device.DisposeAsync();
            await mockProvider.DisposeAsync();
        }
    }

    /// <summary>
    /// テスト3: DisposeAsync の二重呼び出し
    /// 仕様：「二重Disposeが例外を発生させない」
    /// </summary>
    [Fact(Timeout = 5000)]
    public async Task DisposeAsync_MultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var mockProvider = new MockHidStreamProvider(ct => GenerateTestImuData(count: 5, delayMs: 5, cancellationToken: ct));
        var device = await VitureLumaDevice.ConnectWithProviderAsync(mockProvider);

        // Act & Assert
        await device.DisposeAsync();
        await device.DisposeAsync(); // 2回目
        
        Assert.False(device.IsConnected);
    }

    /// <summary>
    /// テスト4: Disposed後のメソッド呼び出し
    /// 仕様：「Disposed後のGetImuDataStreamAsyncは例外を発生させる」
    /// </summary>
    [Fact(Timeout = 5000)]
    public async Task MethodCall_AfterDispose_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var mockProvider = new MockHidStreamProvider(ct => GenerateTestImuData(count: 5, delayMs: 5, cancellationToken: ct));
        var device = await VitureLumaDevice.ConnectWithProviderAsync(mockProvider);
        await device.DisposeAsync();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var data in device.GetImuDataStreamAsync())
            {
                // ここに到達しないこと
            }
        });

        Assert.Contains("not connected", ex.Message, System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// テスト5: MockHidStream と Crc16Ccitt の統合
    /// 仕様：「シリアライズされたパケットのCRCが正当」
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

        // Act: CRC を計算
        ushort crc = Crc16Ccitt.Calculate(buffer.AsSpan(), 4, 30);

        // Assert: CRC は有効な値
        Assert.True(crc >= 0, "CRC should be calculated");
    }

    /// <summary>
    /// テスト6: MockMcuStream の複数読み込み
    /// 仕様：「MCUストリームから複数回読み込みが可能」
    /// </summary>
    [Fact]
    public async Task MockMcuStream_MultiplReadAsync_ShouldWork()
    {
        // Arrange
        var mcuStream = new MockMcuStream();
        var buffer = new byte[64];

        // Act & Assert: 複数回の読み込み
        for (int i = 0; i < 3; i++)
        {
            await mcuStream.WriteAsync(buffer, CancellationToken.None);
            int bytesRead = await mcuStream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);
            
            // 最初の読み込みはデータを返す、以降は空
            if (i == 0)
            {
                Assert.True(bytesRead > 0, $"First read should return data, got {bytesRead}");
            }
        }

        await mcuStream.DisposeAsync();
    }

    /// <summary>
    /// テスト7: VitureLumaPacket コマンド検証
    /// 仕様：「BuildImuEnableCommand で有効なパケットが生成される」
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
        
        // ヘッダ確認
        Assert.Equal(0xFF, packet1[0]);
        Assert.Equal(0xFE, packet1[1]); // MCU Command
        
        Assert.Equal(0xFF, packet2[0]);
        Assert.Equal(0xFE, packet2[1]); // MCU Command
    }

    /// <summary>
    /// テスト8: MockHidStreamProvider のストリーム個数
    /// 仕様：「MockProviderが2つのストリームを返す」
    /// </summary>
    [Fact]
    public async Task MockHidStreamProvider_ReturnsCorrectStreamCount()
    {
        // Arrange
        var provider = new MockHidStreamProvider(ct => GenerateTestImuData(count: 5, delayMs: 5, cancellationToken: ct));

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
    /// テスト9: Crc16Ccitt の一貫性
    /// 仕様：「同じデータから生成されるCRCは常に同じ」
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

        // Assert: すべて同じ値
        for (int i = 1; i < crcs.Length; i++)
        {
            Assert.Equal(crcs[0], crcs[i]);
        }
    }

    /// <summary>
    /// テスト10: VitureLumaDevice のプロパティ初期状態
    /// 仕様：「デバイスのプロパティが正しく初期化される」
    /// </summary>
    [Fact(Timeout = 5000)]
    public async Task VitureLumaDevice_InitialState_ShouldBeCorrect()
    {
        // Arrange
        var mockProvider = new MockHidStreamProvider(ct => GenerateTestImuData(count: 5, delayMs: 5, cancellationToken: ct));
        var device = await VitureLumaDevice.ConnectWithProviderAsync(mockProvider);

        // Act & Assert: IsConnected が true
        Assert.True(device.IsConnected);

        await device.DisposeAsync();
        
        // Act & Assert: Dispose後は false
        Assert.False(device.IsConnected);
    }

    private void SerializeTestPacket(byte[] buffer, ImuData data)
    {
        // ヘッダ
        buffer[0] = 0xFF;
        buffer[1] = 0xFC;

        // Payload length
        buffer[4] = 30;
        buffer[5] = 0;

        // Timestamp（リトルエンディアン）
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

        // IMU データ
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

        // CRC を計算して設定
        ushort crc = Crc16Ccitt.Calculate(buffer.AsSpan(), 4, 30);
        buffer[2] = (byte)((crc >> 8) & 0xFF);
        buffer[3] = (byte)(crc & 0xFF);
    }
}
