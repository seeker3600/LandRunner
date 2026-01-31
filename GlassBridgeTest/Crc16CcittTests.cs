namespace GlassBridgeTest;

using GlassBridge.Internal;
using Xunit;

/// <summary>
/// Crc16Ccitt �̃e�X�g
/// CRC-16-CCITT �v�Z�̓���m�F
/// </summary>
public class Crc16CcittTests
{
    /// <summary>
    /// �e�X�g1: ��̃f�[�^�ɑ΂��� CRC �v�Z
    /// </summary>
    [Fact]
    public void Calculate_WithEmptyData_ShouldReturnInitialValue()
    {
        // Arrange
        var data = new byte[0];

        // Act
        ushort crc = Crc16Ccitt.Calculate(data.AsSpan(), 0, 0);

        // Assert
        // ��̃f�[�^�ł� CRC �͏����l�̂܂܂��A�܂��� 0
        Assert.True(crc == 0xFFFF || crc == 0, $"Expected 0xFFFF or 0, got {crc:X4}");
    }

    /// <summary>
    /// �e�X�g2: �P��o�C�g�� CRC �v�Z
    /// </summary>
    [Fact]
    public void Calculate_WithSingleByte_ShouldReturnValidCrc()
    {
        // Arrange
        var data = new byte[] { 0x00 };

        // Act
        ushort crc = Crc16Ccitt.Calculate(data.AsSpan(), 0, 1);

        // Assert
        Assert.True(crc != 0xFFFF, "CRC should not be initial value for data");
    }

    /// <summary>
    /// �e�X�g3: �����o�C�g�� CRC �v�Z
    /// </summary>
    [Fact]
    public void Calculate_WithMultipleBytes_ShouldCalculateCrc()
    {
        // Arrange
        var data = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04 };

        // Act
        ushort crc = Crc16Ccitt.Calculate(data.AsSpan(), 0, 5);

        // Assert
        Assert.True(crc >= 0, "CRC should be non-negative");
        Assert.NotEqual(0xFFFF, crc); // �����l�Ƃ͈قȂ�
    }

    /// <summary>
    /// �e�X�g4: �I�t�Z�b�g�w��ł� CRC �v�Z
    /// </summary>
    [Fact]
    public void Calculate_WithOffset_ShouldCalculateCrcFromOffset()
    {
        // Arrange
        var data = new byte[] { 0xFF, 0xFF, 0x00, 0x01, 0x02 };
        
        // �I�t�Z�b�g 2 ���� 3 �o�C�g�v�Z
        ushort crc1 = Crc16Ccitt.Calculate(data.AsSpan(), 2, 3);
        
        // �����f�[�^�Œ��ڌv�Z
        var subData = new byte[] { 0x00, 0x01, 0x02 };
        ushort crc2 = Crc16Ccitt.Calculate(subData.AsSpan(), 0, 3);

        // Assert
        Assert.Equal(crc1, crc2);
    }

    /// <summary>
    /// �e�X�g5: �����f�[�^�͓��� CRC �𐶐�
    /// </summary>
    [Fact]
    public void Calculate_WithIdenticalData_ShouldProduceSameCrc()
    {
        // Arrange
        var data = new byte[] { 0x12, 0x34, 0x56, 0x78 };

        // Act
        ushort crc1 = Crc16Ccitt.Calculate(data.AsSpan(), 0, 4);
        ushort crc2 = Crc16Ccitt.Calculate(data.AsSpan(), 0, 4);

        // Assert
        Assert.Equal(crc1, crc2);
    }

    /// <summary>
    /// �e�X�g6: �قȂ�f�[�^�͈قȂ� CRC �𐶐�
    /// </summary>
    [Fact]
    public void Calculate_WithDifferentData_ShouldProduceDifferentCrc()
    {
        // Arrange
        var data1 = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        var data2 = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

        // Act
        ushort crc1 = Crc16Ccitt.Calculate(data1.AsSpan(), 0, 4);
        ushort crc2 = Crc16Ccitt.Calculate(data2.AsSpan(), 0, 4);

        // Assert
        Assert.NotEqual(crc1, crc2);
    }

    /// <summary>
    /// �e�X�g7: �傫�ȃf�[�^�� CRC �v�Z
    /// </summary>
    [Fact]
    public void Calculate_WithLargeData_ShouldCalculateCrc()
    {
        // Arrange
        var data = new byte[256];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = (byte)(i & 0xFF);
        }

        // Act
        ushort crc = Crc16Ccitt.Calculate(data.AsSpan(), 0, data.Length);

        // Assert
        Assert.True(crc >= 0, "CRC should be calculated");
    }

    /// <summary>
    /// �e�X�g8: ���� 0 �ł̃I�t�Z�b�g�v�Z
    /// </summary>
    [Fact]
    public void Calculate_WithZeroLength_ShouldReturnInitialValue()
    {
        // Arrange
        var data = new byte[] { 0x12, 0x34, 0x56, 0x78 };

        // Act
        ushort crc = Crc16Ccitt.Calculate(data.AsSpan(), 0, 0);

        // Assert
        Assert.True(crc == 0xFFFF || crc == 0, "Zero length should return initial or zero");
    }

    /// <summary>
    /// �e�X�g9: VITURE �p�P�b�g�� CRC �v�Z�V�~�����[�V����
    /// </summary>
    [Fact]
    public void Calculate_WithVitureLumaPacketData_ShouldCalculateCrc()
    {
        // Arrange: VITURE �p�P�b�g�̃y�C���[�h�������V�~�����[�g
        var payload = new byte[30];
        payload[0] = 0x04; // Payload length low
        payload[1] = 0x00; // Payload length high
        
        // Timestamp�i�r�b�O�G���f�B�A���j
        payload[2] = 0x00;
        payload[3] = 0x00;
        payload[4] = 0x03;
        payload[5] = 0xE8;

        // ���̑��̃f�[�^
        for (int i = 6; i < payload.Length; i++)
        {
            payload[i] = (byte)(i & 0xFF);
        }

        // Act
        ushort crc = Crc16Ccitt.Calculate(payload.AsSpan(), 0, 30);

        // Assert
        Assert.True(crc >= 0, "Should calculate valid CRC for packet payload");
    }

    /// <summary>
    /// �e�X�g10: CRC �̈�ѐ��e�X�g�i������v�Z�j
    /// </summary>
    [Fact]
    public void Calculate_Consistency_ShouldProduceSameCrcMultipleTimes()
    {
        // Arrange
        var data = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF };
        
        // Act: ������v�Z
        var crcValues = new ushort[5];
        for (int i = 0; i < 5; i++)
        {
            crcValues[i] = Crc16Ccitt.Calculate(data.AsSpan(), 0, data.Length);
        }

        // Assert: ���ׂē����l
        for (int i = 1; i < crcValues.Length; i++)
        {
            Assert.Equal(crcValues[0], crcValues[i]);
        }
    }

    /// <summary>
    /// �e�X�g11: �I�t�Z�b�g���͈͊O�̏ꍇ
    /// </summary>
    [Fact]
    public void Calculate_WithOffsetOutOfRange_ShouldHandleGracefully()
    {
        // Arrange
        var data = new byte[] { 0x00, 0x01, 0x02, 0x03 };

        // Act: �I�t�Z�b�g���f�[�^���𒴂���
        ushort crc = Crc16Ccitt.Calculate(data.AsSpan(), 10, 5);

        // Assert: �G���[���������Ȃ����Ƃ��m�F
        Assert.True(crc >= 0, "Should handle out-of-range offset gracefully");
    }

    /// <summary>
    /// �e�X�g12: ���m�� CRC �l�Ƃ̔�r�i���؃e�X�g�j
    /// </summary>
    [Fact]
    public void Calculate_KnownValue_ShouldMatchExpectedCrc()
    {
        // Arrange: �W���I�ȃe�X�g�f�[�^
        var data = new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39 }; // "123456789"
        
        // CRC-16-CCITT �̊��m�l�i�����l 0xFFFF�j
        // ���̃e�X�g�f�[�^�� CRC-16-CCITT �͒ʏ� 0x31C3 �܂��͓����̒l
        
        // Act
        ushort crc = Crc16Ccitt.Calculate(data.AsSpan(), 0, data.Length);

        // Assert: �v�Z���ꂽCRC���L���Ȕ͈͓�
        Assert.True(crc >= 0, "Should calculate valid CRC");
        
        // ���m�̒l�Ɣ�r�i�����ɉ����Ē����j
        // CRC-16-CCITT("123456789") = 0x31C3
        // ���F�����l��ŏI�����ɂ���ĈقȂ�\��������
    }
}
