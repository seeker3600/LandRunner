namespace GlassBridge.Internal.Recording;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// HID記録セッションのメタデータ（単一ファイルの1行目）
/// </summary>
internal sealed class HidRecordingMetadata
{
    /// <summary>
    /// レコードタイプ（常に "metadata"）
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "metadata";

    /// <summary>
    /// 記録開始時刻（ISO 8601形式）
    /// </summary>
    [JsonPropertyName("recordedAt")]
    public string RecordedAt { get; set; } = string.Empty;

    /// <summary>
    /// フォーマットバージョン
    /// </summary>
    [JsonPropertyName("formatVersion")]
    public int FormatVersion { get; set; } = 2;

    /// <summary>
    /// ストリーム数
    /// </summary>
    [JsonPropertyName("streamCount")]
    public int StreamCount { get; set; }

    /// <summary>
    /// 新しいメタデータを作成
    /// </summary>
    public static HidRecordingMetadata Create(int streamCount)
    {
        return new HidRecordingMetadata
        {
            Type = "metadata",
            RecordedAt = DateTime.UtcNow.ToString("O"),
            FormatVersion = 2,
            StreamCount = streamCount
        };
    }

    /// <summary>
    /// JSON文字列からメタデータを解析
    /// </summary>
    public static HidRecordingMetadata FromJson(string json)
    {
        return JsonSerializer.Deserialize<HidRecordingMetadata>(json)
            ?? throw new InvalidOperationException("Failed to deserialize HID recording metadata");
    }

    /// <summary>
    /// メタデータをJSON文字列に変換（改行なし）
    /// </summary>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this);
    }
}
