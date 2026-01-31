namespace GlassBridge.Internal.Recording;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// IMU記録セッションのメタデータを管理
/// </summary>
internal class ImuRecordingSession
{
    [JsonPropertyName("recordedAt")]
    public string RecordedAt { get; set; } = string.Empty;

    [JsonPropertyName("frameCount")]
    public int FrameCount { get; set; }

    [JsonPropertyName("sampleRate")]
    public int SampleRate { get; set; } = 100;

    [JsonPropertyName("format")]
    public string Format { get; set; } = "jsonl";

    /// <summary>
    /// 新しいセッションを作成
    /// </summary>
    public static ImuRecordingSession CreateNew(int frameCount = 0, int sampleRate = 100)
    {
        return new ImuRecordingSession
        {
            RecordedAt = DateTime.UtcNow.ToString("O"),
            FrameCount = frameCount,
            SampleRate = sampleRate,
            Format = "jsonl"
        };
    }

    /// <summary>
    /// メタデータをJSONで取得
    /// </summary>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// JSONからメタデータを復元
    /// </summary>
    public static ImuRecordingSession FromJson(string json)
    {
        return JsonSerializer.Deserialize<ImuRecordingSession>(json) 
            ?? throw new InvalidOperationException("Failed to deserialize metadata");
    }
}
