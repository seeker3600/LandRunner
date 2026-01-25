## IMU データ記録・再生機能

GlassBridge に IMU デバイスからの生データを記録・再生できる機能を実装しました。

### 概要

- **記録**: デバイスから受け取ったIMUデータをJSON Lines形式で保存
- **再生**: 記録されたJSONファイルから Mock デバイスのように再生可能
- **フォーマット**: ヒューマンリーダブルな JSON Lines 形式（`.jsonl`）
- **テスト対応**: テストやパフォーマンス計測に最適

### ファイル構成

記録されるファイルは以下のような構成です：

```
output_directory/
├── frames_0.jsonl          # IMU フレームデータ (JSON Lines形式)
├── metadata_0.json         # 記録セッションメタデータ
├── frames_1.jsonl
└── metadata_1.json
```

#### frames_*.jsonl の例

```json
{"timestamp":0,"messageCounter":0,"quaternion":{"w":1.0,"x":0.0,"y":0.0,"z":0.0},"eulerAngles":{"roll":0.0,"pitch":0.0,"yaw":0.0},"rawBytes":"AAAAAA=="}
{"timestamp":10,"messageCounter":1,"quaternion":{"w":1.0,"x":0.01,"y":0.02,"z":0.03},"eulerAngles":{"roll":1.0,"pitch":2.0,"yaw":3.0},"rawBytes":"AAAAAA=="}
```

#### metadata_*.json の例

```json
{
  "recordedAt": "2026-01-25T12:34:56.1234567Z",
  "frameCount": 1000,
  "sampleRate": 100,
  "format": "jsonl"
}
```

### 主要なクラス

#### 記録関連

- **RecordingHidStream**: `IHidStream` をラップして記録機能を追加
- **RecordingHidStreamProvider**: HIDストリームプロバイダーを記録機能でラップ
- **ImuFrameRecord**: ImuData を JSON形式で表現
- **ImuRecordingSession**: 記録セッションのメタデータ

#### 再生関連

- **RecordedHidStream**: JSON Lines ファイルから `IHidStream` として再生
- **ReplayHidStreamProvider**: 記録ディレクトリから再生ストリームを提供

### 使用方法

#### デバイスからデータを記録

```csharp
var baseProvider = new HidStreamProvider(0x35CA, new[] { 0x1131 });
var recordingProvider = new RecordingHidStreamProvider(baseProvider, @"C:\IMU_Records");

var device = await VitureLumaDevice.ConnectWithProviderAsync(recordingProvider);

// IMU データを取得（自動的に記録される）
await foreach (var imuData in device.GetImuDataStreamAsync())
{
    // データ処理
}

await recordingProvider.FinalizeRecordingAsync();
await device.DisposeAsync();
```

#### 記録されたデータを再生

```csharp
var replayProvider = new ReplayHidStreamProvider(@"C:\IMU_Records");
var device = await VitureLumaDevice.ConnectWithProviderAsync(replayProvider);

// 記録されたデータを再度取得
await foreach (var imuData in device.GetImuDataStreamAsync())
{
    // データ分析やテスト実行
}
```

#### 記録ファイルを検査

```csharp
// メタデータを読み込む
var metadataJson = File.ReadAllText("output/metadata_0.json");
var metadata = ImuRecordingSession.FromJson(metadataJson);

Console.WriteLine($"Frames: {metadata.FrameCount}");
Console.WriteLine($"Recorded: {metadata.RecordedAt}");

// フレームを1行ずつ処理
using var reader = new StreamReader("output/frames_0.jsonl");
string? line;
while ((line = reader.ReadLine()) != null)
{
    var frameRecord = ImuFrameRecord.FromJsonLine(line);
    Console.WriteLine($"Timestamp: {frameRecord.Timestamp}");
}
```

### アーキテクチャ

```
アプリケーション層
    ↓
IImuDevice (変更なし)
    ↓
VitureLumaDevice
    ↓
[記録ラッパー層]
IHidStreamProvider
    ├─ RecordingHidStreamProvider (記録時)
    └─ ReplayHidStreamProvider (再生時)
    ↓
IHidStream
    ├─ RecordingHidStream (記録機能追加)
    ├─ RecordedHidStream (再生機能)
    ├─ RealHidStream (実デバイス)
    └─ MockHidStream (テスト)
```

### メリット

1. **IImuDevice インターフェイスは変更なし** - アプリケーション側への影響なし
2. **透過的な記録** - `VitureLumaDevice.GetImuDataStreamAsync()` を呼び出すだけで自動記録
3. **ヒューマンリーダブル** - JSON Lines 形式なので テキストエディタで確認可能
4. **柔軟な再生** - Mock デバイスとして機能するので テストやパフォーマンス計測に最適
5. **スケーラブル** - メモリ効率的な行単位処理

### テスト対象

- ? JSON シリアライゼーション
- ? メタデータの保存・復元
- ? JSON Lines フォーマットの可読性
- ? ファイル I/O
- ? ストリーム再生

すべてのテストが成功しています。
