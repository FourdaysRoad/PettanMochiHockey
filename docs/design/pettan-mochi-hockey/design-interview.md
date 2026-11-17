# pettan-mochi-hockey 設計ヒアリング記録

**作成日**: 2026-11-17
**ヒアリング実施**: step4 既存情報ベースの差分ヒアリング（軽量設計＋C#型定義）

## ヒアリング目的

要件定義書と仕様書 7章（Unity 実装アーキテクチャ案）を確認し、設計方針で決まっていない点を明確化するためにヒアリングを実施しました。

## 質問と回答

### Q1: 設計の作業規模
**質問日時**: 2026-11-17
**カテゴリ**: 技術選択
**背景**: サーバー・DB・API がないため、テンプレートの DB スキーマ・API 仕様は不要。代わりに TDD の出発点となる Core の型定義が有用
**回答**: 軽量設計＋C#型定義
**信頼性への影響**: 出力を architecture.md / dataflow.md / interfaces.cs / design-interview.md とする（database-schema.sql・api-endpoints.md は作らない）

---

### Q2: シーン構成
**質問日時**: 2026-11-17
**カテゴリ**: アーキテクチャ
**背景**: 仕様書に画面遷移はあるが、シーンの分け方が未定義
**回答**: メニューと試合の 2 シーン（Menu / Match）
**信頼性への影響**: architecture.md「シーン構成」と `MatchSession` によるシーン間の引き継ぎを 🔴 → 🔵

---

### Q3: 吹っ飛び（Launched）の動きの作り方
**質問日時**: 2026-11-17
**カテゴリ**: アーキテクチャ
**背景**: 仕様書は「物理挙動が暴れないようステートマシンで制御」とあるが、移動を物理に任せるか Core で計算するかは未定義
**回答**: Core で軌道計算（kinematic で反映）
**信頼性への影響**:
- `PettanStateMachine` が位置・高さ・向き・スケールを出す設計を 🔴 → 🔵
- 壁到達判定のため Core に `RinkBounds`（矩形）を追加（リンク形状を矩形とする点は 🟡）
- 吹っ飛びの挙動が EditMode テストで確認できるようになった

---

## ヒアリング結果サマリー

### 確認できた事項
- DB・API なし。型定義は C# で書く
- シーンは Menu / Match の 2 つ
- 吹っ飛びは Core で計算し、Runtime は kinematic で反映する

### 設計方針の決定事項
- Core は `Tick(deltaTime)` ＋ イベント方式。時間をテストから自由に進められる
- Core に数値の既定値を持たせず、`GameBalanceConfig.ToSettings()` で `BalanceSettings` を渡す
- Core のベクトルは `System.Numerics.Vector2`（XZ 平面）
- UI はメニュー・HUD が UI Toolkit、ワールド空間ゲージが uGUI

### 残課題
- リンクの形状（矩形で足りるか、角を丸めるか）
- `LaunchMaxDuration`、吹っ飛び速度、移動・スタミナ・杵スイングの具体値（GameBalanceConfig で調整）
- HitStop 中に被弾するか（設計では「しない」とした）

### 信頼性レベル分布

**ヒアリング前**:
- 🔵 青信号: 26
- 🟡 黄信号: 12
- 🔴 赤信号: 3

**ヒアリング後**:
- 🔵 青信号: 29 (+3)
- 🟡 黄信号: 12 (±0)
- 🔴 赤信号: 0 (-3)

## 関連文書

- **アーキテクチャ設計**: [architecture.md](architecture.md)
- **データフロー**: [dataflow.md](dataflow.md)
- **型定義（C#）**: [interfaces.cs](interfaces.cs)
- **要件定義**: [requirements.md](../../spec/pettan-mochi-hockey/requirements.md)
