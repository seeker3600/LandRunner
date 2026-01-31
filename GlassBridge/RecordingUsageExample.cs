namespace GlassBridge;

using GlassBridge.Internal;
using GlassBridge.Internal.HID;
using GlassBridge.Internal.Recording;

/// <summary>
/// IMU データの記録・再生機能の使用例
/// 
/// ログ出力クラス一覧：
/// - ImuDeviceManager.cs [line 48-76]
/// - VitureLumaDevice.cs [line 13, 103, 145, 175, 235, 285, 322, 365]
/// - HidStreamProvider.cs [line 13, 35, 47, 56, 63]
/// - RecordingHidStream.cs [line 13, 30, 55, 72, 81, 99]
/// 
/// 記録フロー：
/// 1. HidStreamProvider.GetStreamsAsync() [HidStreamProvider line 35-56]
/// 2. RecordingHidStreamProvider でラップ
/// 3. VitureLumaDevice.ConnectWithProviderAsync() [VitureLumaDevice line 13-119]
/// 4. GetImuDataStreamAsync() でデータ取得・自動記録 [VitureLumaDevice line 235-298]
/// 5. RecordingHidStream.FinalizeAsync() でメタデータ保存 [RecordingHidStream line 99-104]
/// </summary>
public class RecordingUsageExample
{
    /// <summary>
    /// デバイスから IMU データを記録する
    /// 
    /// ログ出力クラス：
    /// - ImuDeviceManager.ConnectAndRecordAsync() [line 48-76]
    ///   - HidStreamProvider.GetStreamsAsync() [line 35-56]
    ///   - VitureLumaDevice.ConnectWithProviderAsync() [line 13]
    ///   - VitureLumaDevice.InitializeAsync() [line 103-119]
    ///   - VitureLumaDevice.IdentifyStreamsAsync() [line 145-223]
    /// - VitureLumaDevice.GetImuDataStreamAsync() [line 235-298]
    /// - RecordingHidStream.ReadAsync() [line 30-63]
    /// - RecordingHidStream.FinalizeAsync() [line 99-104]
    /// </summary>
    public static async Task RecordFromDeviceAsync(string outputDirectory)
    {
        using var manager = new ImuDeviceManager();

        // デバイスに接続して記録開始
        // ログ出力: ImuDeviceManager.ConnectAndRecordAsync() [line 48-76]
        //         HidStreamProvider.GetStreamsAsync() [line 35-56]
        //         VitureLumaDevice.InitializeAsync() [line 103-119]
        //         VitureLumaDevice.IdentifyStreamsAsync() [line 145-223]
        var device = await manager.ConnectAndRecordAsync(outputDirectory);
        if (device == null)
            throw new InvalidOperationException("Failed to connect to device for recording");

        try
        {
            // IMU データストリーミング取得
            // ログ出力: VitureLumaDevice.GetImuDataStreamAsync() [line 235-298]
            //         VitureLumaDevice.SendImuEnableCommandAsync() [line 365-414]
            //         RecordingHidStream.ReadAsync() [line 30-63]
            //         FrameCount が 100 の倍数でログ出力 [line 55-58]
            var count = 0;
            await foreach (var imuData in device.GetImuDataStreamAsync())
            {
                Console.WriteLine($"Timestamp: {imuData.Timestamp}, Roll: {imuData.EulerAngles.Roll}");
                
                count++;
                if (count >= 100)  // 100フレーム記録
                    break;
            }

            Console.WriteLine($"Recorded {count} frames to {outputDirectory}");
        }
        finally
        {
            // デバイス破棄時、RecordingHidStream.FinalizeAsync() が呼ばれる
            // ログ出力: RecordingHidStream.FinalizeAsync() [line 99-104]
            await device.DisposeAsync();
        }
    }

    /// <summary>
    /// 記録されたデータから Mock デバイスを再生する
    /// 
    /// ログ出力クラス：
    /// - ImuDeviceManager.ConnectFromRecordingAsync() [line 82-97]
    ///   - ReplayHidStreamProvider インスタンス化
    ///   - VitureLumaDevice.ConnectWithProviderAsync() [line 13]
    ///   - VitureLumaDevice.InitializeAsync() [line 103-119]
    /// - VitureLumaDevice.GetImuDataStreamAsync() [line 235-298]
    /// - ReplayHidStreamProvider.GetStreamsAsync() [実装参照]
    /// </summary>
    public static async Task ReplayFromRecordingAsync(string recordingDirectory)
    {
        using var manager = new ImuDeviceManager();

        // 記録データから再生デバイスを作成
        // ログ出力: ImuDeviceManager.ConnectFromRecordingAsync() [line 82-97]
        //         VitureLumaDevice.InitializeAsync() [line 103-119]
        var device = await manager.ConnectFromRecordingAsync(recordingDirectory);
        if (device == null)
            throw new InvalidOperationException("Failed to create replay device");

        try
        {
            // IMU データストリーミング再生
            // ログ出力: VitureLumaDevice.GetImuDataStreamAsync() [line 235-298]
            //         VitureLumaDevice.TryReadImuDataAsync() [line 322-348]
            //         FrameCount が 1000 の倍数でログ出力 [line 245-248]
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
            // デバイス破棄
            // ログ出力: VitureLumaDevice.DisposeAsync() [line 415-425]
            await device.DisposeAsync();
        }
    }
}
