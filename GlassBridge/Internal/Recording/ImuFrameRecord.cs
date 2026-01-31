namespace GlassBridge.Internal.Recording;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// JSON Lines形式で保存するフレームレコード
/// </summary>
internal class ImuFrameRecord
{
    [JsonPropertyName("timestamp")]
    public uint Timestamp { get; set; }

    [JsonPropertyName("messageCounter")]
    public ushort MessageCounter { get; set; }

    [JsonPropertyName("quaternion")]
    public QuaternionRecord Quaternion { get; set; } = new();

    [JsonPropertyName("eulerAngles")]
    public EulerAnglesRecord EulerAngles { get; set; } = new();

    [JsonPropertyName("rawBytes")]
    public string RawBytes { get; set; } = string.Empty;

    /// <summary>
    /// ImuDataをフレームレコードに変換
    /// </summary>
    public static ImuFrameRecord FromImuData(ImuData data, byte[] rawBytes)
    {
        return new ImuFrameRecord
        {
            Timestamp = data.Timestamp,
            MessageCounter = data.MessageCounter,
            Quaternion = new QuaternionRecord
            {
                W = data.Quaternion.W,
                X = data.Quaternion.X,
                Y = data.Quaternion.Y,
                Z = data.Quaternion.Z
            },
            EulerAngles = new EulerAnglesRecord
            {
                Roll = data.EulerAngles.Roll,
                Pitch = data.EulerAngles.Pitch,
                Yaw = data.EulerAngles.Yaw
            },
            RawBytes = Convert.ToBase64String(rawBytes)
        };
    }

    /// <summary>
    /// フレームレコードをImuDataに変換
    /// </summary>
    public ImuData ToImuData()
    {
        return new ImuData
        {
            Timestamp = Timestamp,
            MessageCounter = MessageCounter,
            Quaternion = new Quaternion(Quaternion.W, Quaternion.X, Quaternion.Y, Quaternion.Z),
            EulerAngles = new EulerAngles(EulerAngles.Roll, EulerAngles.Pitch, EulerAngles.Yaw)
        };
    }

    /// <summary>
    /// JSONから単一行を解析
    /// </summary>
    public static ImuFrameRecord FromJsonLine(string jsonLine)
    {
        return JsonSerializer.Deserialize<ImuFrameRecord>(jsonLine)
            ?? throw new InvalidOperationException("Failed to deserialize frame record");
    }

    /// <summary>
    /// JSONに変換（改行なし）
    /// </summary>
    public string ToJsonLine()
    {
        return JsonSerializer.Serialize(this);
    }
}

/// <summary>
/// Quaternionのシリアライズ用レコード
/// </summary>
internal record QuaternionRecord
{
    [JsonPropertyName("w")]
    public float W { get; set; }

    [JsonPropertyName("x")]
    public float X { get; set; }

    [JsonPropertyName("y")]
    public float Y { get; set; }

    [JsonPropertyName("z")]
    public float Z { get; set; }
}

/// <summary>
/// EulerAnglesのシリアライズ用レコード
/// </summary>
internal record EulerAnglesRecord
{
    [JsonPropertyName("roll")]
    public float Roll { get; set; }

    [JsonPropertyName("pitch")]
    public float Pitch { get; set; }

    [JsonPropertyName("yaw")]
    public float Yaw { get; set; }
}
