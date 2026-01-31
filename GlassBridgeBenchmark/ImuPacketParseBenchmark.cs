namespace GlassBridgeBenchmark;

using BenchmarkDotNet.Attributes;
using GlassBridge;
using GlassBridge.Internal;

/// <summary>
/// IMU パケット解析のベンチマーク
/// HID受信データからImuDataへの変換パフォーマンスを計測
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class ImuPacketParseBenchmark
{
    private byte[] _validImuPacket = null!;
    private byte[] _validImuPacketWithReportId = null!;
    private byte[] _invalidPacket = null!;

    [GlobalSetup]
    public void Setup()
    {
        // 有効なIMUパケット（64バイト）
        _validImuPacket = CreateValidImuPacket();

        // Report ID付きパケット（65バイト、HID読み取り時の形式）
        _validImuPacketWithReportId = new byte[65];
        _validImuPacketWithReportId[0] = 0x00; // Report ID
        Array.Copy(_validImuPacket, 0, _validImuPacketWithReportId, 1, 64);

        // 無効なパケット
        _invalidPacket = new byte[64];
        _invalidPacket[0] = 0xAA;
        _invalidPacket[1] = 0xBB;
    }

    /// <summary>
    /// 有効なIMUパケットを作成（テストデータ）
    /// </summary>
    private static byte[] CreateValidImuPacket()
    {
        var packet = new byte[64];

        // ヘッダ
        packet[0] = 0xFF;
        packet[1] = 0xFC; // IMU Data

        // Payload length (リトルエンディアン): 30バイト
        packet[4] = 30;
        packet[5] = 0;

        // Timestamp (リトルエンディアン): 任意の値
        packet[6] = 0x10;
        packet[7] = 0x27;
        packet[8] = 0x00;
        packet[9] = 0x00;

        // Message counter (リトルエンディアン)
        packet[16] = 0x01;
        packet[17] = 0x00;

        // Euler angles (ビッグエンディアン float32)
        // Roll: 10.5度
        WriteFloat32BigEndian(packet, 18, 10.5f);
        // Pitch: -5.2度
        WriteFloat32BigEndian(packet, 22, -5.2f);
        // Yaw: 45.0度
        WriteFloat32BigEndian(packet, 26, 45.0f);

        // End marker
        packet[35] = 0x03;

        // CRC計算（スキップ可能なのでダミー値）
        packet[2] = 0x00;
        packet[3] = 0x00;

        return packet;
    }

    private static void WriteFloat32BigEndian(byte[] buffer, int offset, float value)
    {
        var bytes = BitConverter.GetBytes(value);
        buffer[offset] = bytes[3];
        buffer[offset + 1] = bytes[2];
        buffer[offset + 2] = bytes[1];
        buffer[offset + 3] = bytes[0];
    }

    /// <summary>
    /// 標準的なIMUパケット解析（CRCスキップ）
    /// 実運用時の典型的なパス
    /// </summary>
    [Benchmark(Baseline = true)]
    public ImuData? ParseValidPacket_SkipCrc()
    {
        VitureLumaPacket.TryParseImuPacket(_validImuPacket.AsSpan(), out var data, skipCrcValidation: true);
        return data;
    }

    /// <summary>
    /// Report ID付きパケット解析（HID読み取り時の実際の形式）
    /// </summary>
    [Benchmark]
    public ImuData? ParseValidPacket_WithReportId()
    {
        VitureLumaPacket.TryParseImuPacket(_validImuPacketWithReportId.AsSpan(), out var data, skipCrcValidation: true);
        return data;
    }

    /// <summary>
    /// CRC検証付きパケット解析
    /// </summary>
    [Benchmark]
    public ImuData? ParseValidPacket_WithCrcValidation()
    {
        VitureLumaPacket.TryParseImuPacket(_validImuPacket.AsSpan(), out var data, skipCrcValidation: false);
        return data;
    }

    /// <summary>
    /// 無効なパケットの早期リジェクト
    /// </summary>
    [Benchmark]
    public bool ParseInvalidPacket_EarlyReject()
    {
        return VitureLumaPacket.TryParseImuPacket(_invalidPacket.AsSpan(), out _, skipCrcValidation: true);
    }
}
