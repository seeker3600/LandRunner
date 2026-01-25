namespace GlassBridge;

using GlassBridge.Internal;
using GlassBridge.Internal.HID;
using GlassBridge.Internal.Recording;

/// <summary>
/// IMU データの記録・再生機能の使用例
/// </summary>
public class RecordingUsageExample
{
    /// <summary>
    /// デバイスから IMU データを記録する
    /// </summary>
    public static async Task RecordFromDeviceAsync(string outputDirectory)
    {
        // HIDストリームプロバイダーを作成
        var baseProvider = new HidStreamProvider();

        // 記録機能でラップ
        var recordingProvider = new RecordingHidStreamProvider(baseProvider, outputDirectory);

        // デバイスに接続
        var device = await VitureLumaDevice.ConnectWithProviderAsync(recordingProvider);
        if (device == null)
            throw new InvalidOperationException("Failed to connect to device");

        try
        {
            // IMU データストリームを取得
            var count = 0;
            await foreach (var imuData in device.GetImuDataStreamAsync())
            {
                Console.WriteLine($"Timestamp: {imuData.Timestamp}, Roll: {imuData.EulerAngles.Roll}");
                
                count++;
                if (count >= 100)  // 100フレーム記録
                    break;
            }

            // 記録を確定
            await recordingProvider.FinalizeRecordingAsync();
            Console.WriteLine($"Recorded {count} frames to {outputDirectory}");
        }
        finally
        {
            await device.DisposeAsync();
            await recordingProvider.DisposeAsync();
        }
    }

    /// <summary>
    /// 記録されたデータから Mock デバイスを再生する
    /// </summary>
    public static async Task ReplayFromRecordingAsync(string recordingDirectory)
    {
        // 再生プロバイダーを作成
        var replayProvider = new ReplayHidStreamProvider(recordingDirectory);

        // Mock デバイスとして再生
        var device = await VitureLumaDevice.ConnectWithProviderAsync(replayProvider);
        if (device == null)
            throw new InvalidOperationException("Failed to create replay device");

        try
        {
            // IMU データストリームを再生
            var count = 0;
            await foreach (var imuData in device.GetImuDataStreamAsync())
            {
                Console.WriteLine($"Replayed - Timestamp: {imuData.Timestamp}, Pitch: {imuData.EulerAngles.Pitch}");
                
                count++;
                if (count >= 50)  // 50フレーム再生
                    break;
            }

            Console.WriteLine($"Replayed {count} frames from {recordingDirectory}");
        }
        finally
        {
            await device.DisposeAsync();
            await replayProvider.DisposeAsync();
        }
    }
}
