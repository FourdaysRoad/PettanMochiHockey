# TASK-0014 エディタ作業手順：プロトタイプの Match シーン

CLAUDE.md の決まりで、シーンの YAML は直接書かず、この手順で Unity エディタから作る。
寸法は `GameBalanceConfig` のリンクの大きさ（仮：横 24m × 奥行き 14m、原点が中心）に合わせてある。

## 1. シーンを作る

1. Project ウィンドウで `Assets/_Project/Scenes` を右クリック → **Create → Scene → Scene**、名前を `Match` にする
2. `Match` をダブルクリックして開く

## 2. 床

1. Hierarchy で右クリック → **3D Object → Plane**、名前を `Floor`
2. Transform：Position `(0, 0, 0)`、Scale `(2.6, 1, 1.6)`（Plane は 10m 四方なので 26m × 16m になる）

## 3. 外周の壁

1. Hierarchy で右クリック → **Create Empty**、名前を `Walls`、Position `(0, 0, 0)`
2. `Walls` の子として **3D Object → Cube** を 4 つ作り、次のように設定する（内側の面がリンクの端に来る）

| 名前 | Position | Scale |
|---|---|---|
| `Wall_North` | `(0, 0.5, 7.25)` | `(25, 1, 0.5)` |
| `Wall_South` | `(0, 0.5, -7.25)` | `(25, 1, 0.5)` |
| `Wall_East` | `(12.25, 0.5, 0)` | `(0.5, 1, 14)` |
| `Wall_West` | `(-12.25, 0.5, 0)` | `(0.5, 1, 14)` |

## 4. 摩擦のない物理マテリアル（壁に引っかからないようにする）

1. Project ウィンドウで `Assets/_Project/Art` を右クリック → **Create → Physics Material**、名前を `Ice`
2. Inspector：Dynamic Friction `0`、Static Friction `0`、Bounciness `0`、Friction Combine `Minimum`
3. 4 つの壁の **Box Collider → Material** に `Ice` を入れる

## 5. プレイヤー 2 人

`Player1` と `Player2` を同じ手順で作る。

1. Hierarchy で右クリック → **3D Object → Capsule**、名前を `Player1`
2. Transform：Position `(-4, 1, 0)`
3. **Capsule Collider → Material** に `Ice` を入れる
4. **Add Component → Rigidbody**
   - Interpolate：`Interpolate`
   - Collision Detection：`Continuous`
5. **Add Component → Player Controller**
   - Config：`Assets/_Project/ScriptableObjects/GameBalanceConfig`
6. **Add Component → Player Input Reader**
   - Actions：`Assets/_Project/Input/MochiHockeyControls`
   - Control Scheme：`KeyboardP1`

`Player2` は Position `(4, 1, 0)`、Control Scheme `KeyboardP2` にする。それ以外は同じ。

> 見分けやすくしたい場合は、Material を 2 色作って Mesh Renderer に入れる（任意）。

## 6. カメラ

`Main Camera` の Transform：Position `(0, 18, -10)`、Rotation `(60, 0, 0)`。
Game ビューでリンク全体が見えることを確認する。

## 7. ビルド対象に入れる

1. **File → Build Profiles** を開く
2. Scene List に `Assets/_Project/Scenes/Match` をドラッグして追加する（`Menu` の下、1 番）

## 8. 保存と動作確認

1. **File → Save**（シーン）と **File → Save Project**
2. Play して次を確かめる
   - P1：`W A S D` で移動、`L` でダッシュ
   - P2：矢印キーで移動、テンキー `3` でダッシュ
   - 入力をやめても少し滑ってから止まる
   - 壁にぶつかっても引っかからない
3. 終わったら Unity を閉じる（バッチモードのテストを動かすため）
