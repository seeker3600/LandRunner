namespace GlassBridge.Internal;

/// <summary>
/// CRC-16-CCITT�v�Z���[�e�B���e�B
/// polynomial: 0x1021, initial value: 0xFFFF
/// </summary>
internal static class Crc16Ccitt
{
    private static readonly ushort[] CrcTable = GenerateCrcTable();

    /// <summary>
    /// CRC�e�[�u���𐶐�
    /// </summary>
    private static ushort[] GenerateCrcTable()
    {
        const ushort polynomial = 0x1021;
        var table = new ushort[256];

        for (int i = 0; i < 256; i++)
        {
            ushort crc = (ushort)(i << 8);
            for (int j = 0; j < 8; j++)
            {
                crc = (ushort)((crc << 1) ^ ((crc & 0x8000) != 0 ? polynomial : 0));
            }
            table[i] = crc;
        }

        return table;
    }

    /// <summary>
    /// CRC-16-CCITT���v�Z
    /// </summary>
    /// <param name="data">�v�Z�Ώۂ̃f�[�^</param>
    /// <param name="offset">�J�n�I�t�Z�b�g</param>
    /// <param name="length">�v�Z�Ώۂ̒���</param>
    /// <returns>CRC�l�i�r�b�O�G���f�B�A���j</returns>
    public static ushort Calculate(ReadOnlySpan<byte> data, int offset, int length)
    {
        ushort crc = 0xFFFF;

        for (int i = offset; i < offset + length && i < data.Length; i++)
        {
            byte index = (byte)((crc >> 8) ^ data[i]);
            crc = (ushort)((crc << 8) ^ CrcTable[index]);
        }

        return crc;
    }
}
