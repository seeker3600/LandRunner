namespace GlassBridgeBenchmark;

using BenchmarkDotNet.Attributes;
using GlassBridge;
using GlassBridge.Internal;

/// <summary>
/// IMU �p�P�b�g��͂̃x���`�}�[�N
/// HID��M�f�[�^����ImuData�ւ̕ϊ��p�t�H�[�}���X���v��
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
        // �L����IMU�p�P�b�g�i64�o�C�g�j
        _validImuPacket = CreateValidImuPacket();

        // Report ID�t���p�P�b�g�i65�o�C�g�AHID�ǂݎ�莞�̌`���j
        _validImuPacketWithReportId = new byte[65];
        _validImuPacketWithReportId[0] = 0x00; // Report ID
        Array.Copy(_validImuPacket, 0, _validImuPacketWithReportId, 1, 64);

        // �����ȃp�P�b�g
        _invalidPacket = new byte[64];
        _invalidPacket[0] = 0xAA;
        _invalidPacket[1] = 0xBB;
    }

    /// <summary>
    /// �L����IMU�p�P�b�g���쐬�i�e�X�g�f�[�^�j
    /// </summary>
    private static byte[] CreateValidImuPacket()
    {
        var packet = new byte[64];

        // �w�b�_
        packet[0] = 0xFF;
        packet[1] = 0xFC; // IMU Data

        // Payload length (���g���G���f�B�A��): 30�o�C�g
        packet[4] = 30;
        packet[5] = 0;

        // Timestamp (���g���G���f�B�A��): �C�ӂ̒l
        packet[6] = 0x10;
        packet[7] = 0x27;
        packet[8] = 0x00;
        packet[9] = 0x00;

        // Message counter (���g���G���f�B�A��)
        packet[16] = 0x01;
        packet[17] = 0x00;

        // Euler angles (�r�b�O�G���f�B�A�� float32)
        // Roll: 10.5�x
        WriteFloat32BigEndian(packet, 18, 10.5f);
        // Pitch: -5.2�x
        WriteFloat32BigEndian(packet, 22, -5.2f);
        // Yaw: 45.0�x
        WriteFloat32BigEndian(packet, 26, 45.0f);

        // End marker
        packet[35] = 0x03;

        // CRC�v�Z�i�X�L�b�v�\�Ȃ̂Ń_�~�[�l�j
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
    /// Report ID�t���p�P�b�g��́iHID�ǂݎ�莞�̎��ۂ̌`���j
    /// </summary>
    [Benchmark]
    public ImuData? ParseValidPacket()
    {
        VitureLumaPacket.TryParseImuPacket(_validImuPacketWithReportId.AsSpan(), out var data, skipCrcValidation: true);
        return data;
    }
}
