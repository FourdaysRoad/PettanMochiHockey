# プロジェクト技術スタック定義

## 🔧 生成情報
- **生成日**: 2026-11-17
- **生成ツール**: init-tech-stack（Unity ゲーム向けに調整）
- **プロジェクトタイプ**: PCブラウザ向け WebGL ゲーム（ローカル対戦のみ）
- **チーム規模**: 個人開発
- **開発期間**: 1〜2ヶ月

## 🎯 プロジェクト要件サマリー
- **パフォーマンス**: 中程度のPCで4人対戦中60fps、入力遅延3フレーム以内（詳細は仕様書 6章）
- **セキュリティ**: 基本レベル（サーバー・個人情報・認証なし。静的ホスティングのみ）
- **技術スキル**: Unity / C# は学習しながら進める
- **学習コスト許容度**: 安定技術優先（Unity 標準パッケージ中心）
- **デプロイ先**: GitHub Pages
- **予算**: コスト最小化（GitHub Free、Unity Personal）

## 🎮 ゲームクライアント
- **エンジン**: Unity 6000.6.8f1（`ProjectSettings/ProjectVersion.txt` に固定）
- **レンダーパイプライン**: URP 17.6
- **言語**: C#
- **ビルドターゲット**: WebGL（WebGL 2.0）
- **物理**: Unity Physics（PhysX）
- **入力**: Input System 1.20（PlayerInputManager による参加受付）
- **カメラ**: Cinemachine 3.x（Target Group による自動ズーム）※要追加
- **UI**: 併用
  - メニュー系（タイトル・対戦設定・ポーズ・リザルト）: UI Toolkit
  - 試合中のワールド空間UI（餅の頭上ゲージ、足元リング、スタミナ/チャージ）: uGUI（World Space Canvas）
  - スコア・タイマーの画面上部HUD: UI Toolkit（uGUI でも可。実装時に統一）
- **調整パラメータ**: `GameBalanceConfig`（ScriptableObject）に集約。コードに数値を直接書かない
- **設定の保存**: PlayerPrefs のみ

### 選択理由
- 仕様書・CLAUDE.md で Unity / URP / WebGL / GitHub Pages が確定している
- Unity 標準パッケージのみで構成でき、学習資料が多く個人開発で扱いやすい
- ゲームロジックを UnityEngine 非依存の Core 層に分けることで、Unity に不慣れでもテストで挙動を確かめながら進められる

## ⚙️ バックエンド / 💾 データベース
- **なし**。ローカル対戦のみで、アカウント・ランキング・オンライン機能は対象外
- 永続化が必要なのは設定値のみ（PlayerPrefs）

## 🏗️ アーキテクチャ（Assembly Definition）

| アセンブリ | 内容 | 依存先 |
|---|---|---|
| `MochiHockey.Core` | 純粋なロジック（`noEngineReferences: true`） | なし |
| `MochiHockey.Runtime` | MonoBehaviour、物理、入力、VFX、UI、ScriptableObject | Core |
| `MochiHockey.Tests.EditMode` | Core の単体テスト | Core |
| `MochiHockey.Tests.PlayMode` | 物理挙動・統合テスト | Runtime, Core |

- Core 層：得点計算、餅の成長判定、Pettan ステートの時間進行、試合タイマー、ルール判定
- MonoBehaviour は入力・物理・見た目と Core 層をつなぐ薄い層にとどめる

## 🛠️ 開発環境
- **エディタ**: Unity Hub + Unity 6000.6.8f1
- **IDE**: Visual Studio Code / Visual Studio / Rider（パッケージ導入済み）
- **パッケージ管理**: Unity Package Manager（`Packages/manifest.json`）

### 追加が必要なパッケージ
- `com.unity.test-framework`（Unity Test Framework）
- `com.unity.cinemachine`（Cinemachine 3.x）

### 開発ツール
- **テストフレームワーク**: Unity Test Framework（NUnit）
  - EditMode: Core 層の TDD（先にテストを書く）
  - PlayMode: 物理・統合動作
- **テスト実行（ローカル）**:
  ```
  Unity -batchmode -runTests -testPlatform EditMode -projectPath . -testResults results.xml
  ```
- **コード品質**: IDE のアナライザー（.NET 標準）。追加のリンターは導入しない

### CI/CD
- **CI/CD**: GitHub Actions + GameCI
  - Pull Request: EditMode / PlayMode テスト（`game-ci/unity-test-runner`）
  - main への push: テスト → WebGL ビルド（`game-ci/unity-builder`）→ `actions/upload-pages-artifact` → `actions/deploy-pages`
- **必要な Secrets**: `UNITY_LICENSE`、`UNITY_EMAIL`、`UNITY_PASSWORD`
- **キャッシュ**: `Library` フォルダをキャッシュしてビルドを高速化
- **Git LFS**: checkout 時に `lfs: true`。LFS 帯域（無料枠 月1GB）を節約するため LFS キャッシュも使う

## ☁️ インフラ・デプロイ
- **ホスティング**: GitHub Pages（`https://fourdaysroad.github.io/PettanMochiHockey/`）
- **WebGL 圧縮**: Gzip または Brotli + **Decompression Fallback 有効**（GitHub Pages は Content-Encoding ヘッダーを設定できないため）
- **ローディング**: カスタム WebGL テンプレート（タイトルロゴ + 進捗バー）

## 🚫 WebGL の制約（使わないもの）
- スレッド（System.Threading による並列処理）
- System.IO によるファイル操作
- 通常のソケット通信
- Compute Shader に依存する機能・アセット
- ユーザー操作前の音声再生（タイトル画面の最初の入力で音声を有効化）

## 🔒 セキュリティ
- **HTTPS**: GitHub Pages が提供
- **Secrets**: Unity ライセンス情報は GitHub Secrets のみで管理し、リポジトリに含めない
- **アセットライセンス**: WebGL 公開・公開リポジトリへの同梱が許されているかをアセットごとに確認

## 📊 品質基準
- **テスト**: Core 層のロジックは EditMode テストでカバー（TDD）
- **パフォーマンス**: 4人対戦中 60fps 目標、入力遅延3フレーム以内
- **ビルドサイズ**: 圧縮後の初回ダウンロード 50MB 以下（案）
- **初回ロード**: 一般的な回線で15秒以内（案）
- **アクセシビリティ**: プレイヤー識別は色だけでなく番号表示も併用

## 📁 ディレクトリ構造

```
./ (プロジェクトルート)
├── Assets/
│   └── _Project/
│       ├── Scripts/
│       │   ├── Core/          # MochiHockey.Core（UnityEngine 非依存）
│       │   └── Runtime/       # MochiHockey.Runtime
│       ├── Tests/
│       │   ├── EditMode/      # MochiHockey.Tests.EditMode
│       │   └── PlayMode/      # MochiHockey.Tests.PlayMode
│       ├── Prefabs/
│       ├── Scenes/
│       ├── ScriptableObjects/ # GameBalanceConfig など
│       ├── UI/                # UXML / USS / uGUI プレハブ
│       ├── Art/
│       ├── Audio/
│       └── VFX/
├── Packages/
├── ProjectSettings/
├── .github/
│   └── workflows/             # GameCI ワークフロー
├── docs/
│   ├── spec/                  # 企画・仕様書
│   ├── tech-stack.md          # このファイル
│   ├── spec/<要件名>/          # kairo-requirements の出力
│   ├── design/                # 設計書
│   └── tasks/                 # タスク管理
├── .gitignore
├── .gitattributes
└── CLAUDE.md
```

## 🚀 セットアップ手順

### 1. 開発環境準備
1. Unity Hub で Unity 6000.6.8f1（WebGL Build Support モジュール付き）をインストール
2. `git lfs install` を実行してからリポジトリを clone
3. Unity Hub からプロジェクトを開く
4. Package Manager で Test Framework と Cinemachine を追加

### 2. 主要コマンド
```
# EditMode テスト
Unity -batchmode -runTests -testPlatform EditMode -projectPath . -testResults results.xml

# PlayMode テスト
Unity -batchmode -runTests -testPlatform PlayMode -projectPath . -testResults results-playmode.xml
```

## 📝 カスタマイズ方法
1. **技術の追加**: パッケージを追加したらこのファイルにも追記
2. **要件の変更**: 非機能要件（fps・サイズ・ロード時間）の確定値を反映
3. **UI 方針の変更**: 併用が負担になった場合は uGUI に統一することも検討

## 🔄 更新履歴
- 2026-11-17: 初回生成（init-tech-stack により生成、Unity 向けに調整）
