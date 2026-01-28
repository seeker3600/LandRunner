# LandRunner - VITURE Luma IMU Viewer

WPF で実装された VITURE Luma IMU センサーのリアルタイム表示・ログ出力アプリケーションです。

## 機能

- **リアルタイムIMUデータ表示**：Euler角度、Quaternionをリアルタイムで表示
- **3D軸可視化**：XYZ軸とYaw角度に基づく回転軸を画面に表示
- **自動IMUデータ記録**：GlassBridgeの記録機能を使用してIMUデータをJSON Lines形式で記録
- **デバッグログ出力**：すべてのアクティビティを日時付きログファイルに記録

## プロジェクト構成（MVVM パターン）

```
LandRunner/
├── Models/
│   └── DebugLogger.cs          # デバッグログ出力
├── ViewModels/
│   ├── ViewModelBase.cs        # MVVM基本クラス（INotifyPropertyChanged）
│   ├── RelayCommand.cs         # ICommand実装（同期・非同期）
│   └── MainWindowViewModel.cs  # 状態管理・ビジネスロジック
├── MainWindow.xaml             # UIレイアウト（DataBinding）
├── MainWindow.xaml.cs          # CodeBehind（最小限：ビジュアル化のみ）
├── ImuLogger.cs                # DebugLoggerのエイリアス
└── App.xaml, App.xaml.cs
```

## 使用方法

### 実行

```bash
dotnet run --project LandRunner
```

### テスト実行

```bash
dotnet test LandRunnerTest
```

## ログ出力先

ロギングデータは以下に保存されます：

```
C:\Users\<User>\AppData\Roaming\LandRunner\
├── debug_<yyyyMMdd_HHmmss>.log              # デバッグログ（日時付き）
└── <IMU記録ファイル>                        # GlassBridgeが自動生成
```

### デバッグログ形式

```
[2026-01-26 21:46:11.234] ImuLogger initialized
[2026-01-26 21:46:11.235] Debug log: C:\Users\...\debug_20260126_214611.log
[2026-01-26 21:46:11.236] Recording IMU data to: C:\Users\...\LandRunner
[2026-01-26 21:46:12.500] Successfully connected to device
[2026-01-26 21:46:12.501] Disposing device (GlassBridge will finalize recording)
```

### IMUデータ記録

IMUデータはGlassBridgeの記録機能により、JSON Lines形式で自動記録されます：
```
{"Timestamp":12345,"MessageCounter":100,"Quaternion":{"W":0.707107,"X":0.707107,"Y":0,"Z":0},"EulerAngles":{"Roll":45.0,"Pitch":30.0,"Yaw":15.0}}
{"Timestamp":12350,"MessageCounter":101,"Quaternion":{"W":0.707107,"X":0.707107,"Y":0,"Z":0},"EulerAngles":{"Roll":46.0,"Pitch":31.0,"Yaw":16.0}}
```

## テスト

`LandRunnerTest` プロジェクトに以下のテストクラスが含まれます（1対1対応）：

### テストクラス一覧

| テストクラス | 対象クラス | テスト数 | 説明 |
|----------|-----------|--------|------|
| `ImuLoggerTests` | `ImuLogger` | 4件 | デバッグログ出力機能 |
| `ImuDeviceManagerTests` | `ImuDeviceManager` | 1件 | デバイスマネージャー |
| `MockImuDeviceTests` | `MockImuDevice` | 1件 | モックデバイス |
| `ImuDataTests` | `ImuData` | 3件 | IMUデータ構造 |
| `MainWindowViewModelTests` | `MainWindowViewModel` | 5件 | ViewModel状態管理 |
| `RelayCommandTests` | `RelayCommand` | 5件 | コマンド実装 |

**合計: 19件のテスト ? すべて合格**

### テスト内容

#### ImuLoggerTests
- `ImuLogger_Initialize_CreatesLogFile` - ログファイルの作成確認
- `ImuLogger_LogDebug_WritesMessage` - メッセージ記録の確認
- `ImuLogger_Dispose_ClosesFiles` - ファイルクローズの確認
- `ImuLogger_ThreadSafe_ConcurrentWrites` - マルチスレッド安全性

#### ImuDeviceManagerTests
- `ImuDeviceManager_CreateInstance_ShouldNotThrow` - インスタンス化確認

#### MockImuDeviceTests
- `MockDevice_StreamData_ProducesData` - モックストリーム動作

#### ImuDataTests
- `ImuData_EulerAngles_ShouldBeAccurate` - Euler角度の精度
- `Quaternion_Operations_ShouldWork` - Quaternion演算
- `ImuData_Record_ShouldContainRequiredFields` - 必須フィールド確認

#### MainWindowViewModelTests
- `MainWindowViewModel_Initialize_DefaultValues` - 初期値確認
- `MainWindowViewModel_PropertyChanged_RaisesEvent` - プロパティ変更イベント
- `MainWindowViewModel_ConnectCommand_IsNotNull` - コマンド存在確認
- `MainWindowViewModel_UpdateFromImuData_UpdatesProperties` - データ反映
- `MainWindowViewModel_GetLastEulerAngles_ParsesCorrectly` - パース精度

#### RelayCommandTests
- `RelayCommand_Execute_InvokesAction` - 実行確認
- `RelayCommand_CanExecute_ReturnsTrue_WhenNoCondition` - 実行可能確認
- `RelayCommand_CanExecute_RespectsPredicate` - 述語ロジック
- `AsyncRelayCommand_Execute_InvokesAsyncAction` - 非同期実行
- `AsyncRelayCommand_CanExecute_ReturnsTrue_WhenNotExecuting` - 非同期実行可能確認

## 依存関係

- **GlassBridge**: IMUセンサーとの通信（HID経由・自動記録機能付き）
- **.NET 10.0**
- **WPF** (Windows Presentation Foundation)

## MVVM パターンの特徴

1. **ViewModelBase**：`INotifyPropertyChanged`を実装し、UI更新を自動化
2. **RelayCommand**：ICommandの非同期対応実装
3. **DataBinding**：XAMLでViewModelプロパティにバインド
4. **関心の分離**：
   - UI ロジック → ViewModel
   - 表示のみ → View (XAML)
   - ビジュアル化 → CodeBehind（最小限）

## GlassBridgeの記録機能の利用

### 従来のアプローチ
```
LandRunner → IMUデータ受信 → CSV手動記録
```

### 新しいアプローチ（GlassBridge統合）
```
ConnectAndRecordAsync() → GlassBridgeが自動記録 → JSON Lines形式
                          │
                          └─ `imu_data_*.jsonl` ファイル自動生成
```

このアプローチにより：
- ? LandRunnerはシンプルになる（ログ記録のみ）
- ? GlassBridgeが標準化されたフォーマットで記録
- ? 再生機能（`ConnectFromRecordingAsync()`）で簡単にリプレイ可能

---

**実装完了日**: 2026年01月26日
**ステータス**: ? 完成・テスト合格

