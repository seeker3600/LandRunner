# GlassBridge

XRグラスからのIMU（慣性測定装置）データを取得するための .NET ライブラリ。

**現在対応：** VITURE Luma、VITURE Pro、VITURE One、VITURE One Lite、VITURE Luma Pro

## 概要

GlassBridgeは、Windows上でVITURE系シースルーグラスから3DoF（ロール、ピッチ、ヨー）の頭部姿勢データを非同期ストリームで取得できるライブラリです。

HIDプロトコルの詳細を隠蔽し、シンプルで非同期的なAPIを提供します。また、テスト時にはモック実装で容易にシミュレーションできます。

## 特徴

- ? **複数モデル対応** - VITURE Luma・Pro・One系列をサポート
- ? **非同期ストリーム** - `IAsyncEnumerable<ImuData>`で自然なデータフロー
- ? **テスト可能** - インターフェース分離とモック実装
- ? **複数フォーマット対応** - オイラー角とクォータニオンの両方を提供
- ? **CRC検証** - パケットの整合性確認
- ? **リソース管理** - `IAsyncDisposable`による自動クリーンアップ

## プロジェクト構成

```
GlassBridge/
├── 公開 API
│   ├── ImuData.cs                 IMUデータ型（record）
│   ├── Interfaces.cs              インターフェース定義
│   ├── ImuDeviceManager.cs        デバイス接続マネージャー
│   └── MockImuDevice.cs           テスト用モック実装
└── 内部実装 (GlassBridge.Internal namespace)
    ├── VitureLumaDevice.cs        HIDデバイス実装
    ├── VitureLumaPacket.cs        プロトコルパケット処理
    └── Crc16Ccitt.cs              CRC-16計算ユーティリティ
```

### 名前空間

- **GlassBridge** - 公開API（`ImuDeviceManager`、`ImuData` 等）
- **GlassBridge.Internal** - 内部実装詳細（HIDデバイス、パケット処理等）

## インストール

このライブラリはソリューションの一部として含まれます。プロジェクトファイルで参照してください。

### 依存パッケージ

- **HidSharp** 2.6.4 - HIDデバイス通信

### 要件

- **.NET 10** 以上
- **Windows** (USB HID通信のため)

## クイックスタート

### 基本的な使用方法

```csharp
using GlassBridge;

// マネージャーを作成
using var manager = new ImuDeviceManager();

// VITURE Lumaに接続
var device = await manager.ConnectAsync();
if (device == null)
{
    Console.WriteLine("デバイスが見つかりません");
    return;
}

// IMUデータストリームを処理
await using (device)
{
    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    
    await foreach (var imuData in device.GetImuDataStreamAsync(cts.Token))
    {
        var euler = imuData.EulerAngles;
        var quat = imuData.Quaternion;
        
        Console.WriteLine($"Roll: {euler.Roll:F1}°, Pitch: {euler.Pitch:F1}°, Yaw: {euler.Yaw:F1}°");
        Console.WriteLine($"Quaternion: W={quat.W:F3}, X={quat.X:F3}, Y={quat.Y:F3}, Z={quat.Z:F3}");
    }
}
```

## API リファレンス

### ImuDeviceManager

ユーザー向けのメインエントリーポイント。

#### `ConnectAsync(CancellationToken = default)`

VITURE Lumaデバイスを検出して接続します。

**戻り値:** `Task<IImuDevice?>` - 接続されたデバイス、または接続失敗時は`null`

```csharp
var device = await manager.ConnectAsync();
```

### IImuDevice

接続されたIMUデバイスを表します。

#### `GetImuDataStreamAsync(CancellationToken = default)`

IMUデータの非同期ストリームを取得します。

**戻り値:** `IAsyncEnumerable<ImuData>`

```csharp
await foreach (var data in device.GetImuDataStreamAsync(cancellationToken))
{
    // データ処理
}
```

#### `IsConnected`

デバイスが接続されているかを示すプロパティ。

```csharp
if (device.IsConnected)
{
    // デバイスが接続中
}
```

### ImuData

IMUデータを表すレコード型。

```csharp
public record ImuData
{
    public required Quaternion Quaternion { get; init; }
    public required EulerAngles EulerAngles { get; init; }
    public required uint Timestamp { get; init; }
    public required ushort MessageCounter { get; init; }
}
```

### Quaternion

クォータニオン表現。

```csharp
public record Quaternion(float W, float X, float Y, float Z)
```

**メソッド:**
- `Conjugate()` - 共役クォータニオンを計算
- `operator *(Quaternion q1, Quaternion q2)` - 2つのクォータニオンを乗算

```csharp
var conjugate = quat.Conjugate();
var combined = quat1 * quat2;  // 回転の合成
```

### EulerAngles

オイラー角表現（度単位）。

```csharp
public record EulerAngles(float Roll, float Pitch, float Yaw);
```

## テスト

### モックデバイスの使用

テスト時には`MockImuDevice`で実デバイスの代わりができます。

#### 静的データを返すモック

```csharp
var testData = new ImuData
{
    Quaternion = Quaternion.Identity,
    EulerAngles = new EulerAngles(0, 0, 0),
    Timestamp = 0,
    MessageCounter = 0
};

var mockDevice = MockImuDevice.CreateWithStaticData(testData);
await using (mockDevice)
{
    await foreach (var data in mockDevice.GetImuDataStreamAsync())
    {
        Assert.Equal(testData, data);
    }
}
```

#### 定期的にデータを生成するモック

```csharp
var mockDevice = MockImuDevice.CreateWithPeriodicData(
    counter =>
    {
        float angle = counter * 5.0f;  // 5度ずつ回転
        return new ImuData
        {
            Quaternion = Quaternion.Identity,
            EulerAngles = new EulerAngles(angle, angle * 0.5f, angle * 1.5f),
            Timestamp = (uint)counter,
            MessageCounter = counter
        };
    },
    intervalMs: 16,      // 60FPS相当
    maxIterations: 100
);

await using (mockDevice)
{
    var count = 0;
    await foreach (var data in mockDevice.GetImuDataStreamAsync())
    {
        count++;
    }
    Assert.Equal(100, count);
}
```

#### テストでのインターフェース利用

`IImuDevice`インターフェースを使用すれば、実装をテスト時に切り替えられます：

```csharp
public class ImuDataProcessor
{
    private readonly IImuDevice _device;

    public ImuDataProcessor(IImuDevice device)
    {
        _device = device;  // コンストラクタインジェクション
    }

    public async Task ProcessDataAsync()
    {
        await foreach (var data in _device.GetImuDataStreamAsync())
        {
            // 処理
        }
    }
}

// テスト時
[Fact]
public async Task TestWithMockDevice()
{
    var mockDevice = MockImuDevice.CreateWithStaticData(
        new ImuData { /* ... */ }
    );
    
    var processor = new ImuDataProcessor(mockDevice);
    await processor.ProcessDataAsync();
    
    // 検証
}
```

## 技術仕様

### 対応デバイス

| デバイス | VID | PID | サポート |
|---------|-----|-----|---------|
| VITURE Luma | 0x35CA | 0x1131 | ? |

### VITURE Lumaプロトコル

詳細は `docs/hid/VITURE_Luma.md` を参照してください。

#### パケット構造

- **IMU データ**: ヘッダ `0xFF 0xFC`
- **MCU ACK**: ヘッダ `0xFF 0xFD`
- **MCU コマンド**: ヘッダ `0xFF 0xFE`

#### データ形式

- **オイラー角**: ビッグエンディアン IEEE754 float32
- **クォータニオン**: オイラー角から変換
- **CRC**: CRC-16-CCITT (polynomial 0x1021, initial 0xFFFF)

### IMUデータ更新レート

VITURE Lumaは標準で約60?100Hzでデータを送信します。

## トラブルシューティング

### デバイスが見つからない

1. VITURE Lumaが正しくUSB接続されているか確認
2. 他のアプリケーション（SpaceWalkerなど）がグラスを使用していないか確認
3. デバイスドライバが正しくインストールされているか確認
4. `ImuDeviceManager.ConnectAsync()`で`null`が返された場合、デバイス管理画面でVITUREグラスが認識されているか確認

### データストリームが止まる

1. キャンセレーションの状態を確認
2. デバイスの接続状態を確認 (`IImuDevice.IsConnected`)
3. USB接続が不安定でないか確認

### CRC エラーは自動的にスキップされます

破損したパケットは自動的に破棄され、次の有効なパケットを待ちます。

## 軸マッピング

**重要:** 軸マッピングは実装時の標準値ですが、実機での検証を推奨します。

現在の実装（WebXR仕様に基づく）:
- `Yaw = -raw0`
- `Roll = -raw1`
- `Pitch = raw2`

実際のアプリケーションで期待と異なる場合は、以下を確認してください：

1. "右を向く" → Yawが増加するか
2. "上を向く" → Pitchが増加するか
3. "右に傾ける" → Rollが増加するか

## リソース管理

`IImuDevice`は`IAsyncDisposable`を実装しており、接続を適切にクローズします：

```csharp
// using文で自動的にDisposeされます
await using (var device = await manager.ConnectAsync())
{
    // 使用
}  // ここで自動的にIMU無効化コマンドが送信されます
```

## 拡張性

今後他のデバイスに対応させる場合は、以下を実装してください：

1. `IImuDevice`を実装した新しいデバイスクラス
2. デバイス固有のプロトコルパーサー
3. `IImuDeviceManager.ConnectAsync()`で新デバイスの検出を追加

## ライセンス

[プロジェクトのライセンスに準じます]

## 関連リソース

- [VITURE Luma HID プロトコル仕様](../docs/hid/VITURE_Luma.md)
- [bfvogel/viture-webxr-extension](https://github.com/bfvogel/viture-webxr-extension) - リバースエンジニアリング資料の出典
