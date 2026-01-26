# LandRunner

LandRunner は、VITURE デバイスを操作するための C# ライブラリです。HID インターフェースを通じて IMU センサーデータを取得・記録・再生できます。

## セットアップ

### 必要な環境
- .NET 10 以上
- Visual Studio 2022 以上（推奨）

### インストール

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

## プロジェクト構成

| プロジェクト | 説明 |
|-----------|------|
| **GlassBridge** | VITURE デバイス操作の公開 API |
| **GlassBridgeTest** | ユニット・統合テスト |

## ドキュメント参照ガイド

### HID プロトコル実装に関わる場合
- **docs/hid/VITURE_Luma.md** - VITURE デバイスの USB VID/PID、HID インターフェース構成、プロトコル仕様

### GlassBridge API を使用する場合
- **GlassBridge/README.md** - 公開インターフェース説明、プロジェクト構成、依存関係

### IMU データ記録・再生機能に関わる場合
- **GlassBridge/RECORDING_API_GUIDE.md** - 記録・再生の使用方法、ファイル形式説明

### 参考資料
- **docs/archive/** - 過去の実装報告書・作業ログ（参考用のみ）

## コード品質

- すべての新機能はテストコード付きで追加
- `.github/copilot-instructions.md` のコーディング規則に従う
- ドキュメント配置ルール（§2.1）を遵守

## ライセンス

詳細はリポジトリを参照してください。