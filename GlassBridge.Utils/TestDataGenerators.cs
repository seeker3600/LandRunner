namespace GlassBridge.Utils;

using System.Runtime.CompilerServices;
using GlassBridge;

/// <summary>
/// テスト用のIMUデータ生成ユーティリティ
/// </summary>
public static class TestDataGenerators
{
    /// <summary>
    /// テスト用IMUデータジェネレータ
    /// データ受信速度をシミュレーション可能
    /// </summary>
    /// <param name="count">生成するデータ数</param>
    /// <param name="delayMs">フレーム間の遅延（ms）。0 でパフォーマンス計測、>0 でタイムアウト等をテスト</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    public static async IAsyncEnumerable<ImuData> GenerateTestImuData(
        int count = 10,
        int delayMs = 0,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < count; i++)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            yield return new ImuData
            {
                Quaternion = new Quaternion(0.707f, 0f, 0f, 0.707f),
                EulerAngles = new EulerAngles(0f, 45f, 0f),
                Timestamp = (uint)(1000 + i),
                MessageCounter = (ushort)i
            };

            if (delayMs > 0)
            {
                await Task.Delay(delayMs, cancellationToken);
            }
        }
    }

    /// <summary>
    /// 回転するIMUデータを生成（アニメーションテスト用）
    /// </summary>
    /// <param name="count">生成するデータ数</param>
    /// <param name="rotationStepDegrees">フレームごとの回転量（度）</param>
    /// <param name="delayMs">フレーム間の遅延（ms）</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    public static async IAsyncEnumerable<ImuData> GenerateRotatingImuData(
        int count = 100,
        float rotationStepDegrees = 5.0f,
        int delayMs = 16,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < count; i++)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            float angle = i * rotationStepDegrees;
            yield return new ImuData
            {
                Quaternion = new Quaternion(1.0f, 0f, 0f, 0f),
                EulerAngles = new EulerAngles(angle, angle * 0.5f, angle * 1.5f),
                Timestamp = (uint)(1000 + i * 16),
                MessageCounter = (ushort)i
            };

            if (delayMs > 0)
            {
                await Task.Delay(delayMs, cancellationToken);
            }
        }
    }
}
