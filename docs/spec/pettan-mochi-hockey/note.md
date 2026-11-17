# pettan-mochi-hockey コンテキストノート

**作成日**: 2026-11-17

## 技術スタック
- Unity 6000.6.8f1 / URP 17.6 / C# / WebGL 2.0（詳細: [docs/tech-stack.md](../../tech-stack.md)）
- Input System 1.20、Cinemachine 3.x（要追加）、Unity Test Framework（要追加）
- UI: メニューは UI Toolkit、試合中のワールド空間ゲージは uGUI
- 公開: GitHub Pages（`https://fourdaysroad.github.io/PettanMochiHockey/`）
- CI/CD: GitHub Actions + GameCI（unity-test-runner / unity-builder）
- Git + Git LFS（`.gitignore` / `.gitattributes` 設定済み、初回 push 済み）

## 開発ルール（CLAUDE.md より）
- Web 用フレームワークや別エンジンを提案しない
- ゲームロジックは `MochiHockey.Core`（UnityEngine 非依存）に置き、EditMode テストを先に書く（TDD）
- 調整用の数値は `GameBalanceConfig`（ScriptableObject）に置く
- WebGL で使えない機能（スレッド、System.IO、ソケット、Compute Shader）を使わない
- シーン・プレハブの YAML を直接編集しない。エディタ作業は手順として書き出す
- テスト実行: `Unity -batchmode -runTests -testPlatform EditMode -projectPath . -testResults results.xml`

## 開発体制
- 個人開発、期間 1〜2ヶ月、Unity / C# は学習しながら

## 関連実装（現状）
- `Assets/` には URP テンプレートのサンプル（SampleScene、TutorialInfo、Settings、InputSystem_Actions）のみ
- ゲームコード・asmdef・テストは未作成

## 設計文書
- 企画・仕様書: [docs/spec/pettan-mochi-hockey.md](../pettan-mochi-hockey.md)
- 設計書・タスクは未作成

## 注意事項
- GitHub Pages は Content-Encoding ヘッダーを設定できない → WebGL ビルドは Decompression Fallback を有効にする
- ブラウザはユーザー操作前に音を鳴らせない／ゲームパッドはボタンを一度押すまで認識されない
- LFS の無料枠（月1GB帯域）に注意。CI で LFS キャッシュを使う
