namespace GlassBridge;

/// <summary>
/// IMU（姿勢）データを表す構造体
/// </summary>
public record ImuData
{
    /// <summary>
    /// クォータニオン (w, x, y, z)
    /// </summary>
    public required Quaternion Quaternion { get; init; }

    /// <summary>
    /// オイラー角（度単位）: Roll, Pitch, Yaw
    /// </summary>
    public required EulerAngles EulerAngles { get; init; }

    /// <summary>
    /// パケットのタイムスタンプ
    /// </summary>
    public required uint Timestamp { get; init; }

    /// <summary>
    /// メッセージカウンター
    /// </summary>
    public required ushort MessageCounter { get; init; }
}

/// <summary>
/// クォータニオン表現 (w, x, y, z)
/// </summary>
public record Quaternion(float W, float X, float Y, float Z)
{
    /// <summary>
    /// 単位クォータニオン（回転なし）
    /// </summary>
    public static readonly Quaternion Identity = new(1.0f, 0.0f, 0.0f, 0.0f);

    /// <summary>
    /// クォータニオンの共役を計算
    /// </summary>
    public Quaternion Conjugate() => new(W, -X, -Y, -Z);

    /// <summary>
    /// 2つのクォータニオンを乗算（q1 * q2）
    /// </summary>
    public static Quaternion operator *(Quaternion q1, Quaternion q2)
    {
        var (w1, x1, y1, z1) = q1;
        var (w2, x2, y2, z2) = q2;

        return new Quaternion(
            w1 * w2 - x1 * x2 - y1 * y2 - z1 * z2,
            w1 * x2 + x1 * w2 + y1 * z2 - z1 * y2,
            w1 * y2 - x1 * z2 + y1 * w2 + z1 * x2,
            w1 * z2 + x1 * y2 - y1 * x2 + z1 * w2
        );
    }
}

/// <summary>
/// オイラー角表現（度単位）
/// </summary>
public record EulerAngles(float Roll, float Pitch, float Yaw);
