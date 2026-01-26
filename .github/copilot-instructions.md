# Copilot 作業指示書

LandRunner プロジェクトの開発ガイドラインです。

---

## 1. C# コーディング規則

### 一般的なガイドライン
- Visual Studio のベストプラクティスに従う
- ファイル階層と名前空間を一貫させる
- 既存のコード規約に合わせる

### プロジェクト固有のルール
- **関心の分離**: 公開API（`GlassBridge` 名前空間）と内部実装（`GlassBridge.Internal` 名前空間）を厳密に分離
  - 内部実装ファイルは `Internal/` フォルダに配置
  - 公開インターフェースのみ `ImuDeviceManager`, `IImuDevice` など をルート直下に配置
- **VITURE デバイス固有の知識**は `GlassBridge.Internal.HID.VitureDeviceIdentifiers` で一元管理
  - ベンダーID、プロダクトID はこの定数クラスを参照
  - 新しいモデル追加時はこのファイルのみ修正
- **非同期処理**: `IAsyncDisposable` を活用し、リソースの自動クリーンアップを実装

---

## 2. ドキュメント構成ルール

### 2.1 ドキュメント配置ルール

**基本原則**: ドキュメント位置の統一により、参照性と保守性を向上させる

| 対象 | 配置場所 | 用途 |
|------|---------|------|
| **README.md** | ソリューション直下 | プロジェクト全体の概要・セットアップ手順 |
| **プロジェクト README.md** | `GlassBridge/` など | 各プロジェクトの概要・依存関係・API 説明 |
| **ARCHITECTURE.md** | プロジェクトフォルダ（オプション） | アーキテクチャ設計・モジュール責務 |
| **その他ドキュメント** | `docs/` サブフォルダ | ドメイン知識・プロトコル仕様・ガイド |

### 2.2 既存ドキュメント一覧

#### ソリューション直下
- **README.md** - プロジェクト全体の紹介・セットアップ方法
- **docs/hid/VITURE_Luma.md** - VITURE HID プロトコル仕様（重要）
- **docs/** - ドメイン知識・プロトコル仕様・実装ガイド

#### プロジェクトレベル（GlassBridge）
- **GlassBridge/README.md** - GlassBridge 公開API の説明

#### ドメイン知識・実装ガイド（docs/）
- **docs/hid/VITURE_Luma.md** - VITURE HID プロトコル仕様
- **docs/recording/API_GUIDE.md** - IMU データ記録・再生機能の使用ガイド
- **docs/recording/IMPLEMENTATION.md** - 記録機能の内部実装説明

---

## 3. 参照すべきドキュメント

### HID プロトコル実装に関わる場合
?? **docs/hid/VITURE_Luma.md**
- VITURE デバイスの USB VID/PID
- HID インターフェース構成（IMU/MCU の2つのストリーム）
- プロトコル仕様・パケット形式
- デバイス識別ロジック

### GlassBridge API を使用する場合
?? **GlassBridge/README.md**
- 公開インターフェース（`IImuDeviceManager`, `IImuDevice` 等）
- プロジェクト構成・依存関係

### IMU データ記録・再生機能に関わる場合
?? **docs/recording/API_GUIDE.md**
- `ImuDeviceManager.ConnectAndRecordAsync()` の使用方法
- `ImuDeviceManager.ConnectFromRecordingAsync()` の使用方法
- 記録ファイル形式（JSON Lines）

---

## 4. コード品質

### テスト方針
- `GlassBridgeTest/` ユニット・統合テストを実装
- 新機能追加時はテストコードも一緒に追加
- モック（`MockHidStreamProvider` 等）を活用して実デバイス不要で検証

### パッケージ管理
- `HidSharp` - HID デバイス通信（現在 2.6.4）
- 新しいパッケージ追加時は互換性確認（特に .NET 10 対応状況）

---

## 5. 開発ワークフロー

### 新機能追加時
1. 対応するドキュメント（README.md, ARCHITECTURE.md 等）を先に更新 or 作成
2. テストコード → 実装コード の順で開発
3. プロジェクト構成の一貫性を確認（Internal/Public の分離など）
4. ビルド・テスト合格を確認後にコミット

### ドキュメント追加時
1. 配置ルール（§2.1）に従い配置場所を決定
2. 既存ドキュメント（§2.2）と重複がないか確認
