# LandRunner 実装完了報告

## ? 実装完了内容

### 1. **IMU リアルタイム表示アプリケーション**
- VITURE Luma からのIMUデータをWPFで表示
- リアルタイムステータス表示（ステータスバー）
- 3D軸の可視化（X/Y/Z軸と Yaw 回転軸）
- Euler角度とQuaternion値の表示

### 2. **ログ出力機能**
- **デバッグログ**: `debug_<timestamp>.log` - 日時付きログ
- **IMUデータ CSV**: `imu_data_<timestamp>.csv` - センサーデータを CSV形式で記録
- 保存先: `%AppData%/LandRunner/`

### 3. **MVVM パターン（ベストプラクティス）**
```
LandRunner/
├── Models/ImuLogger.cs              ← ロギング・データ出力
├── ViewModels/
│   ├── ViewModelBase.cs             ← INotifyPropertyChanged実装
│   ├── RelayCommand.cs              ← ICommand (同期・非同期対応)
│   └── MainWindowViewModel.cs       ← 状態管理・ロジック
└── Views/
    ├── MainWindow.xaml              ← UIレイアウト（DataBinding）
    └── MainWindow.xaml.cs           ← CodeBehind（最小限）
```

### 4. **テスト（全10件 ? 合格）**

#### ImuLoggerTests (5件)
- `ImuLogger_Initialize_CreatesLogFiles` ?
- `ImuLogger_LogDebug_WritesMessage` ?
- `ImuLogger_LogImuData_WritesCsvRow` ?
- `ImuLogger_MultipleDataPoints_PreservesOrder` ?
- `ImuLogger_Dispose_ClosesFiles` ?

#### DeviceConnectionIntegrationTests (4件)
- `ImuDeviceManager_CreateInstance_ShouldNotThrow` ?
- `MockDevice_StreamData_ProducesData` ?
- `ImuData_EulerAngles_ShouldBeAccurate` ?
- `Quaternion_Operations_ShouldWork` ?

#### LoggerThreadSafetyTests (1件)
- `ImuLogger_ConcurrentWrites_ShouldNotCorrupt` ?

**テスト結果: 成功 10/10 (失敗 0)**

---

## ?? 主要機能

### ステータスバー（上部）
- 接続状態表示
- リアルタイムメッセージカウント
- タイムスタンプ表示

### ビジュアライゼーション（左側）
- X/Y/Z軸の描画（赤/緑/青）
- Yaw角度による回転軸表示（紫色）
- 原点マーク

### データ表示パネル（右側）
- **Euler Angles**: Roll, Pitch, Yaw（度）
- **Quaternion**: W, X, Y, Z
- **メタデータ**: Timestamp, Message Counter

### コントロール（下部）
- Connect Device ボタン
- Disconnect ボタン
- ステータステキスト

---

## ??? MVVM パターンの実装

### ViewModelBase
```csharp
public class ViewModelBase : INotifyPropertyChanged
{
    // PropertyChanged イベント自動管理
    // SetProperty<T>() で変更検知と通知を自動化
}
```

### RelayCommand
```csharp
// 非同期コマンド対応
public class AsyncRelayCommand : ICommand
{
    // データバインド → コマンド実行 → 非同期処理
}
```

### MainWindowViewModel
- `StatusText`, `RollText`, `YawText` などのプロパティ
- `ConnectCommand`, `DisconnectCommand`
- `UpdateFromImuData()` でデータ更新を自動反映

---

## ?? ログ出力例

### debug_20260126_214611.log
```
[2026-01-26 21:46:11.234] ImuLogger initialized
[2026-01-26 21:46:11.235] Debug log: C:\Users\...\debug_20260126_214611.log
[2026-01-26 21:46:11.236] IMU data log: C:\Users\...\imu_data_20260126_214611.csv
[2026-01-26 21:46:12.102] Starting device connection
[2026-01-26 21:46:12.500] Successfully connected to device
```

### imu_data_20260126_214611.csv
```csv
Timestamp,MessageCounter,Yaw,Pitch,Roll,W,X,Y,Z
12345,100,15.123456,30.456789,45.789012,0.707107,0.707107,0.000000,0.000000
12350,101,16.234567,31.567890,46.890123,0.707107,0.707107,0.000000,0.000000
...
```

---

## ?? テスト実行方法

```bash
# すべてのテスト実行
dotnet test

# LandRunnerTest のみ実行
dotnet test LandRunnerTest

# 詳細出力
dotnet test --verbosity detailed
```

---

## ?? 実行方法

```bash
# アプリケーション実行
dotnet run --project LandRunner

# またはビルド後、EXEを直接実行
LandRunner\bin\Debug\net10.0-windows\LandRunner.exe
```

---

## ?? ファイル構造

```
LandRunner/
├── Models/
│   └── ImuLogger.cs                 # ログ・CSV出力
├── ViewModels/
│   ├── ViewModelBase.cs             # INotifyPropertyChanged
│   ├── RelayCommand.cs              # ICommand実装
│   └── MainWindowViewModel.cs       # ビジネスロジック・状態管理
├── Views/ (または Views フォルダ)
│   ├── MainWindow.xaml              # UI定義
│   └── MainWindow.xaml.cs           # CodeBehind
├── ImuLogger.cs                     # 互換性用（ルート）
├── README.md                        # このドキュメント
└── app.xaml, App.xaml.cs

LandRunnerTest/
├── UnitTest1.cs                     # テストスイート（10件）
└── LandRunnerTest.csproj
```

---

## ?? MVVM パターンの利点

1. **テスト容易性**: ViewModel のみをテスト可能
2. **UI/ロジック分離**: MainWindow.xaml.cs が薄い（コードビハインド最小化）
3. **保守性向上**: 責任が明確に分離
4. **再利用性**: ViewModel は別の View でも使用可能
5. **DataBinding**: 宣言的 UI 更新

---

## ? 今後の拡張案

1. **グラフ表示**: 加速度・角速度のリアルタイムグラフ
2. **キャリブレーション**: センサーキャリブレーション機能
3. **複数デバイス**: 複数 VITURE デバイスの同時接続対応
4. **ネットワーク**: UDP/TCP でのデータ送信
5. **録画・再生**: IMU データの録画・再生機能

---

## ?? 技術スタック

- **Framework**: .NET 10.0
- **UI**: WPF (Windows Presentation Foundation)
- **パターン**: MVVM (Model-View-ViewModel)
- **テスト**: XUnit 2.9.3
- **デバイス通信**: GlassBridge（HID経由）

---

## ?? 注記

- ログファイルはクリアな手で複数スレッドからの安全な書き込みに対応（`lock` で同期）
- ファイルハンドルは明示的にフラッシュして確実にクローズ
- テスト後は GC による確実なリリースを待つ

---

**実装完了日**: 2026年01月26日
**ステータス**: ? 完成・テスト合格
