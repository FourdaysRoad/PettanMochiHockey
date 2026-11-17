# pettan-mochi-hockey タスク概要

**作成日**: 2026-11-17
**プロジェクト期間**: 2026-11-18 - 2026-12-31（32営業日）
**推定工数**: 256時間
**総タスク数**: 32件

> 個人開発・1日1タスク（8時間、学習時間込み）で数えた目安。開発期間 1〜2ヶ月のうち、約1.5ヶ月分にあたる。

## 関連文書

- **要件定義書**: [📋 requirements.md](../../spec/pettan-mochi-hockey/requirements.md)
- **設計文書**: [📐 architecture.md](../../design/pettan-mochi-hockey/architecture.md)
- **インターフェース定義**: [📝 interfaces.cs](../../design/pettan-mochi-hockey/interfaces.cs)
- **データフロー図**: [🔄 dataflow.md](../../design/pettan-mochi-hockey/dataflow.md)
- **コンテキストノート**: [📝 note.md](../../spec/pettan-mochi-hockey/note.md)
- **準備タスク**: [🔧 prep.md](../../spec/pettan-mochi-hockey/prep.md)

> API 仕様・DB 設計はこのプロジェクトにはない（サーバーなし）。

## フェーズ構成

| フェーズ | 期間 | 成果物 | タスク数 | 工数 | ファイル |
|---------|------|--------|----------|------|----------|
| Phase 1 基盤構築 | 11/18 - 11/23 | asmdef 構成、CI/CD、GameBalanceConfig | 4 | 32h | [TASK-0001~0004](#phase-1-基盤構築) |
| Phase 2 プロトタイプ | 11/24 - 12/10 | Core ロジック一式、カプセル・球・Cube のプロトタイプ | 13 | 104h | [TASK-0005~0017](#phase-2-プロトタイプ) |
| Phase 3 ルール完成 | 12/11 - 12/23 | 試合ルール、画面遷移、HUD、カメラ、参加受付 | 9 | 72h | [TASK-0018~0026](#phase-3-ルール完成) |
| Phase 4 ポリッシュ | 12/24 - 12/31 | モデル・VFX・サウンド・ローディング、最適化 | 6 | 48h | [TASK-0027~0032](#phase-4-ポリッシュ) |

## タスク番号管理

**使用済みタスク番号**: TASK-0001 ~ TASK-0032
**次回開始番号**: TASK-0033

## 全体進捗

- [ ] Phase 1: 基盤構築
- [ ] Phase 2: プロトタイプ
- [ ] Phase 3: ルール完成
- [ ] Phase 4: ポリッシュ

## マイルストーン

- **M1: 基盤構築完了** (2026-11-23): Step 0：空のシーンが GitHub Pages で表示され、CI でテストが回る
- **M2: プロトタイプ完了** (2026-12-10): Step 1：2人がキーボードで遊べ、Core 層のテストが通る
- **M3: ルール完成完了** (2026-12-23): Step 2：1試合を最初から最後まで遊べる（ゲームパッド4人まで）
- **M4: ポリッシュ完了** (2026-12-31): Step 3：演出がそろい、非機能要件（fps・サイズ・ロード時間）を満たす

---

## Phase 1: 基盤構築

**期間**: 11/18 - 11/23
**目標**: Step 0：空のシーンが GitHub Pages で表示され、CI でテストが回る
**成果物**: asmdef 構成、CI/CD、GameBalanceConfig

### タスク一覧

- [x] [TASK-0001: フォルダ構成・asmdef・パッケージ追加](TASK-0001.md) - 8h (DIRECT) 🔵
- [x] [TASK-0002: GameCI ワークフロー（テスト・ビルド・Pages デプロイ）](TASK-0002.md) - 8h (DIRECT) 🔵
- [x] [TASK-0003: WebGL ビルド設定と Pages 公開確認](TASK-0003.md) - 8h (DIRECT) 🔵
- [x] [TASK-0004: BalanceSettings と GameBalanceConfig](TASK-0004.md) - 8h (TDD) 🔵

### 依存関係

```
TASK-0001 → TASK-0002
TASK-0002 → TASK-0003
TASK-0001 → TASK-0004
```

---

## Phase 2: プロトタイプ

**期間**: 11/24 - 12/10
**目標**: Step 1：2人がキーボードで遊べ、Core 層のテストが通る
**成果物**: Core ロジック一式、カプセル・球・Cube のプロトタイプ

### タスク一覧

- [x] [TASK-0005: MochiGrowth（餅のレベル判定）](TASK-0005.md) - 8h (TDD) 🔵
- [x] [TASK-0006: Mochi（保持・吸収・落下・再吸着禁止）](TASK-0006.md) - 8h (TDD) 🔵
- [x] [TASK-0007: SkateMotion と Stamina（慣性移動・ダッシュ）](TASK-0007.md) - 8h (TDD) 🔵
- [x] [TASK-0008: ShotCharge（チャージショット）](TASK-0008.md) - 8h (TDD) 🔵
- [x] [TASK-0009: RinkBounds（リンク外周の壁）](TASK-0009.md) - 8h (TDD) 🟡
- [x] [TASK-0010: PettanStateMachine（状態と時間進行）](TASK-0010.md) - 8h (TDD) 🔵
- [x] [TASK-0011: PettanStateMachine（吹っ飛び軌道・張り付き・ずり落ち）](TASK-0011.md) - 8h (TDD) 🔵
- [x] [TASK-0012: HitRules（杵の当たり判定ルール）](TASK-0012.md) - 8h (TDD) 🔵
- [x] [TASK-0013: RiceSpawnSchedule（もち米の補充）](TASK-0013.md) - 8h (TDD) 🔵
- [ ] [TASK-0014: プロトタイプリンクと PlayerController（移動・ダッシュ）](TASK-0014.md) - 8h (TDD) 🔵
- [ ] [TASK-0015: MochiBall ともち米（ドリブル・シュート・成長）](TASK-0015.md) - 8h (TDD) 🔵
- [ ] [TASK-0016: MalletHitbox と被弾の反映（Pettan）](TASK-0016.md) - 8h (TDD) 🔵
- [ ] [TASK-0017: プロトタイプ統合確認（Step 1 完了）](TASK-0017.md) - 8h (DIRECT) 🔵

### 依存関係

```
TASK-0004 → TASK-0005
TASK-0005 → TASK-0006
TASK-0004 → TASK-0007
TASK-0004 → TASK-0008
TASK-0004 → TASK-0009
TASK-0009 → TASK-0010
TASK-0010 → TASK-0011
TASK-0010 → TASK-0012
TASK-0009 → TASK-0013
TASK-0007 → TASK-0014
TASK-0006 → TASK-0015
TASK-0008 → TASK-0015
TASK-0013 → TASK-0015
TASK-0014 → TASK-0015
TASK-0011 → TASK-0016
TASK-0012 → TASK-0016
TASK-0014 → TASK-0016
TASK-0015 → TASK-0017
TASK-0016 → TASK-0017
```

---

## Phase 3: ルール完成

**期間**: 12/11 - 12/23
**目標**: Step 2：1試合を最初から最後まで遊べる（ゲームパッド4人まで）
**成果物**: 試合ルール、画面遷移、HUD、カメラ、参加受付

### タスク一覧

- [ ] [TASK-0018: MatchRules（スコア・ゴール・タイマー）](TASK-0018.md) - 8h (TDD) 🔵
- [ ] [TASK-0019: MatchRules（サドンデス・フェーズ遷移・再出現）](TASK-0019.md) - 8h (TDD) 🔵
- [ ] [TASK-0020: GoalPost と MatchManager（試合進行）](TASK-0020.md) - 8h (TDD) 🔵
- [ ] [TASK-0021: MatchSession と PlayerJoinManager（参加受付）](TASK-0021.md) - 8h (TDD) 🔵
- [ ] [TASK-0022: Menu シーン UI（タイトル・対戦設定）](TASK-0022.md) - 8h (TDD) 🔵
- [ ] [TASK-0023: Match HUD・ポーズ・リザルト画面](TASK-0023.md) - 8h (TDD) 🔵
- [ ] [TASK-0024: ワールド空間ゲージ（餅の頭上・足元リング）](TASK-0024.md) - 8h (DIRECT) 🔵
- [ ] [TASK-0025: カメラ（Cinemachine）と画面サイズ・フルスクリーン](TASK-0025.md) - 8h (DIRECT) 🔵
- [ ] [TASK-0026: 4 人ゲームパッド対戦の統合確認（Step 2 完了）](TASK-0026.md) - 8h (DIRECT) 🔵

### 依存関係

```
TASK-0005 → TASK-0018
TASK-0018 → TASK-0019
TASK-0015 → TASK-0020
TASK-0016 → TASK-0020
TASK-0019 → TASK-0020
TASK-0014 → TASK-0021
TASK-0021 → TASK-0022
TASK-0020 → TASK-0023
TASK-0022 → TASK-0023
TASK-0020 → TASK-0024
TASK-0020 → TASK-0025
TASK-0023 → TASK-0026
TASK-0024 → TASK-0026
TASK-0025 → TASK-0026
```

---

## Phase 4: ポリッシュ

**期間**: 12/24 - 12/31
**目標**: Step 3：演出がそろい、非機能要件（fps・サイズ・ロード時間）を満たす
**成果物**: モデル・VFX・サウンド・ローディング、最適化

### タスク一覧

- [ ] [TASK-0027: アセット選定・ライセンス確認・モデルとアニメーション適用](TASK-0027.md) - 8h (DIRECT) 🟡
- [ ] [TASK-0028: 演出（ヒットストップ・Pettan!!! VFX・氷しぶき）](TASK-0028.md) - 8h (DIRECT) 🔵
- [ ] [TASK-0029: サウンド（SE・BGM・圧縮設定）](TASK-0029.md) - 8h (DIRECT) 🟡
- [ ] [TASK-0030: カスタム WebGL テンプレート（ロゴ・進捗バー）](TASK-0030.md) - 8h (DIRECT) 🔵
- [ ] [TASK-0031: パフォーマンス・ビルドサイズ・ロード時間の計測と最適化](TASK-0031.md) - 8h (DIRECT) 🟡
- [ ] [TASK-0032: バランス調整とリリース確認](TASK-0032.md) - 8h (DIRECT) 🟡

### 依存関係

```
TASK-0026 → TASK-0027
TASK-0027 → TASK-0028
TASK-0027 → TASK-0029
TASK-0003 → TASK-0030
TASK-0028 → TASK-0031
TASK-0029 → TASK-0031
TASK-0030 → TASK-0031
TASK-0031 → TASK-0032
```

---

## 信頼性レベルサマリー

### 全タスク統計

- **総タスク数**: 32件
- 🔵 **青信号**: 27件 (84%)
- 🟡 **黄信号**: 5件 (16%)
- 🔴 **赤信号**: 0件 (0%)

### フェーズ別信頼性

| フェーズ | 🔵 青 | 🟡 黄 | 🔴 赤 | 合計 |
|---------|-------|-------|-------|------|
| Phase 1 | 4 | 0 | 0 | 4 |
| Phase 2 | 12 | 1 | 0 | 13 |
| Phase 3 | 9 | 0 | 0 | 9 |
| Phase 4 | 2 | 4 | 0 | 6 |

**項目単位（全タスクの実装項目・テスト要件 120項目）**: 🔵 77 (64%) / 🟡 43 (36%) / 🔴 0 (0%)

**品質評価**: 高品質（🟡 は主に調整値・見た目・アセットなど、遊びながら決める項目）

## クリティカルパス

```
TASK-0001 → TASK-0004 → TASK-0009 → TASK-0010 → TASK-0011 → TASK-0016 → TASK-0020 → TASK-0023 → TASK-0026 → TASK-0027 → TASK-0028 → TASK-0031 → TASK-0032
```

**クリティカルパス工数**: 104時間
**並行作業可能工数**: 152時間（個人開発のため実際は順番に進める）

## 次のステップ

タスクを実装するには:
- 全タスク順番に実装: `/tsumiki-legacy:kairo-implement`
- 特定タスクを実装: `/tsumiki-legacy:kairo-implement TASK-0001`
