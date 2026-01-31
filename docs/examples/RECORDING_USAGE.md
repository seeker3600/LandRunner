# IMU データ記録・再生の使用例

## 概要

GlassBridge ライブラリを使用して IMU データを記録し、後から再生する方法を説明します。

---

## デバイスからデータを記録する

```csharp
using GlassBridge;

public static async Task RecordFromDeviceAsync(string outputDirectory)
{
    using var manager = new ImuDeviceManager();

    // デバイスに接続して記録開始
    var device = await manager.ConnectAndRecordAsync(outputDirectory);
    if (device == null)
        throw new InvalidOperationException("Failed to connect to device for recording");

    try
    {
        // IMU データストリーミング取得（自動的に記録される）
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
        // デバイス破棄時、メタデータが自動保存される
        await device.DisposeAsync();
    }
}
```

---

## 記録データを再生する

```csharp
using GlassBridge;

public static async Task ReplayFromRecordingAsync(string recordingDirectory)
{
    using var manager = new ImuDeviceManager();

    // 記録データから再生デバイスを作成
    var device = await manager.ConnectFromRecordingAsync(recordingDirectory);
    if (device == null)
        throw new InvalidOperationException("Failed to create replay device");

    try
    {
        // IMU データストリーミング再生
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
    }
}
```

---

## ログ出力について

### 記録フローのログ出力

```
1. HidStreamProvider.GetStreamsAsync()      - HID ストリーム取得
2. RecordingHidStreamProvider でラップ       - 記録用ラッパー適用
3. VitureLumaDevice.ConnectWithProviderAsync() - デバイス接続
4. GetImuDataStreamAsync() でデータ取得      - 自動記録開始
5. RecordingHidStream.FinalizeAsync()        - メタデータ保存
```

### 再生フローのログ出力

```
1. ReplayHidStreamProvider インスタンス化    - 再生プロバイダー作成
2. VitureLumaDevice.ConnectWithProviderAsync() - デバイス接続
3. GetImuDataStreamAsync()                   - 記録データ再生
```

---

## 記録ファイル形式

記録データは JSON Lines 形式で保存されます。詳細は [API_GUIDE.md](../recording/API_GUIDE.md) を参照してください。

---

## 関連ドキュメント

- [基本的な使用例](USAGE.md)
- [IMU 記録 API ガイド](../recording/API_GUIDE.md)
- [記録機能の内部実装](../recording/IMPLEMENTATION.md)
