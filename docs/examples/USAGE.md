# GlassBridge 使用例

## 概要

GlassBridge ライブラリを使用して VITURE Luma デバイスから IMU データを取得する方法を説明します。

---

## 基本的な使用例

### IMU データストリーミング取得

```csharp
using GlassBridge;

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

        // IMU データストリーミング取得
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
```

---

## モックデバイスの使用例

テスト用にモックデバイスを使用する場合：

```csharp
using GlassBridge;
using GlassBridge.Utils;

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
```

---

## ログ出力について

### ログ出力クラス一覧

| クラス | 主なログ出力箇所 |
|--------|------------------|
| `ImuDeviceManager` | 接続フロー、デバイス検出 |
| `VitureLumaDevice` | 初期化、ストリーム識別、データ取得、コマンド送信 |
| `HidStreamProvider` | HID デバイス列挙、ストリーム取得 |

### ログレベル

| レベル | 用途 |
|--------|------|
| **DEBUG** | 接続フロー、フレーム数カウント、デバイス情報 |
| **INFO** | 重要なイベント（接続成功、ストリーム開始/終了） |
| **WARN** | 回復可能なエラー（デバイス検出失敗など） |
| **ERROR** | 動作失敗（接続失敗、コマンド送信失敗） |
| **TRACE** | 最も詳細（通信内容、パケット情報）- 本番環境では無効化推奨 |

---

## 関連ドキュメント

- [記録・再生機能の使用例](RECORDING_USAGE.md)
- [IMU 記録 API ガイド](../recording/API_GUIDE.md)
