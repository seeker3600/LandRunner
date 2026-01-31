namespace GlassBridge.Internal;

/// <summary>
/// CRC-16-CCITT計算ユーティリティ
/// polynomial: 0x1021, initial value: 0xFFFF
/// </summary>
internal static class Crc16Ccitt
{
    private static readonly ushort[] CrcTable = GenerateCrcTable();

    /// <summary>
    /// CRCテーブルを生成
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
    /// CRC-16-CCITTを計算
    /// </summary>
    /// <param name="data">計算対象のデータ</param>
    /// <param name="offset">開始オフセット</param>
    /// <param name="length">計算対象の長さ</param>
    /// <returns>CRC値（ビッグエンディアン）</returns>
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
