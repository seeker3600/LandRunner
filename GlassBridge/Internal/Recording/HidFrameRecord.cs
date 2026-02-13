namespace GlassBridge.Internal.Recording;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// HID生データフレームのレコード（VitureLuma非依存）
/// JSON Lines形式で保存される単一フレーム
/// </summary>
internal sealed class HidFrameRecord
{
    /// <summary>
    /// レコードタイプ（常に "frame"）
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "frame";

    /// <summary>
    /// 記録時刻（ミリ秒単位のタイムスタンプ）
    /// </summary>
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    /// <summary>
    /// HID生バイト列（Base64エンコード）
    /// </summary>
    [JsonPropertyName("rawBytes")]
    public string RawBytes { get; set; } = string.Empty;

    /// <summary>
    /// HID生データからフレームレコードを作成
    /// </summary>
    /// <param name="rawBytes">HIDバイト列</param>
    /// <param name="timestamp">記録時刻（ミリ秒）</param>
    public static HidFrameRecord Create(byte[] rawBytes, long timestamp)
    {
        return new HidFrameRecord
        {
            Type = "frame",
            Timestamp = timestamp,
            RawBytes = Convert.ToBase64String(rawBytes)
        };
    }

    /// <summary>
    /// JSON文字列からフレームレコードを解析
    /// </summary>
    public static HidFrameRecord FromJson(string json)
    {
        return JsonSerializer.Deserialize<HidFrameRecord>(json)
            ?? throw new InvalidOperationException("Failed to deserialize HID frame record");
    }

    /// <summary>
    /// フレームレコードをJSON文字列に変換（改行なし）
    /// </summary>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this);
    }

    /// <summary>
    /// Base64エンコードされたバイト列をデコード
    /// </summary>
    public byte[] DecodeRawBytes()
    {
        return Convert.FromBase64String(RawBytes);
    }
}
