# TASK-0021 エディタ作業手順：プレイヤーを参加者から生成する

CLAUDE.md の決まりで、シーンとプレハブの YAML は直接書かず、この手順で Unity エディタから作る。
前提：TASK-0014〜0020 の手順で `Match` シーンができていること。

これまで `Match` シーンに直接置いていた `Player1` / `Player2` を、プレハブから生成する形に変える。
対戦設定（Menu）を通らずに `Match` を直接 Play したときは、これまでどおりキーボード 2 人（1P = チーム A、2P = チーム B）で生成される。

## 1. プレイヤーのプレハブを作る

1. `Match` シーンを開く
2. Hierarchy の `Player1` を、Project ウィンドウの `Assets/_Project/Prefabs` にドラッグする
   - 「Original Prefab か Prefab Variant か」を聞かれたら **Original Prefab**
3. できたプレハブの名前を `Player` に変える
4. プレハブ `Player` を選び、Inspector で次を確かめる（Player1 の設定がそのまま入っている）
   - Player Controller：Config = `GameBalanceConfig`（Team は生成時に上書きされるのでどれでもよい）
   - Player Input Reader：Actions = `MochiHockeyControls`（Control Scheme は生成時に上書きされる）
   - Mallet Hitbox、Rigidbody、Capsule Collider（Material = `Ice`）
5. Hierarchy の `Player1` と `Player2` を **削除する**

## 2. プレイヤーを置く地点（各チーム 2 か所）

1. Hierarchy で右クリック → **Create Empty**、名前を `PlayerSpots`、Position `(0, 0, 0)`
2. `PlayerSpots` の子として **Create Empty** を 4 つ作る

| 名前 | Position |
|---|---|
| `SpotA1` | `(-4, 1, 2.5)` |
| `SpotA2` | `(-4, 1, -2.5)` |
| `SpotB1` | `(4, 1, 2.5)` |
| `SpotB2` | `(4, 1, -2.5)` |

> 1vs1 のときは 2 地点の真ん中（`(-4, 1, 0)` と `(4, 1, 0)`）に置かれる。

## 3. スポナー

1. Hierarchy で右クリック → **Create Empty**、名前を `PlayerSpawner`
2. **Add Component → Match Player Spawner**
   - Player Prefab：`Assets/_Project/Prefabs/Player`
   - Team A Spots：Size を `2` にして `SpotA1`、`SpotA2`
   - Team B Spots：Size を `2` にして `SpotB1`、`SpotB2`
3. **File → Save**（シーン）

## 4. Menu シーンに参加受付を置く

1. `Assets/_Project/Scenes/Menu` を開く
2. Hierarchy で右クリック → **Create Empty**、名前を `PlayerJoinManager`
3. **Add Component → Player Join Manager**（Match Scene Name は `Match` のまま）
4. **File → Save**（シーン）

> 参加状況を表示する画面は TASK-0022 で作る。今は表示なしで動作だけ確かめる。

## 5. 動作確認

1. `Match` シーンを開いて Play：これまでどおり 2 人で遊べる（`Player1` = A、`Player2` = B が生成される）
2. `Menu` シーンを開いて Play：
   - `J`（1P）とテンキー `1`（2P）を押してから `Enter` → `Match` に移り、2 人で遊べる
   - ゲームパッドがあれば、A ボタンで参加 → 4 人（キーボード 2 + パッド 2）で `Enter` → 2vs2 で生成される
3. 終わったら Unity を閉じる（バッチモードのテストを動かすため）
