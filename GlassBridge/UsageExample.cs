namespace GlassBridge;

/// <summary>
/// GlassBridgeライブラリの使用例
/// </summary>
public static class UsageExample
{
    /// <summary>
    /// VITURE LumaデバイスからのIMUデータストリーミング例
    /// </summary>
    public static async Task StreamImuDataAsync()
    {
        using var manager = new ImuDeviceManager();

        // デバイスに接続
        var device = await manager.ConnectAsync();
        if (device == null)
        {
            Console.WriteLine("Failed to connect to VITURE Luma device");
            return;
        }

        await using (device)
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            // IMUデータストリームを処理
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
    /// </summary>
    public static async Task MockDeviceExampleAsync()
    {
        // テスト用のモックデバイスを作成
        var mockDevice = MockImuDevice.CreateWithPeriodicData(
            counter =>
            {
                // カウンター値に基づいて回転値を生成
                float angle = counter * 5.0f; // 5度ずつ増加
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
