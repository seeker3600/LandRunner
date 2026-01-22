namespace GlassBridge.Internal;

/// <summary>
/// VITURE Lumaプロトコルのパケット処理ユーティリティ
/// </summary>
internal static class VitureLumaPacket
{
    public const int PacketSize = 64;
    public const int HeaderSize = 2;
    public const int CrcOffset = 2;
    public const int LengthOffset = 4;
    public const int TimestampOffset = 6;
    public const int CommandIdOffset = 14;
    public const int MessageCounterOffset = 16;
    public const int PayloadOffset = 18;
    public const int EndMarkerValue = 0x03;

    // パケットヘッダ
    public const byte HeaderByte0 = 0xFF;
    public const byte HeaderImuData = 0xFC;
    public const byte HeaderMcuAck = 0xFD;
    public const byte HeaderMcuCommand = 0xFE;

    /// <summary>
    /// IMUデータパケットを解析
    /// </summary>
    public static bool TryParseImuPacket(ReadOnlySpan<byte> buffer, out ImuData? imuData)
    {
        imuData = null;

        // バッファサイズチェック
        if (buffer.Length < PacketSize)
            return false;

        // ヘッダ確認
        if (buffer[0] != HeaderByte0 || buffer[1] != HeaderImuData)
            return false;

        // CRC検証
        if (!VerifyCrc(buffer))
            return false;

        // Payload lengthを取得（リトルエンディアン）
        ushort payloadLen = (ushort)(buffer[LengthOffset] | (buffer[LengthOffset + 1] << 8));
        int totalLen = LengthOffset + payloadLen;

        // End markerを確認
        if (totalLen > 0 && totalLen <= buffer.Length && buffer[totalLen - 1] != EndMarkerValue)
            return false;

        // タイムスタンプを取得
        uint timestamp = (uint)(buffer[TimestampOffset] | 
                               (buffer[TimestampOffset + 1] << 8) |
                               (buffer[TimestampOffset + 2] << 16) |
                               (buffer[TimestampOffset + 3] << 24));

        // メッセージカウンターを取得（リトルエンディアン）
        ushort msgCounter = (ushort)(buffer[MessageCounterOffset] | 
                                    (buffer[MessageCounterOffset + 1] << 8));

        // オイラー角を取得（ビッグエンディアン float32）
        var euler = ExtractEulerAngles(buffer);

        // クォータニオンに変換
        var quat = ConvertEulerToQuaternion(euler);

        imuData = new ImuData
        {
            Quaternion = quat,
            EulerAngles = euler,
            Timestamp = timestamp,
            MessageCounter = msgCounter
        };

        return true;
    }

    /// <summary>
    /// CRCを検証
    /// </summary>
    private static bool VerifyCrc(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < 6)
            return false;

        // 保存されたCRC（ビッグエンディアン）
        ushort storedCrc = (ushort)((buffer[CrcOffset] << 8) | buffer[CrcOffset + 1]);

        // Payload lengthを取得
        ushort payloadLen = (ushort)(buffer[LengthOffset] | (buffer[LengthOffset + 1] << 8));
        int dataLen = payloadLen;

        // CRCを再計算（offset 0x04以降）
        ushort calculatedCrc = Crc16Ccitt.Calculate(buffer, LengthOffset, dataLen);

        return storedCrc == calculatedCrc;
    }

    /// <summary>
    /// バッファからオイラー角を抽出（ビッグエンディアン float32）
    /// </summary>
    private static EulerAngles ExtractEulerAngles(ReadOnlySpan<byte> buffer)
    {
        // raw0, raw1, raw2 はそれぞれ4バイトの float32（ビッグエンディアン）
        // offset 0x12 (18), 0x16 (22), 0x1A (26)
        float raw0 = ReadBigEndianFloat(buffer, PayloadOffset);
        float raw1 = ReadBigEndianFloat(buffer, PayloadOffset + 4);
        float raw2 = ReadBigEndianFloat(buffer, PayloadOffset + 8);

        // 軸マッピング（WebXR実装に基づく）
        // yaw = -raw0, roll = -raw1, pitch = raw2
        float yaw = -raw0;
        float roll = -raw1;
        float pitch = raw2;

        return new EulerAngles(roll, pitch, yaw);
    }

    /// <summary>
    /// ビッグエンディアン float32 を読む
    /// </summary>
    private static float ReadBigEndianFloat(ReadOnlySpan<byte> buffer, int offset)
    {
        if (offset + 4 > buffer.Length)
            return 0.0f;

        // ビッグエンディアンの4バイトをリトルエンディアンに変換して読む
        Span<byte> floatBytes = stackalloc byte[4];
        floatBytes[0] = buffer[offset + 3];
        floatBytes[1] = buffer[offset + 2];
        floatBytes[2] = buffer[offset + 1];
        floatBytes[3] = buffer[offset];

        return System.BitConverter.ToSingle(floatBytes);
    }

    /// <summary>
    /// オイラー角（度）をクォータニオンに変換
    /// </summary>
    private static Quaternion ConvertEulerToQuaternion(EulerAngles euler)
    {
        // 度をラジアンに変換
        float toRad = (float)(System.Math.PI / 180.0);
        float roll = euler.Roll * toRad;
        float pitch = euler.Pitch * toRad;
        float yaw = euler.Yaw * toRad;

        // Yaw-Pitch-Roll順での合成（WebHID実装に基づく）
        float cr = (float)System.Math.Cos(roll / 2.0f);
        float sr = (float)System.Math.Sin(roll / 2.0f);
        float cp = (float)System.Math.Cos(pitch / 2.0f);
        float sp = (float)System.Math.Sin(pitch / 2.0f);
        float cy = (float)System.Math.Cos(yaw / 2.0f);
        float sy = (float)System.Math.Sin(yaw / 2.0f);

        float w = cy * cp * cr + sy * sp * sr;
        float x = cy * cp * sr - sy * sp * cr;
        float y = cy * sp * cr + sy * cp * sr;
        float z = sy * cp * cr - cy * sp * sr;

        return new Quaternion(w, x, y, z);
    }

    /// <summary>
    /// MCU IMU有効化コマンドパケットを構築
    /// </summary>
    public static byte[] BuildImuEnableCommand(bool enable, ushort messageCounter = 0)
    {
        var packet = new byte[PacketSize];

        // ヘッダ
        packet[0] = HeaderByte0;
        packet[1] = HeaderMcuCommand;

        // CRC: 後で計算

        // Payload length（リトルエンディアン）: header 2 + CRC 2 + length 2 + reserved 4 + cmd 2 + msg 2 + data 1 + end 1 = 16
        ushort payloadLen = 12; // length(2) + reserved(4) + cmd(2) + msg(2) + data(1) + end(1) = 12
        packet[LengthOffset] = (byte)(payloadLen & 0xFF);
        packet[LengthOffset + 1] = (byte)((payloadLen >> 8) & 0xFF);

        // Timestamp: 0（MCUコマンドでは使わない）
        packet[TimestampOffset] = 0;
        packet[TimestampOffset + 1] = 0;
        packet[TimestampOffset + 2] = 0;
        packet[TimestampOffset + 3] = 0;

        // Reserved: 0埋め（already 0）

        // Command ID（リトルエンディアン）: 0x0015
        const ushort cmdId = 0x0015;
        packet[CommandIdOffset] = (byte)(cmdId & 0xFF);
        packet[CommandIdOffset + 1] = (byte)((cmdId >> 8) & 0xFF);

        // Message counter（リトルエンディアン）
        packet[MessageCounterOffset] = (byte)(messageCounter & 0xFF);
        packet[MessageCounterOffset + 1] = (byte)((messageCounter >> 8) & 0xFF);

        // Data: 0x01(enable) or 0x00(disable)
        packet[PayloadOffset] = enable ? (byte)0x01 : (byte)0x00;

        // End marker
        packet[PayloadOffset + 1] = EndMarkerValue;

        // CRC計算（offset 0x04以降）
        ushort crc = Crc16Ccitt.Calculate(packet, LengthOffset, payloadLen);
        packet[CrcOffset] = (byte)((crc >> 8) & 0xFF);
        packet[CrcOffset + 1] = (byte)(crc & 0xFF);

        return packet;
    }
}
