namespace GlassBridge;

/// <summary>
/// IMU�i�p���j�f�[�^��\���\����
/// </summary>
public record ImuData
{
    /// <summary>
    /// �N�H�[�^�j�I�� (w, x, y, z)
    /// </summary>
    public required Quaternion Quaternion { get; init; }

    /// <summary>
    /// �I�C���[�p�i�x�P�ʁj: Roll, Pitch, Yaw
    /// </summary>
    public required EulerAngles EulerAngles { get; init; }

    /// <summary>
    /// �p�P�b�g�̃^�C���X�^���v
    /// </summary>
    public required uint Timestamp { get; init; }

    /// <summary>
    /// ���b�Z�[�W�J�E���^�[
    /// </summary>
    public required ushort MessageCounter { get; init; }
}

/// <summary>
/// �N�H�[�^�j�I���\�� (w, x, y, z)
/// </summary>
public record Quaternion(float W, float X, float Y, float Z)
{
    /// <summary>
    /// �P�ʃN�H�[�^�j�I���i��]�Ȃ��j
    /// </summary>
    public static readonly Quaternion Identity = new(1.0f, 0.0f, 0.0f, 0.0f);

    /// <summary>
    /// �N�H�[�^�j�I���̋������v�Z
    /// </summary>
    public Quaternion Conjugate() => new(W, -X, -Y, -Z);

    /// <summary>
    /// 2�̃N�H�[�^�j�I������Z�iq1 * q2�j
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
/// �I�C���[�p�\���i�x�P�ʁj
/// </summary>
public record EulerAngles(float Roll, float Pitch, float Yaw);
