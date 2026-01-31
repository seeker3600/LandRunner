namespace GlassBridge.Internal;

/// <summary>
/// VITURE Luma�v���g�R���̃p�P�b�g�������[�e�B���e�B
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

    // �p�P�b�g�w�b�_
    public const byte HeaderByte0 = 0xFF;
    public const byte HeaderImuData = 0xFC;
    public const byte HeaderMcuAck = 0xFD;
    public const byte HeaderMcuCommand = 0xFE;

    /// <summary>
    /// IMU�f�[�^�p�P�b�g�����
    /// HID�ǂݎ��ł͐擪��Report ID (0x00) ���t�����Ƃ����邽�ߎ������o����
    /// </summary>
    /// <param name="buffer">���̓o�b�t�@</param>
    /// <param name="imuData">��͌���</param>
    /// <param name="skipCrcValidation">CRC���؂��X�L�b�v���邩�ǂ����i�f�t�H���g: false�j</param>
    public static bool TryParseImuPacket(ReadOnlySpan<byte> buffer, out ImuData? imuData, bool skipCrcValidation = false)
    {
        imuData = null;

        // Report ID �̌��o�ƃI�t�Z�b�g����
        // HID�ǂݎ��ł͐擪�� Report ID (0x00) ���t�����Ƃ�����
        int offset = 0;
        if (buffer.Length > 1 && buffer[0] == 0x00 && buffer[1] == HeaderByte0)
        {
            offset = 1;
        }

        var packet = buffer[offset..];

        // �o�b�t�@�T�C�Y�`�F�b�N
        if (packet.Length < PacketSize)
            return false;

        // �w�b�_�m�F
        if (packet[0] != HeaderByte0 || packet[1] != HeaderImuData)
            return false;

        // CRC���؁i�I�v�V���i���j
        // ���f�o�C�X�ł�CRC�v�Z���d�l�ƈقȂ�ꍇ�����邽�߁A�X�L�b�v�\
        if (!skipCrcValidation && !VerifyCrc(packet))
            return false;

        // Payload length���擾�i���g���G���f�B�A���j
        // payload_length �� offset 0x06 ���� End marker �܂ł̃o�C�g��
        ushort payloadLen = (ushort)(packet[LengthOffset] | (packet[LengthOffset + 1] << 8));
        
        // End marker �̈ʒu: TimestampOffset (0x06) + payloadLen - 1
        int endMarkerPos = TimestampOffset + payloadLen - 1;

        // End marker���m�F�i���݂���ꍇ�̂݌��؁A0x00�p�f�B���O�̏ꍇ�̓X�L�b�v�j
        // ���f�o�C�X�ł� End marker ���ȗ�����邱�Ƃ�����
        if (endMarkerPos > 0 && endMarkerPos < packet.Length)
        {
            byte endByte = packet[endMarkerPos];
            if (endByte != EndMarkerValue && endByte != 0x00)
                return false;
        }

        // �^�C���X�^���v���擾
        uint timestamp = (uint)(packet[TimestampOffset] | 
                               (packet[TimestampOffset + 1] << 8) |
                               (packet[TimestampOffset + 2] << 16) |
                               (packet[TimestampOffset + 3] << 24));

        // ���b�Z�[�W�J�E���^�[���擾�i���g���G���f�B�A���j
        ushort msgCounter = (ushort)(packet[MessageCounterOffset] | 
                                    (packet[MessageCounterOffset + 1] << 8));

        // �I�C���[�p���擾�i�r�b�O�G���f�B�A�� float32�j
        var euler = ExtractEulerAngles(packet);

        // �N�H�[�^�j�I���ɕϊ�
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
    /// CRC������
    /// CRC�v�Z�͈�: offset 0x04 �ȍ~�i�w�b�_��CRC�t�B�[���h���̂����O�j
    /// payload_length �� offset 0x06 ���� End marker �܂ł̃o�C�g��
    /// ����Čv�Z�͈͂� offset 0x04 ���� 2 + payload_length �o�C�g
    /// </summary>
    private static bool VerifyCrc(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < 6)
            return false;

        // �ۑ����ꂽCRC�i�r�b�O�G���f�B�A���j
        ushort storedCrc = (ushort)((buffer[CrcOffset] << 8) | buffer[CrcOffset + 1]);

        // Payload length���擾�ioffset 0x06 ���� End marker �܂ł̃o�C�g���j
        ushort payloadLen = (ushort)(buffer[LengthOffset] | (buffer[LengthOffset + 1] << 8));
        
        // CRC�v�Z�͈�: length field (2�o�C�g) + payload
        int crcDataLen = 2 + payloadLen;

        // CRC���Čv�Z�ioffset 0x04�ȍ~�j
        ushort calculatedCrc = Crc16Ccitt.Calculate(buffer, LengthOffset, crcDataLen);

        return storedCrc == calculatedCrc;
    }

    /// <summary>
    /// �o�b�t�@����I�C���[�p�𒊏o�i�r�b�O�G���f�B�A�� float32�j
    /// </summary>
    private static EulerAngles ExtractEulerAngles(ReadOnlySpan<byte> buffer)
    {
        // raw0, raw1, raw2 �͂��ꂼ��4�o�C�g�� float32�i�r�b�O�G���f�B�A���j
        // offset 0x12 (18), 0x16 (22), 0x1A (26)
        float raw0 = ReadBigEndianFloat(buffer, PayloadOffset);
        float raw1 = ReadBigEndianFloat(buffer, PayloadOffset + 4);
        float raw2 = ReadBigEndianFloat(buffer, PayloadOffset + 8);

        // ���}�b�s���O�iWebXR�����Ɋ�Â��j
        // yaw = -raw0, roll = -raw1, pitch = raw2
        float yaw = -raw0;
        float roll = -raw1;
        float pitch = raw2;

        return new EulerAngles(roll, pitch, yaw);
    }

    /// <summary>
    /// �r�b�O�G���f�B�A�� float32 ��ǂ�
    /// </summary>
    private static float ReadBigEndianFloat(ReadOnlySpan<byte> buffer, int offset)
    {
        if (offset + 4 > buffer.Length)
            return 0.0f;

        // �r�b�O�G���f�B�A����4�o�C�g�����g���G���f�B�A���ɕϊ����ēǂ�
        Span<byte> floatBytes = stackalloc byte[4];
        floatBytes[0] = buffer[offset + 3];
        floatBytes[1] = buffer[offset + 2];
        floatBytes[2] = buffer[offset + 1];
        floatBytes[3] = buffer[offset];

        return System.BitConverter.ToSingle(floatBytes);
    }

    /// <summary>
    /// �I�C���[�p�i�x�j���N�H�[�^�j�I���ɕϊ�
    /// </summary>
    private static Quaternion ConvertEulerToQuaternion(EulerAngles euler)
    {
        // �x�����W�A���ɕϊ�
        float toRad = (float)(System.Math.PI / 180.0);
        float roll = euler.Roll * toRad;
        float pitch = euler.Pitch * toRad;
        float yaw = euler.Yaw * toRad;

        // Yaw-Pitch-Roll���ł̍����iWebHID�����Ɋ�Â��j
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
    /// MCU IMU�L�����R�}���h�p�P�b�g���\�z
    /// </summary>
    public static byte[] BuildImuEnableCommand(bool enable, ushort messageCounter = 0)
    {
        var packet = new byte[PacketSize];

        // �w�b�_
        packet[0] = HeaderByte0;
        packet[1] = HeaderMcuCommand;

        // CRC: ��Ōv�Z

        // Payload length�i���g���G���f�B�A���j: header 2 + CRC 2 + length 2 + reserved 4 + cmd 2 + msg 2 + data 1 + end 1 = 16
        ushort payloadLen = 12; // length(2) + reserved(4) + cmd(2) + msg(2) + data(1) + end(1) = 12
        packet[LengthOffset] = (byte)(payloadLen & 0xFF);
        packet[LengthOffset + 1] = (byte)((payloadLen >> 8) & 0xFF);

        // Timestamp: 0�iMCU�R�}���h�ł͎g��Ȃ��j
        packet[TimestampOffset] = 0;
        packet[TimestampOffset + 1] = 0;
        packet[TimestampOffset + 2] = 0;
        packet[TimestampOffset + 3] = 0;

        // Reserved: 0���߁ialready 0�j

        // Command ID�i���g���G���f�B�A���j: 0x0015
        const ushort cmdId = 0x0015;
        packet[CommandIdOffset] = (byte)(cmdId & 0xFF);
        packet[CommandIdOffset + 1] = (byte)((cmdId >> 8) & 0xFF);

        // Message counter�i���g���G���f�B�A���j
        packet[MessageCounterOffset] = (byte)(messageCounter & 0xFF);
        packet[MessageCounterOffset + 1] = (byte)((messageCounter >> 8) & 0xFF);

        // Data: 0x01(enable) or 0x00(disable)
        packet[PayloadOffset] = enable ? (byte)0x01 : (byte)0x00;

        // End marker
        packet[PayloadOffset + 1] = EndMarkerValue;

        // CRC�v�Z�ioffset 0x04�ȍ~�j
        ushort crc = Crc16Ccitt.Calculate(packet, LengthOffset, payloadLen);
        packet[CrcOffset] = (byte)((crc >> 8) & 0xFF);
        packet[CrcOffset + 1] = (byte)(crc & 0xFF);

        return packet;
    }
}
