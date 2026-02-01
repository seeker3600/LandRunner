# AxisVisualizer - VITURE XR グラス IMU ビューア

WPF で実装された VITURE XR グラス（Luma / Pro / One 系列）向けのリアルタイム IMU データビューア・ロガーです。

## 主な機能

- 📊 **リアルタイム IMU データ表示** - Euler 角度、Quaternion をダッシュボードで表示
- 🎯 **3D 回転ビジュアライゼーション** - XYZ 軸を Roll / Pitch / Yaw に基づいて回転表示
- 📝 **IMU データ自動記録** - GlassBridge の記録機能で JSON Lines 形式で自動保存
- 🔍 **Serilog ログ出力** - すべてのアクティビティを構造化ログで記録

## 要件

- **.NET 10** 以上
- **Windows** (WPF)
- **VITURE XR グラス** (Luma / Luma Pro / Pro / One / One Lite)

## 依存関係

- **GlassBridge** - IMU デバイス通信ライブラリ
- **Microsoft.Extensions.Logging** - ログ抽象化
- **Serilog** - 構造化ログ出力（ファイル・コンソール）

## プロジェクト構成（MVVM パターン）

```
AxisVisualizer/
├── ViewModels/
│   ├── ViewModelBase.cs          # MVVM 基本クラス（INotifyPropertyChanged）
│   ├── RelayCommand.cs           # ICommand 実装（同期・非同期対応）
│   └── MainWindowViewModel.cs    # 状態管理・GlassBridge 連携
├── MainWindow.xaml               # UI レイアウト（DataBinding）
├── MainWindow.xaml.cs            # 3D 軸ビジュアライゼーション描画
├── App.xaml
└── App.xaml.cs                   # Serilog 設定・DI
```

### コンポーネントの役割

| ファイル | 役割 |
|---------|------|
| **ViewModelBase.cs** | `INotifyPropertyChanged` 実装、プロパティ変更通知 |
| **RelayCommand.cs** | UI ボタン・メニュー操作のハンドリング（非同期対応） |
| **MainWindowViewModel.cs** | GlassBridge 接続・IMU データ更新・UI 状態管理 |
| **MainWindow.xaml.cs** | Canvas への XYZ 軸描画（Roll / Pitch / Yaw 回転） |

## 実行方法

```bash
# デバッグビルド＆実行
dotnet run --project AxisVisualizer

# リリースビルド＆実行
dotnet run --project AxisVisualizer --configuration Release
```

## ログ・記録ファイルの出力先

```
%APPDATA%\LandRunner\
├── log-<yyyyMMdd>.txt             Serilog ログファイル
└── imu_data_<yyyyMMdd_HHmmss>.jsonl   IMU データ記録（GlassBridge 自動生成）
```

### IMU データ記録形式

GlassBridge の `ConnectAndRecordAsync()` により、IMU データは **JSON Lines 形式** で自動記録されます：

```json
{"Timestamp":12345,"MessageCounter":100,"Quaternion":{"W":0.707107,"X":0.707107,"Y":0,"Z":0},"EulerAngles":{"Roll":45.0,"Pitch":30.0,"Yaw":15.0}}
{"Timestamp":12350,"MessageCounter":101,"Quaternion":{"W":0.707107,"X":0.707107,"Y":0,"Z":0},"EulerAngles":{"Roll":46.0,"Pitch":31.0,"Yaw":16.0}}
```

## GlassBridge との連携

本アプリケーションは **GlassBridge** ライブラリを使用して VITURE XR グラスと通信します：

```csharp
// 接続と同時に IMU データを自動記録
var manager = new ImuDeviceManager();
var device = await manager.ConnectAndRecordAsync(outputDirectory);

// IMU データストリームを非同期で取得
await foreach (var imuData in device.GetImuDataStreamAsync(cancellationToken))
{
    // Euler 角度・Quaternion を UI に反映
}
```

詳細は **GlassBridge/README.md** を参照してください。

## テスト

LandRunnerTest プロジェクトには、以下のテストクラス・テストケースが含まれています。

### テストクラス一覧（全 19 件）

| テストクラス | 対象クラス | テスト数 | 説明 |
|----------|-----------|--------|------|
| **ImuLoggerTests** | ImuLogger | 4 件 | デバッグログ出力機能・スレッド安全性 |
| **ImuDeviceManagerTests** | ImuDeviceManager | 1 件 | デバイスマネージャーのインスタンス化 |
| **MockImuDeviceTests** | MockImuDevice | 1 件 | モックデバイスのストリーム動作 |
| **ImuDataTests** | ImuData | 3 件 | IMU データ構造・値の精度 |
| **MainWindowViewModelTests** | MainWindowViewModel | 5 件 | ViewModel 状態管理・イベント |
| **RelayCommandTests** | RelayCommand | 5 件 | コマンド実行・非同期対応 |
| **合計** | | **19 件** | **すべて合格 ?** |

### テストケース詳細

#### ImuLoggerTests
- `ImuLogger_Initialize_CreatesLogFile` - ログファイルの作成を確認
- `ImuLogger_LogDebug_WritesMessage` - メッセージがファイルに記録されることを確認
- `ImuLogger_Dispose_ClosesFiles` - 破棄時にファイルがクローズされることを確認
- `ImuLogger_ThreadSafe_ConcurrentWrites` - マルチスレッド環境での安全性を確認

#### ImuDeviceManagerTests
- `ImuDeviceManager_CreateInstance_ShouldNotThrow` - インスタンス化が成功することを確認

#### MockImuDeviceTests
- `MockDevice_StreamData_ProducesData` - モック デバイスがデータを生成することを確認

#### ImuDataTests
- `ImuData_EulerAngles_ShouldBeAccurate` - Euler 角度計算の精度を確認
- `Quaternion_Operations_ShouldWork` - Quaternion 演算を確認
- `ImuData_Record_ShouldContainRequiredFields` - レコード型に必須フィールドが含まれることを確認

#### MainWindowViewModelTests
- `MainWindowViewModel_Initialize_DefaultValues` - 初期値が正しく設定されることを確認
- `MainWindowViewModel_PropertyChanged_RaisesEvent` - プロパティ変更時にイベントが発火することを確認
- `MainWindowViewModel_ConnectCommand_IsNotNull` - ConnectCommand が null でないことを確認
- `MainWindowViewModel_UpdateFromImuData_UpdatesProperties` - IMU データ更新時に UI が反映されることを確認
- `MainWindowViewModel_GetLastEulerAngles_ParsesCorrectly` - Euler 角度の解析精度を確認

#### RelayCommandTests
- `RelayCommand_Execute_InvokesAction` - コマンド実行がアクションを呼び出すことを確認
- `RelayCommand_CanExecute_ReturnsTrue_WhenNoCondition` - 条件なしで実行可能であることを確認
- `RelayCommand_CanExecute_RespectsPredicate` - 述語ロジックが正しく機能することを確認
- `AsyncRelayCommand_Execute_InvokesAsyncAction` - 非同期コマンド実行を確認
- `AsyncRelayCommand_CanExecute_ReturnsTrue_WhenNotExecuting` - 非同期実行中でない時は実行可能であることを確認

## 依存関係と技術スタック

| 項目 | 説明 |
|------|------|
| **フレームワーク** | .NET 10.0-windows7.0 |
| **UI フレームワーク** | WPF (Windows Presentation Foundation) |
| **IMU ライブラリ** | GlassBridge (このソリューション内) |
| **テストフレームワーク** | xUnit + 標準ユニットテスト |
| **アーキテクチャ** | MVVM (Model-View-ViewModel) |

## MVVM パターンの設計

LandRunner は MVVM パターンを採用し、関心の分離を実現しています：

### 役割分担

| レイヤー | 担当 | 主要クラス |
|--------|------|-----------|
| **Model** | データ・ビジネスロジック | DebugLogger、ImuData |
| **ViewModel** | 状態管理・コマンド処理 | MainWindowViewModel、RelayCommand |
| **View** | UI 表示 | MainWindow.xaml |

### 通信フロー

```
User → View (XAML) 
          ↓
       MainWindow.xaml.cs (最小限)
          ↓
       ViewModel (MainWindowViewModel)
          ↓
       GlassBridge (IMU 取得・記録)
          ↓
       Model (DebugLogger、ImuData)
```

### DataBinding の活用

ViewModelのプロパティ変更を XAML DataBinding で自動検出：

```xml
<TextBlock Text="{Binding RollText}" />
<TextBlock Text="{Binding PitchText}" />
<TextBlock Text="{Binding YawText}" />
```

詳細は `MainWindow.xaml` を参照。

## GlassBridge との統合

### 従来のアプローチ
```
LandRunner → IMU データ受信 → 手動で CSV 記録
```

### 統合後のアプローチ
```
ConnectAndRecordAsync()
    ↓
GlassBridge が自動記録 → imu_data_*.jsonl
    ↓
LandRunner は表示とログ出力のみに特化
```

メリット：
- ?? LandRunner が簡潔になる（表示・ログのみ）
- ?? GlassBridge が標準化した JSON Lines 形式で記録
- ?? 再生機能（ConnectFromRecordingAsync()）で簡単にリプレイ

詳細は **GlassBridge/RECORDING_API_GUIDE.md** を参照。

