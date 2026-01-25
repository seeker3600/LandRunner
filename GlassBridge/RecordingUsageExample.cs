namespace GlassBridge;

using GlassBridge.Internal;
using GlassBridge.Internal.HID;
using GlassBridge.Internal.Recording;

/// <summary>
/// IMU データの記録・再生機能を使用する例
/// </summary>
public class RecordingUsageExample
{
    /// <summary>
    /// デバイスから IMU データを記録する例
    /// </summary>
    public static async Task RecordFromDeviceAsync(string outputDirectory)
    {
        // HIDストリームプロバイダーを作成
        var baseProvider = new HidStreamProvider(
            vendorId: 0x35CA,
            0x1131  // VITURE Luma
        );

        // 記録機能をラップ
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
    /// 記録されたデータから Mock デバイスを再生する例
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

    /// <summary>
    /// 記録ファイルの内容を確認する例
    /// </summary>
    public static void InspectRecordingFiles(string recordingDirectory)
    {
        // メタデータを読み込む
        var metadataFiles = Directory.GetFiles(recordingDirectory, "metadata_*.json");
        foreach (var metadataFile in metadataFiles)
        {
            var json = File.ReadAllText(metadataFile);
            var metadata = ImuRecordingSession.FromJson(json);
            
            Console.WriteLine($"Recording: {metadataFile}");
            Console.WriteLine($"  Recorded At: {metadata.RecordedAt}");
            Console.WriteLine($"  Frame Count: {metadata.FrameCount}");
            Console.WriteLine($"  Sample Rate: {metadata.SampleRate}");
        }

        // フレームデータの最初の数行を表示
        var framesFiles = Directory.GetFiles(recordingDirectory, "frames_*.jsonl");
        foreach (var framesFile in framesFiles)
        {
            Console.WriteLine($"\nFirst 5 frames from {Path.GetFileName(framesFile)}:");
            
            var lines = File.ReadLines(framesFile).Take(5);
            int lineNum = 1;
            foreach (var line in lines)
            {
                var record = ImuFrameRecord.FromJsonLine(line);
                Console.WriteLine($"  Frame {lineNum}: Timestamp={record.Timestamp}, " +
                    $"Quat=({record.Quaternion.W:F2},{record.Quaternion.X:F2}), " +
                    $"Euler=({record.EulerAngles.Roll:F2},{record.EulerAngles.Pitch:F2},{record.EulerAngles.Yaw:F2})");
                lineNum++;
            }
        }
    }
}
