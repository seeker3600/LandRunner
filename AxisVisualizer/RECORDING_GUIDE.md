# AxisVisualizer - IMU データ記録・再生ガイド

AxisVisualizer に IMU データの記録・再生機能が追加されました。

## 機能概要

### 1. 通常モード（記録なし）
- リアルタイムでデバイスから IMU データを取得して表示
- データは保存されません

### 2. 記録モード
- デバイスからのデータをリアルタイムで表示しながら、同時にファイルに記録
- GlassBridge の `ConnectAndRecordAsync()` を使用
- 記録ファイルは JSON Lines 形式で保存

### 3. 再生モード
- 過去に記録したデータファイルを読み込んで再生
- GlassBridge の `ConnectFromRecordingAsync()` を使用
- 実デバイス不要で動作確認やデバッグが可能

## 使用方法

### 記録モード

```
┌─────────────────────────────────────┐
│ UI 操作手順                         │
└─────────────────────────────────────┘

1. アプリケーション起動
2. [Recording] チェックボックスをONにする（デフォルトON）
3. 必要に応じて [Browse...] で記録先フォルダを変更
4. [Connect Device] ボタンをクリック
5. デバイスから IMU データを取得・表示・記録
6. [Disconnect] ボタンで終了

記録ファイル:
  - frames_0.jsonl    - IMU フレームデータ
  - metadata_0.json   - セッションメタデータ
```

### 再生モード

```
┌─────────────────────────────────────┐
│ UI 操作手順                         │
└─────────────────────────────────────┘

1. アプリケーション起動
2. [Replay Mode] チェックボックスをONにする
3. [Browse...] で記録ファイルが保存されているフォルダを選択
4. [Connect Device] ボタンをクリック（実際にはファイルから読み込み）
5. 記録されたデータが再生される
6. [Disconnect] ボタンで終了
```

### 通常モード（記録なし）

```
┌─────────────────────────────────────┐
│ UI 操作手順                         │
└─────────────────────────────────────┘

1. アプリケーション起動
2. [Recording] チェックボックスをOFFにする
3. [Connect Device] ボタンをクリック
4. デバイスから IMU データを取得・表示（記録はされない）
5. [Disconnect] ボタンで終了
```

## UI コントロール

### チェックボックス

| コントロール | 説明 |
|-------------|------|
| **Recording** | 記録モードの有効/無効を切り替え（接続前のみ変更可能） |
| **Replay Mode** | 再生モードの有効/無効を切り替え（接続前のみ変更可能） |

### ボタン

| ボタン | 説明 |
|--------|------|
| **Browse...** | 記録先または再生元のフォルダを選択（接続前のみ使用可能） |
| **Connect Device** | デバイス接続または記録ファイル読み込みを開始 |
| **Disconnect** | 接続を切断または再生を停止 |

### パス表示

記録先/再生元のフォルダパスが画面下部に表示されます。

**デフォルトパス:**
```
C:\Users\<username>\AppData\Roaming\LandRunner\Recordings
```

## 記録ファイル形式

GlassBridge の記録機能により、以下のファイルが作成されます。

### frames_0.jsonl（JSON Lines形式）

```json
{"Timestamp":1234567890,"MessageCounter":0,"Quaternion":{"W":1.0,"X":0.0,"Y":0.0,"Z":0.0},"EulerAngles":{"Roll":0.0,"Pitch":0.0,"Yaw":0.0}}
{"Timestamp":1234567891,"MessageCounter":1,"Quaternion":{"W":0.999,"X":0.001,"Y":0.002,"Z":0.003},"EulerAngles":{"Roll":0.1,"Pitch":0.2,"Yaw":0.3}}
...
```

### metadata_0.json

```json
{
  "SessionId": "12345678-1234-1234-1234-123456789012",
  "StartTime": "2024-01-01T00:00:00.0000000Z",
  "EndTime": "2024-01-01T00:10:00.0000000Z",
  "TotalFrames": 6000
}
```

## 実装の詳細

### GlassBridge API 使用箇所

#### 記録モード
```csharp
_deviceManager = new ImuDeviceManager();
_device = await _deviceManager.ConnectAndRecordAsync(RecordingPath);
```

#### 再生モード
```csharp
_deviceManager = new ImuDeviceManager();
_device = await _deviceManager.ConnectFromRecordingAsync(RecordingPath);
```

#### 通常モード
```csharp
_deviceManager = new ImuDeviceManager();
_device = await _deviceManager.ConnectAsync();
```

### ViewModel プロパティ

| プロパティ | 型 | 説明 |
|-----------|-----|------|
| `IsRecordingEnabled` | bool | 記録モードが有効かどうか |
| `IsReplayMode` | bool | 再生モードが有効かどうか |
| `RecordingPath` | string | 記録先または再生元のフォルダパス |

## トラブルシューティング

### 記録ファイルが見つからない

**原因**: 再生モードでフォルダパスが間違っている

**解決策**:
1. [Browse...] ボタンで正しいフォルダを選択
2. フォルダ内に `frames_0.jsonl` と `metadata_0.json` があることを確認

### デバイスに接続できない

**原因**: 
- デバイスが接続されていない
- 他のアプリケーションがデバイスを使用中

**解決策**:
1. VITURE Luma デバイスが USB で接続されているか確認
2. 他のアプリケーションを終了
3. 再生モードで動作確認してからデバイスを接続

### 記録が保存されない

**原因**: 
- [Recording] チェックボックスがOFFになっている
- `DisposeAsync()` が呼ばれていない

**解決策**:
1. 接続前に [Recording] チェックボックスがONになっているか確認
2. 必ず [Disconnect] ボタンで切断する（正常にメタデータが保存される）

## 参考資料

- [GlassBridge 記録・再生 API ガイド](../docs/recording/API_GUIDE.md)
- [GlassBridge README](../GlassBridge/README.md)
- [VITURE HID プロトコル仕様](../docs/hid/VITURE_Luma.md)

## まとめ

AxisVisualizer では GlassBridge の記録・再生機能を活用して:

- **記録モード**: デバイスデータを保存しながらリアルタイム表示
- **再生モード**: 保存されたデータを読み込んで再現
- **通常モード**: 記録なしでリアルタイム表示

これにより、デバッグ、テスト、データ分析が容易になります。
