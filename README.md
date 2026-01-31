# LandRunner

LandRunner は、VITURE XR グラスをホスト OS から制御するための .NET ライブラリおよび WPF アプリケーションです。

## 概要

### ?? GlassBridge ライブラリ
VITURE デバイス（Luma、Luma Pro、Pro、One、One Lite など）と USB HID インターフェース経由で通信し、3DoF の IMU（姿勢）データを取得・記録・再生します。テスト用のモック実装も含まれており、本体デバイスがなくても開発・検証が可能です。

### ?? LandRunner WPF アプリケーション
GlassBridge を使用した、VITURE Luma 向けのリアルタイム IMU データビューア・ロガー。Euler 角・Quaternion の表示、3D 回転ビジュアライゼーション、デバッグログ出力に対応しています。

## セットアップ

### 必要な環境
- **.NET 10** 以上
- **Visual Studio 2022** 以上（推奨）
- **Windows**（USB HID 通信のため）

### インストール・ビルド

1. リポジトリをクローン：
   ```bash
   git clone https://github.com/seeker3600/LandRunner.git
   cd LandRunner
   ```

2. プロジェクトをビルド：
   ```bash
   dotnet build
   ```

3. テストを実行：
   ```bash
   dotnet test
   ```

4. LandRunner アプリを実行（Windows のみ）：
   ```bash
   dotnet run --project LandRunner
   ```

## プロジェクト構成

| プロジェクト | 説明 | ターゲットフレームワーク |
|-----------|------|----------------------|
| **GlassBridge** | 公開 API（ImuDeviceManager、ImuData など） | net10.0-windows7.0 |
| **GlassBridgeTest** | ユニット・統合テスト | net10.0-windows7.0 |
| **LandRunner** | WPF IMU ビューア・ロガー | net10.0-windows7.0 |
| **LandRunnerTest** | LandRunner のテスト | net10.0-windows7.0 |

## ?? ドキュメント構成と参照ガイド

### ?? 参照先別ガイド

#### HID プロトコル仕様を確認する
→ **docs/hid/VITURE_Luma.md**
- VITURE デバイスの USB Vendor ID / Product ID
- HID インターフェース（IMU/MCU）の構成・パケット構造
- プロトコル仕様・制御コマンド
- デバイス別の互換性情報

#### GlassBridge API を使用する
→ **GlassBridge/README.md**
- 公開インターフェース（IImuDeviceManager、IImuDevice など）
- プロジェクト構成・フォルダ配置
- クイックスタート例・基本的な使い方

#### IMU データ記録・再生機能の詳細
→ **GlassBridge/RECORDING_API_GUIDE.md**
- `ConnectAndRecordAsync()` の使用方法
- `ConnectFromRecordingAsync()` の使用方法
- 記録ファイル形式（JSON Lines）詳細

#### LandRunner アプリケーション
→ **LandRunner/README.md**
- 機能概要（リアルタイム表示、3D ビジュアライゼーション、ロギング）
- MVVM アーキテクチャ・フォルダ構成
- 実行方法・ログ出力先
- テスト一覧・テストケース

### ?? ドキュメント配置ルール

| ドキュメント | 配置先 | 用途 |
|-----------|-------|------|
| **README.md** | ソリューションルート | プロジェクト全体の概要・セットアップ・ドキュメントガイド |
| **GlassBridge/README.md** | `GlassBridge/` | GlassBridge の公開 API 仕様・使用例 |
| **GlassBridge/RECORDING_API_GUIDE.md** | `GlassBridge/` | 記録・再生機能の詳細ガイド |
| **LandRunner/README.md** | `LandRunner/` | LandRunner アプリの説明・機能・ロギング情報 |
| **docs/hid/VITURE_Luma.md** | `docs/hid/` | VITURE HID プロトコル仕様（基本資料）|
| **その他ドキュメント** | `docs/` サブフォルダ | ドライバー情報・プロトコル詳細・実装ガイド |

## ?? コード管理方針

### 開発時のルール
1. **テストコード** - 新機能を追加する場合は、テストコードを合わせて実装
2. **コーディング規則** - `.github/copilot-instructions.md` に従う
3. **ドキュメント** - 新機能追加時は対応する README.md や ARCHITECTURE.md を更新
4. **モジュール構成** - GlassBridge では、公開 API（直下）と内部実装（Internal/）を明確に分離

### パッケージ管理
- **HidSharp** 2.6.4 - USB HID デバイス通信
- 新しいパッケージ追加時は .NET 10 互換性を確認

## ?? 相互参照（技術者向け）

- **GlassBridge で HID 低レベル実装を変更する** → docs/hid/VITURE_Luma.md を参照して仕様を確認
- **VITURE デバイスの対応状況を確認する** → docs/hid/VITURE_Luma.md（第2章）を確認
- **LandRunner で GlassBridge を使用する** → GlassBridge/README.md を参照
- **新しい記録形式を追加する** → GlassBridge/RECORDING_API_GUIDE.md を参照

## ライセンス

詳細はリポジトリを参照してください。