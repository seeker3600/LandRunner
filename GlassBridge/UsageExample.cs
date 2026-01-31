namespace GlassBridge;

/// <summary>
/// GlassBridge ライブラリの使用例
/// 
/// ログ出力クラス一覧：
/// - ImuDeviceManager.cs (lines 7, 27, 48, 64, 82, 97)
/// - VitureLumaDevice.cs (lines 13, 103, 145, 175, 235, 285, 322, 365, 384)
/// - HidStreamProvider.cs (lines 13, 35, 47, 56, 63)
/// 
/// 詳細ログレベル：
/// - DEBUG: 接続フロー、フレーム数カウント、デバイス情報
/// - INFO: 重要なイベント（接続成功、ストリーム開始/終了）
/// - WARN: 回復可能なエラー（デバイス検出失敗など）
/// - ERROR: 動作失敗（接続失敗、コマンド送信失敗）
/// - TRACE: 最も詳細（通信内容、パケット情報）- 本番環境では無効化推奨
/// </summary>
public static class UsageExample
{
    /// <summary>
    /// VITURE Luma デバイスから IMU データストリーミングを取得
    /// 
    /// ログ出力クラス：
    /// - ImuDeviceManager.ConnectAsync() [line 27-30]
    /// - VitureLumaDevice.ConnectAsync() [line 13]
    /// - VitureLumaDevice.InitializeAsync() [line 103-119]
    /// - VitureLumaDevice.IdentifyStreamsAsync() [line 145-223]
    /// - VitureLumaDevice.GetImuDataStreamAsync() [line 235-298]
    /// - VitureLumaDevice.TryReadImuDataAsync() [line 322-348]
    /// - VitureLumaDevice.SendImuEnableCommandAsync() [line 365-414]
    /// </summary>
    public static async Task StreamImuDataAsync()
    {
        using var manager = new ImuDeviceManager();

        // デバイスに接続
        // ログ出力: ImuDeviceManager.ConnectAsync() [line 27-30]
        var device = await manager.ConnectAsync();
        if (device == null)
        {
            Console.WriteLine("Failed to connect to VITURE Luma device");
            return;
        }

        await using (device)
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            // IMU データストリーミング取得
            // ログ出力: VitureLumaDevice.GetImuDataStreamAsync() [line 235-298]
            //         VitureLumaDevice.SendImuEnableCommandAsync() [line 365-414]
            //         VitureLumaDevice.TryReadImuDataAsync() [line 322-348]
            await foreach (var imuData in device.GetImuDataStreamAsync(cts.Token))
            {
                var euler = imuData.EulerAngles;
                var quat = imuData.Quaternion;

                Console.WriteLine(
                    $"Timestamp: {imuData.Timestamp}, " +
                    $"Euler(R/P/Y): {euler.Roll:F2}/{euler.Pitch:F2}/{euler.Yaw:F2}, " +
                    $"Quat(W/X/Y/Z): {quat.W:F3}/{quat.X:F3}/{quat.Y:F3}/{quat.Z:F3}");
            }
        }
    }

    /// <summary>
    /// テスト用：モックデバイスの使用例
    /// 
    /// ログ出力：なし（モックデバイスはロギング未対応）
    /// </summary>
    public static async Task MockDeviceExampleAsync()
    {
        // テスト用のモックデバイスを作成
        var mockDevice = MockImuDevice.CreateWithPeriodicData(
            counter =>
            {
                // カウンター値に基づいて回転値を生成
                float angle = counter * 5.0f; // 5度ずつ回転
                return new ImuData
                {
                    Quaternion = new Quaternion(1.0f, 0.0f, 0.0f, 0.0f),
                    EulerAngles = new EulerAngles(angle, angle * 0.5f, angle * 1.5f),
                    Timestamp = (uint)counter,
                    MessageCounter = counter
                };
            },
            intervalMs: 16,
            maxIterations: 10
        );

        await using (mockDevice)
        {
            await foreach (var data in mockDevice.GetImuDataStreamAsync())
            {
                Console.WriteLine($"Mock data - Euler: {data.EulerAngles}");
            }
        }
    }
}
