# LandRunner IMUデータ記録フロー（GlassBridge統合版）

## ?? 高レベルアーキテクチャ図

```
┌──────────────────────────────────────────────────────────┐
│         LandRunner WPF Application                       │
└──────────────────────────────────────────────────────────┘
                        │
        ┌───────────────┼───────────────┐
        │               │               │
        ▼               ▼               ▼
    ┌─────────┐  ┌──────────┐  ┌──────────────┐
    │MainWnd  │  │ViewModels│  │DebugLogger   │
    │ (XAML)  │  │(Logic)   │  │(File Output) │
    └────┬────┘  └────┬─────┘  └──────┬───────┘
         │            │               │
         └────────────┼───────────────┘
                      │
         ┌────────────▼──────────────┐
         │  GlassBridge Library      │
         │  (ImuDeviceManager)       │
         └────────┬─────────┬────────┘
                  │         │
         ┌────────▼─┐  ┌────▼──────────┐
         │HID Comm  │  │Recording      │
         │(USB)     │  │(JSON Lines)   │
         └────┬─────┘  └────┬──────────┘
              │             │
         ┌────▼─────────────▼─────┐
         │  imu_data_<ts>.jsonl   │
         │  (自動生成)             │
         └────────────────────────┘
```

## ?? IMUデータ記録フロー（シーケンス図）

```
ユーザー         LandRunner         GlassBridge         ファイルシステム
  │                 │                   │                     │
  │  [Connect]      │                   │                     │
  ├────────────────→│                   │                     │
  │                 │                   │                     │
  │                 │ ConnectAndRecord  │                     │
  │                 ├──────────────────→│                     │
  │                 │                   │                     │
  │                 │                   │ Open HID Device     │
  │                 │                   ├────────────────────→│
  │                 │                   │ (VITURE Luma)       │
  │                 │                   │                     │
  │                 │                   │ Create Recording    │
  │                 │                   ├────────────────────→│
  │                 │                   │ (imu_data_*.jsonl)  │
  │                 │  ? Connected      │                     │
  │                 │←──────────────────┤                     │
  │  ? Ready        │                   │                     │
  │←────────────────┤                   │                     │
  │                 │                   │                     │
  ├─ (??? ?? ??)                   │                     │
  │                 │ GetImuDataStream()│                     │
  │                 ├──────────────────→│                     │
  │                 │                   │                     │
  │                 │                   │  Read Sensor Data   │
  │                 │                   ├────────────────────→│
  │                 │                   │ (Euler, Quaternion) │
  │                 │                   │                     │
  │                 │ ImuData #1        │                     │
  │                 │←──────────────────┤  Auto-Record JSON   │
  │                 │                   │←────────────────────┤
  │  ?? Display     │                   │                     │
  │←────────────────┤                   │                     │
  │                 │                   │                     │
  │                 │ ImuData #2        │                     │
  │                 │←──────────────────┤  Auto-Record JSON   │
  │  ?? Display     │                   │←────────────────────┤
  │←────────────────┤                   │                     │
  │                 │                   │                     │
  │                 │  (継続...)          │                     │
  │                 │                   │                     │
  │  [Disconnect]   │                   │                     │
  ├────────────────→│                   │                     │
  │                 │                   │                     │
  │                 │ Finalize Recording│                     │
  │                 ├──────────────────→│                     │
  │                 │                   │                     │
  │                 │                   │ Close File/HID      │
  │                 │                   ├────────────────────→│
  │                 │                   │ (Flush JSON Lines)  │
  │                 │  ? Done           │                     │
  │                 │←──────────────────┤                     │
  │  ? Disconnected │                   │                     │
  │←────────────────┤                   │                     │
  │                 │                   │                     │
```

## ?? ファイル出力構造

```
%AppData%/LandRunner/
│
├─ debug_20260126_214611.log
│  ├─ [2026-01-26 21:46:11.110] ImuLogger initialized
│  ├─ [2026-01-26 21:46:11.111] Debug log: C:\Users\...\debug_20260126_214611.log
│  ├─ [2026-01-26 21:46:11.112] Recording IMU data to: C:\Users\...\LandRunner
│  ├─ [2026-01-26 21:46:12.200] Starting device connection with GlassBridge recording
│  ├─ [2026-01-26 21:46:12.500] Successfully connected to device
│  ├─ [2026-01-26 21:46:12.501] Recording IMU data to: C:\Users\...\LandRunner
│  └─ [2026-01-26 21:46:20.000] Device disconnected
│
└─ imu_data_20260126_214611.jsonl
   ├─ {"Timestamp":12345,"MessageCounter":100,"Quaternion":{...},"EulerAngles":{...}}
   ├─ {"Timestamp":12350,"MessageCounter":101,"Quaternion":{...},"EulerAngles":{...}}
   └─ ...
```

## ?? スレッドセーフな処理フロー

```
Main Thread              Worker Thread (Streaming)
    │                              │
    ├─ DebugLogger                 │
    │  └─ lock()                   │
    │     └─ LogDebug()            │
    │        └─ unlock()           │
    │                              │
    │                    ┌─────────┼──────────┐
    │                    │         │          │
    │                    ▼         ▼          ▼
    │              Thread 1    Thread 2   Thread 3
    │              (Recv #1)  (Recv #2)  (Display)
    │                │         │          │
    │                ├─ lock() ─┤         │
    │                │  LogImu  │         │
    │                │  Flush   │         │
    │                └─ unlock()─┤         │
    │                │          ├─────────→ Update UI
    │                │          │
    │                │          ├─ lock() ─┐
    │                │          │  LogImu  │
    │                │          │  Flush   │
    │                │          └─ unlock()─┤
    │                │                     │
    │                                      ▼
    │                            imu_data_*.jsonl
    │                            (ディスク書き込み)
    │
    │ [Disconnect]
    │     │
    │     └─ cancel token
    │        │
    │        └─ DebugLogger.Dispose()
    │           └─ Flush & Close all files
    │
    ▼
```

## ?? データフロー（GlassBridge記録機能）

```
1??  接続フェーズ

    ConnectAndRecordAsync(outputDirectory)
         │
         └─→ RecordingHidStreamProvider 生成
             ├─ outputDirectory を設定
             └─ HID ストリーム = 記録ラッパー

2??  ストリーミングフェーズ

    GetImuDataStreamAsync()
         │
         ├─ VITURE Luma → HID パケット受信
         │
         ├─ ImuData パース
         │
         ├─ JSON Lines フォーマット
         │  └─ {"Timestamp":...,"MessageCounter":...,"Quaternion":{...},"EulerAngles":{...}}
         │
         ├─ ファイル書き込み（自動）
         │  └─ imu_data_<timestamp>.jsonl に追加
         │
         └─ LandRunner に返却
            └─ UI更新 & DebugLogger.LogDebug()

3??  終了フェーズ

    device.DisposeAsync()
         │
         └─ RecordingHidStreamProvider.FinalizeRecordingAsync()
            ├─ JSON Lines ファイル Flush
            ├─ HID ストリーム Close
            └─ ファイルハンドル Release

```

## ? 改善点（旧版との比較）

| 項目 | 旧版 | 新版（GlassBridge統合） |
|------|------|----------------------|
| **IMUデータ記録** | 手動（CSV形式） | 自動（JSON Lines） |
| **DebugLogger責務** | 1. IMU記録 + 2. ログ | ログのみ |
| **GlassBridge活用** | 接続のみ | 接続 + 記録 + 再生 |
| **コード量** | 多い（CSV出力ロジック） | 少ない（シンプル） |
| **標準化** | 独自形式 | JSON Lines標準 |
| **再生機能** | なし | `ConnectFromRecordingAsync()` で可能 |

---

**設計更新日**: 2026年01月26日
**ステータス**: ? GlassBridge統合版として完成
