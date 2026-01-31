# ImuDeviceManager の記録・再生機能 - 使用ガイド

`ImuDeviceManager` に記録・再生機能が組み込まれています。クライアント開発者は簡単に利用できます。

## 基本的な使用方法

### 1. デバイスからのデータ記録

```csharp
using var manager = new ImuDeviceManager();

// デバイスに接続して記録を開始
await using var device = await manager.ConnectAndRecordAsync(@"C:\IMU_Records");
if (device == null)
{
    Console.WriteLine("Failed to connect to device");
    return;
}

// IMU データストリームを取得
// 終了時に C:\IMU_Records に frames_0.jsonl, metadata_0.json として保存される
var count = 0;
await foreach (var imuData in device.GetImuDataStreamAsync())
{
    Console.WriteLine($"Timestamp: {imuData.Timestamp}, Roll: {imuData.EulerAngles.Roll}");
    
    count++;
    if (count >= 1000)  // 1000フレーム記録したら終了
        break;
}

Console.WriteLine($"Recorded {count} frames");
// device.DisposeAsync() 時に最終的にメタデータが保存される
```

### 2. 記録されたデータの再生（テスト・パフォーマンス分析）

```csharp
using var manager = new ImuDeviceManager();

// 記録ファイルから再生デバイスを作成
await using var replayDevice = await manager.ConnectFromRecordingAsync(@"C:\IMU_Records");
if (replayDevice == null)
{
    Console.WriteLine("No recording files found");
    return;
}

// 記録されたデータをストリームで送信
// 実デバイスと同じインターフェースで利用可能
var count = 0;
await foreach (var imuData in replayDevice.GetImuDataStreamAsync())
{
    // テストや性能分析用ロジック
    Console.WriteLine($"Replayed - Timestamp: {imuData.Timestamp}");
    
    count++;
    if (count >= 100)  // 100フレーム再生したら終了
        break;
}

Console.WriteLine($"Replayed {count} frames");
```

### 3. 通常のデバイス接続（変更なし）

```csharp
using var manager = new ImuDeviceManager();

// 通常のデバイス接続
var device = await manager.ConnectAsync();
if (device == null)
{
    Console.WriteLine("Failed to connect to device");
    return;
}

try
{
    await foreach (var imuData in device.GetImuDataStreamAsync())
    {
        Console.WriteLine($"Timestamp: {imuData.Timestamp}");
    }
}
finally
{
    await device.DisposeAsync();
}
```

## API リファレンス

### IImuDeviceManager

#### ConnectAsync()
実デバイスに接続します。変更なし。

```csharp
Task<IImuDevice?> ConnectAsync(CancellationToken cancellationToken = default);
```

**戻り値**: 接続されたデバイス、接続失敗時は `null`

---

#### ConnectAndRecordAsync()
デバイスに接続し、取得したデータをファイルに記録します。

```csharp
Task<IImuDevice?> ConnectAndRecordAsync(
    string outputDirectory,
    CancellationToken cancellationToken = default);
```

**パラメータ**:
- `outputDirectory`: 記録ファイルの出力ディレクトリ（なければ作成）
- `cancellationToken`: キャンセルトークン（オプション）

**戻り値**: 記録機能付きのデバイス、接続失敗時は `null`

**出力ファイル**:
- `frames_0.jsonl`: IMU フレームデータ（JSON Lines形式）
- `metadata_0.json`: 記録セッションのメタデータ

**メタデータの定期保存**:
- `device.DisposeAsync()` 時に最終的に `metadata_*.json` が保存されます
- `await using` を使用して確実にメモリを解放するようにしてください

**例外**:
- `ArgumentException`: `outputDirectory` が null または empty の場合

---

#### ConnectFromRecordingAsync()
記録されたファイルから Mock デバイスを作成して再生します。

```csharp
Task<IImuDevice?> ConnectFromRecordingAsync(
    string recordingDirectory,
    CancellationToken cancellationToken = default);
```

**パラメータ**:
- `recordingDirectory`: 記録ファイルが保存されているディレクトリ
- `cancellationToken`: キャンセルトークン（オプション）

**戻り値**: 再生用の Mock デバイス、ファイルなければ `null`

**例外**:
- `ArgumentException`: `recordingDirectory` が null または empty の場合
- `DirectoryNotFoundException`: ディレクトリが見つからない場合

---

## 実装例：テストシナリオ

### テストの流れと実行

```csharp
public class ImuDataProcessingTests
{
    [Fact]
    public async Task ProcessingLogic_WithRecordedData()
    {
        using var manager = new ImuDeviceManager();
        
        // 記録ファイルから再生
        await using var replayDevice = await manager.ConnectFromRecordingAsync(@"C:\TestRecordings");
        if (replayDevice == null)
            throw new InvalidOperationException("No recording found");

        var processingResults = new List<ProcessingResult>();
        
        // データ処理
        await foreach (var imuData in replayDevice.GetImuDataStreamAsync())
        {
            var result = ProcessImuData(imuData);
            processingResults.Add(result);
            
            if (processingResults.Count >= 1000)
                break;
        }

        // 結果確認
        Assert.NotEmpty(processingResults);
        Assert.All(processingResults, r => Assert.True(r.IsValid));
    }

    private ProcessingResult ProcessImuData(ImuData data)
    {
        // カスタムロジック
        return new ProcessingResult { IsValid = true };
    }
}
```

### パフォーマンス計測

```csharp
using var manager = new ImuDeviceManager();
var recordingDir = @"C:\BenchmarkRecordings";

await using var replayDevice = await manager.ConnectFromRecordingAsync(recordingDir);
if (replayDevice == null)
    throw new InvalidOperationException("No recording found");

var stopwatch = System.Diagnostics.Stopwatch.StartNew();
var frameCount = 0;

await foreach (var imuData in replayDevice.GetImuDataStreamAsync())
{
    // ベンチマーク対象のロジック
    var euler = imuData.EulerAngles;
    var quat = imuData.Quaternion;
    
    frameCount++;
    if (frameCount >= 10000)
        break;
}

stopwatch.Stop();
Console.WriteLine($"Processed {frameCount} frames in {stopwatch.ElapsedMilliseconds}ms");
Console.WriteLine($"Average: {stopwatch.ElapsedMilliseconds / (double)frameCount}ms per frame");
```

## 記録ファイルの構造

### frames_*.jsonl
JSON Lines 形式のフレームデータ。1行が1フレームです。

```json
{"timestamp":0,"messageCounter":0,"quaternion":{"w":1.0,"x":0.0,"y":0.0,"z":0.0},"eulerAngles":{"roll":0.0,"pitch":0.0,"yaw":0.0},"rawBytes":"..."}
```

**フィールド**:
- `timestamp`: フレームのタイムスタンプ（uint32）
- `messageCounter`: メッセージカウンター（ushort）
- `quaternion`: クォータニオン（w, x, y, z）
- `eulerAngles`: オイラー角（roll, pitch, yaw）
- `rawBytes`: 生バイト列（Base64エンコード）

### metadata_*.json
記録セッションのメタデータ。**device.DisposeAsync() 時に最終的に作成**されます。

```json
{
  "recordedAt": "2026-01-25T12:34:56.1234567Z",
  "frameCount": 1000,
  "sampleRate": 100,
  "format": "jsonl"
}
```

**フィールド**:
- `recordedAt`: 記録開始時刻（ISO 8601形式）
- `frameCount`: フレーム数
- `sampleRate`: サンプリングレート（Hz）
- `format`: ファイル形式（通常 "jsonl"）

## エラーハンドリング

```csharp
using var manager = new ImuDeviceManager();

try
{
    // 記録
    await using var device = await manager.ConnectAndRecordAsync(recordingDir);
    if (device == null)
    {
        Console.WriteLine("Device connection failed");
        return;
    }

    // データ取得
    // device.DisposeAsync() 時に最終的にメタデータが保存される
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Invalid argument: {ex.Message}");
}
catch (DirectoryNotFoundException ex)
{
    Console.WriteLine($"Directory not found: {ex.Message}");
}
catch (ObjectDisposedException ex)
{
    Console.WriteLine($"Manager already disposed: {ex.Message}");
}
finally
{
    manager.Dispose();
}
```

## 注意事項・ベストプラクティス

1. **ファイルシステム I/O**: 記録時はファイルシステムへの書き込みが発生するため、ディスク速度に左右されます
2. **シーケンシャル**: 再生は1フレーム単位で1行読み込むため、ランダムアクセスできません
3. **マルチセッション**: 複数マネージャーで同じディレクトリに記録する場合、異なるセッションごとに device は解放が必要です
4. **リアルタイムディレイ**: 再生は記録時のタイムスタンプを使用しますが、実行速度のずれに左右されます
5. **メタデータ保存**: `device.DisposeAsync()` で最終的にメタデータが保存されるため、`await using` の使用を強制します

## テスト仕様一覧

? デバイス接続時のデータ記録
? 記録ファイルからの再生
? device.DisposeAsync() 時のメタデータ保存
? マルチセッション切り替え
? エラーハンドリング（拡張仕様など）
? インターフェース確認

すべてのテストが実装されています。
