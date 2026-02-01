using System.Numerics;
using GlassBridge;
using Microsoft.Extensions.Logging;

namespace LandRunner.Services;

/// <summary>
/// IMU データから画面上の位置・角度を計算するサービス
/// </summary>
public sealed class HeadTrackingService : IDisposable
{
    private readonly ILogger<HeadTrackingService> _logger = App.CreateLogger<HeadTrackingService>();

    private IImuDevice? _device;
    private CancellationTokenSource? _cts;
    private Task? _trackingTask;
    private bool _disposed;

    // 基準姿勢（リセット時の姿勢）
    private GlassBridge.Quaternion _referenceQuaternion = GlassBridge.Quaternion.Identity;

    // 現在の相対姿勢
    private volatile EulerAngles _currentAngles = new(0, 0, 0);

    // 視野角の設定（度）- XR グラスの視野角に近い値
    private const float HorizontalFov = 46f;
    private const float VerticalFov = 26f;

    /// <summary>
    /// トラッキングデータが更新されたときに発生
    /// </summary>
    public event EventHandler<TrackingData>? TrackingUpdated;

    /// <summary>
    /// 現在の相対オイラー角
    /// </summary>
    public EulerAngles CurrentAngles => _currentAngles;

    /// <summary>
    /// 接続されたデバイスでトラッキングを開始
    /// </summary>
    public void StartTracking(IImuDevice device)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(HeadTrackingService));

        _logger.LogInformation("ヘッドトラッキング開始");

        _device = device;
        _cts = new CancellationTokenSource();

        _trackingTask = Task.Run(() => TrackingLoopAsync(_cts.Token));
    }

    /// <summary>
    /// トラッキングを停止
    /// </summary>
    public async Task StopTrackingAsync()
    {
        _logger.LogInformation("ヘッドトラッキング停止");

        _cts?.Cancel();
        if (_trackingTask != null)
        {
            try
            {
                await _trackingTask;
            }
            catch (OperationCanceledException)
            {
                // 正常なキャンセル
            }
        }
        _cts?.Dispose();
        _cts = null;
    }

    /// <summary>
    /// 現在の姿勢を基準としてリセット
    /// </summary>
    public void ResetReference()
    {
        _logger.LogDebug("基準姿勢リセット要求");
        _pendingReset = true;
    }

    private volatile bool _pendingReset = true;

    private async Task TrackingLoopAsync(CancellationToken cancellationToken)
    {
        if (_device == null) return;

        try
        {
            await foreach (var imuData in _device.GetImuDataStreamAsync(cancellationToken))
            {
                // 基準姿勢のリセット
                if (_pendingReset)
                {
                    _referenceQuaternion = imuData.Quaternion;
                    _pendingReset = false;
                    _logger.LogDebug("基準姿勢をリセットしました");
                }

                // 相対姿勢を計算（基準姿勢からの差分）
                var relativeQuat = _referenceQuaternion.Conjugate() * imuData.Quaternion;

                // クォータニオンからオイラー角に変換
                var euler = QuaternionToEuler(relativeQuat);
                _currentAngles = euler;

                // 画面上の位置を計算
                var trackingData = CalculateTrackingData(euler);
                TrackingUpdated?.Invoke(this, trackingData);
            }
        }
        catch (OperationCanceledException)
        {
            // 正常なキャンセル
            _logger.LogDebug("トラッキングループがキャンセルされました");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "トラッキングループでエラーが発生しました");
        }
    }

    /// <summary>
    /// クォータニオンからオイラー角（度）に変換
    /// </summary>
    private static EulerAngles QuaternionToEuler(GlassBridge.Quaternion q)
    {
        var (w, x, y, z) = q;

        // Roll (X軸回転)
        var sinr_cosp = 2 * (w * x + y * z);
        var cosr_cosp = 1 - 2 * (x * x + y * y);
        var roll = MathF.Atan2(sinr_cosp, cosr_cosp);

        // Pitch (Y軸回転)
        var sinp = 2 * (w * y - z * x);
        float pitch;
        if (MathF.Abs(sinp) >= 1)
            pitch = MathF.CopySign(MathF.PI / 2, sinp);
        else
            pitch = MathF.Asin(sinp);

        // Yaw (Z軸回転)
        var siny_cosp = 2 * (w * z + x * y);
        var cosy_cosp = 1 - 2 * (y * y + z * z);
        var yaw = MathF.Atan2(siny_cosp, cosy_cosp);

        // ラジアンから度に変換
        const float radToDeg = 180f / MathF.PI;
        return new EulerAngles(roll * radToDeg, pitch * radToDeg, yaw * radToDeg);
    }

    /// <summary>
    /// オイラー角からトラッキングデータを計算
    /// </summary>
    private TrackingData CalculateTrackingData(EulerAngles euler)
    {
        // Yaw → 水平方向のオフセット（-1 ～ 1）
        // Pitch → 垂直方向のオフセット（-1 ～ 1）
        // Roll → 回転角度

        // 視野角で正規化（視野角の端で ±1）
        var horizontalOffset = -euler.Yaw / (HorizontalFov / 2);
        var verticalOffset = euler.Pitch / (VerticalFov / 2);

        // クランプ（視野外に出ないように）
        horizontalOffset = Math.Clamp(horizontalOffset, -1f, 1f);
        verticalOffset = Math.Clamp(verticalOffset, -1f, 1f);

        return new TrackingData(
            HorizontalOffset: horizontalOffset,
            VerticalOffset: verticalOffset,
            RotationAngle: -euler.Roll,
            RawAngles: euler);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cts?.Cancel();
        _cts?.Dispose();
    }
}

/// <summary>
/// トラッキングデータ
/// </summary>
/// <param name="HorizontalOffset">水平方向オフセット（-1 ～ 1、視野角の範囲）</param>
/// <param name="VerticalOffset">垂直方向オフセット（-1 ～ 1、視野角の範囲）</param>
/// <param name="RotationAngle">回転角度（度）</param>
/// <param name="RawAngles">生のオイラー角</param>
public record TrackingData(
    float HorizontalOffset,
    float VerticalOffset,
    float RotationAngle,
    EulerAngles RawAngles);
