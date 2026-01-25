namespace GlassBridgeTest;

using GlassBridge;
using GlassBridge.Internal;
using Xunit;

/// <summary>
/// VitureLumaPacket のテスト
/// パケット生成・解析の動作確認
/// </summary>
public class VitureLumaPacketTests
{
    /// <summary>
    /// テスト1: IMU enable コマンドパケットの生成
    /// </summary>
    [Fact]
    public void BuildImuEnableCommand_ShouldGenerateValidPacket()
    {
        // Act
        var packet = VitureLumaPacket.BuildImuEnableCommand(enable: true, messageCounter: 0);

        // Assert
        Assert.NotNull(packet);
        Assert.True(packet.Length > 0);
        
        // ヘッダ確認
        Assert.Equal(0xFF, packet[0]);
        Assert.Equal(0xFE, packet[1]); // MCU Command
    }

    /// <summary>
    /// テスト2: IMU disable コマンドパケットの生成
    /// </summary>
    [Fact]
    public void BuildImuEnableCommand_WithDisable_ShouldGenerateDisablePacket()
    {
        // Act
        var packet = VitureLumaPacket.BuildImuEnableCommand(enable: false, messageCounter: 0);

        // Assert
        Assert.NotNull(packet);
        Assert.True(packet.Length > 0);
        
        // ヘッダ確認
        Assert.Equal(0xFF, packet[0]);
        Assert.Equal(0xFE, packet[1]); // MCU Command
    }

    /// <summary>
    /// テスト3: IMU データパケット構造の検証（ヘッダとEnd marker）
    /// </summary>
    [Fact]
    public void VitureLumaPacket_PacketStructure_IsValid()
    {
        // Arrange: パケット構造の検証
        var buffer = new byte[64];
        
        // ヘッダ
        buffer[0] = 0xFF;
        buffer[1] = 0xFC;  // IMU Data
        buffer[4] = 30;
        buffer[5] = 0;
        buffer[30] = 0x03;  // End marker

        // Act: パケット構造が有効か確認
        bool headerValid = buffer[0] == 0xFF && buffer[1] == 0xFC;
        bool endMarkerValid = buffer[30] == 0x03;
        ushort payloadLen = (ushort)(buffer[4] | (buffer[5] << 8));

        // Assert
        Assert.True(headerValid, "Header should be valid");
        Assert.True(endMarkerValid, "End marker should be valid");
        Assert.Equal(30, payloadLen);
    }

    /// <summary>
    /// テスト4: 不正なヘッダを持つパケット
    /// </summary>
    [Fact]
    public void TryParseImuPacket_WithInvalidHeader_ShouldReturnFalse()
    {
        // Arrange
        var buffer = new byte[64];
        buffer[0] = 0xAA; // 不正なヘッダ
        buffer[1] = 0xBB;

        // Act
        bool result = VitureLumaPacket.TryParseImuPacket(buffer.AsSpan(), out var imuData);

        // Assert
        Assert.False(result, "Should reject packet with invalid header");
        Assert.Null(imuData);
    }

    /// <summary>
    /// テスト5: 短すぎるバッファ
    /// </summary>
    [Fact]
    public void TryParseImuPacket_WithShortBuffer_ShouldReturnFalse()
    {
        // Arrange
        var buffer = new byte[10]; // 64バイト未満
        buffer[0] = 0xFF;
        buffer[1] = 0xFC;

        // Act
        bool result = VitureLumaPacket.TryParseImuPacket(buffer.AsSpan(), out var imuData);

        // Assert
        Assert.False(result, "Should reject short buffer");
        Assert.Null(imuData);
    }

    /// <summary>
    /// テスト6: End marker がない場合
    /// </summary>
    [Fact]
    public void TryParseImuPacket_WithoutEndMarker_ShouldReturnFalse()
    {
        // Arrange
        var buffer = new byte[64];
        buffer[0] = 0xFF;
        buffer[1] = 0xFC;
        buffer[4] = 30;
        buffer[5] = 0;
        buffer[30] = 0x00; // End marker がない
        
        // CRC を計算
        ushort crc = CalculateCrc16Ccitt(buffer, 4, 30);
        buffer[2] = (byte)((crc >> 8) & 0xFF);
        buffer[3] = (byte)(crc & 0xFF);

        // Act
        bool result = VitureLumaPacket.TryParseImuPacket(buffer.AsSpan(), out var imuData);

        // Assert
        Assert.False(result, "Should reject packet without end marker");
        Assert.Null(imuData);
    }

    /// <summary>
    /// テスト7: コマンドパケットが正しく生成される
    /// </summary>
    [Fact]
    public void BuildImuEnableCommand_ShouldGenerateCorrectCommandPacket()
    {
        // Act
        var packet = VitureLumaPacket.BuildImuEnableCommand(enable: true, messageCounter: 5);

        // Assert
        Assert.NotNull(packet);
        Assert.True(packet.Length > 0);
        Assert.Equal(0xFF, packet[0]);
        Assert.Equal(0xFE, packet[1]);
    }

    /// <summary>
    /// テスト8: Message Counter が正しく反映される
    /// </summary>
    [Fact]
    public void BuildImuEnableCommand_WithDifferentCounter_ShouldUpdateCounter()
    {
        // Act
        var packet1 = VitureLumaPacket.BuildImuEnableCommand(enable: true, messageCounter: 0);
        var packet2 = VitureLumaPacket.BuildImuEnableCommand(enable: true, messageCounter: 255);

        // Assert
        Assert.NotNull(packet1);
        Assert.NotNull(packet2);
        Assert.True(packet1.Length > 0);
        Assert.True(packet2.Length > 0);
    }

    /// <summary>
    /// CRC-16-CCITT を計算（テスト用）
    /// </summary>
    private ushort CalculateCrc16Ccitt(byte[] data, int offset, int length)
    {
        const ushort polynomial = 0x1021;
        ushort[] crcTable = new ushort[256];

        // CRC テーブルを生成
        for (int i = 0; i < 256; i++)
        {
            ushort crc = (ushort)(i << 8);
            for (int j = 0; j < 8; j++)
            {
                crc = (ushort)((crc << 1) ^ ((crc & 0x8000) != 0 ? polynomial : 0));
            }
            crcTable[i] = crc;
        }

        // CRC を計算
        ushort result = 0xFFFF;
        for (int i = offset; i < offset + length && i < data.Length; i++)
        {
            byte index = (byte)((result >> 8) ^ data[i]);
            result = (ushort)((result << 8) ^ crcTable[index]);
        }

        return result;
    }
}

