## IMU データ記録・再生機能

GlassBridge は IMU デバイスからの複数データを記録・再生できる機能が組み込まれています。

### 概要

- **記録**: デバイスから受け取った IMU データを JSON Lines 形式で保存
- **再生**: 記録された JSON ファイルから Mock デバイスのように再生可能
- **フォーマット**: 人間が読み取れる JSON Lines 形式（`.jsonl`）
- **テスト対応**: テスト用パフォーマンス分析に最適

### ファイル構造

記録結果のファイルは以下のような構造です：

```
output_directory/
├── frames_0.jsonl          # IMU フレームデータ (JSON Lines形式)
├── metadata_0.json         # 記録セッションのメタデータ
├── frames_1.jsonl
├── metadata_1.json
```

#### frames_*.jsonl の形式

```json
{"timestamp":0,"messageCounter":0,"quaternion":{"w":1.0,"x":0.0,"y":0.0,"z":0.0},"eulerAngles":{"roll":0.0,"pitch":0.0,"yaw":0.0},"rawBytes":"AAAAAA=="}
{"timestamp":10,"messageCounter":1,"quaternion":{"w":1.0,"x":0.01,"y":0.02,"z":0.03},"eulerAngles":{"roll":1.0,"pitch":2.0,"yaw":3.0},"rawBytes":"AAAAAA=="}
```

#### metadata_*.json の形式

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
- **RecordingHidStreamProvider**: HID ストリームプロバイダーを記録機能でラップ
- **ImuFrameRecord**: ImuData を JSON 形式で表現
- **ImuRecordingSession**: 記録セッションのメタデータ

#### 再生関連

- **RecordedHidStream**: JSON Lines ファイルから `IHidStream` として再生
- **ReplayHidStreamProvider**: 記録ディレクトリから再生ストリームを作成

### 使用方法

#### デバイスからデータを記録

```csharp
var baseProvider = new HidStreamProvider(0x35CA, new[] { 0x1131 });
var recordingProvider = new RecordingHidStreamProvider(baseProvider, @"C:\IMU_Records");

var device = await VitureLumaDevice.ConnectWithProviderAsync(recordingProvider);

// IMU データを取得（同時に記録される）
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

// 記録されたデータを順序通り取得
await foreach (var imuData in device.GetImuDataStreamAsync())
{
    // データを処理・テスト実行
}
```

#### 記録ファイルを読み込む

```csharp
// メタデータを読み込む
var metadataJson = File.ReadAllText("output/metadata_0.json");
var metadata = ImuRecordingSession.FromJson(metadataJson);

Console.WriteLine($"Frames: {metadata.FrameCount}");
Console.WriteLine($"Recorded: {metadata.RecordedAt}");

// フレームを1行ごとに読む
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
[記録・再生ラッパー層]
IHidStreamProvider
    ├── RecordingHidStreamProvider (記録層)
    ├── ReplayHidStreamProvider (再生層)
    ↓
IHidStream
    ├── RecordingHidStream (記録機能追加)
    ├── RecordedHidStream (再生機能)
    ├── RealHidStream (実デバイス)
    ├── MockHidStream (テスト)
```

### 制約事項

1. **IImuDevice インターフェースは変更なし** - アプリケーション側への影響なし
2. **逐次的な記録** - `VitureLumaDevice.GetImuDataStreamAsync()` を呼び出している間に記録
3. **人間が読めるデータ形式** - JSON Lines 形式なので テキストエディタで確認可能
4. **単純な再生** - Mock デバイスのような機能なので テスト・性能分析に最適
5. **スケーラブル** - 複数セッションの同時記録が可能

### テスト対応

- ? JSON シリアライザー処理
- ? メタデータの保存・読み込み
- ? JSON Lines フォーマットの検証
- ? ファイル I/O
- ? ストリーム再生

すべてのテストが実装されています。
