# プロジェクト前提

- ゲーム：Pettan! MOCHI HOCKEY（仕様は docs/spec/pettan-mochi-hockey.md）
- エンジン：6000.6.8f1 / URP / C#
- ビルドターゲット：WebGL（PCブラウザ向け）
- 公開：GitHub Pages ／ CI/CD：GitHub Actions + GameCI
- 対戦形式：ローカル対戦のみ（オンライン対戦は対象外）

# 守ること
- Web用のフレームワーク（React / Vite など）や別のゲームエンジンを提案しない。
- ゲームロジックは MochiHockey.Core（UnityEngine非依存）に置き、EditModeテストを先に書く（TDD）。
- 調整用の数値は GameBalanceConfig（ScriptableObject）に置き、コードに直接書かない。
- WebGLで使えない機能（スレッド、System.IO によるファイル操作、通常のソケット通信、Compute Shader への依存）を使わない。
- シーンやプレハブの YAML を直接編集しない。必要なエディタ上の作業は手順として書き出す。
- テストの実行：Unity -batchmode -runTests -testPlatform EditMode -projectPath . -testResults results.xml
