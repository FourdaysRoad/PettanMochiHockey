# TASK-0020 エディタ作業手順：ゴールと MatchManager

CLAUDE.md の決まりで、シーンの YAML は直接書かず、この手順で Unity エディタから作る。
前提：TASK-0014〜0016 の手順で `Match` シーンにプレイヤー 2 人（Player1 = チーム A、Player2 = チーム B）・餅・スポナーがあること。

チーム A は左側（x < 0）、チーム B は右側（x > 0）にいる。各チームは自分の側のゴールを守る。
今は臼の見た目の代わりに円柱を置く（モデルはポリッシュのフェーズで差し替える）。

## 1. ゴール 2 つ

| 名前 | Position | Scale | Goal Post → Defending Team |
|---|---|---|---|
| `Goal_A` | `(-10.5, 1, 0)` | `(2, 1, 2)` | `A` |
| `Goal_B` | `(10.5, 1, 0)` | `(2, 1, 2)` | `B` |

それぞれ次のように作る。

1. Hierarchy で右クリック → **3D Object → Cylinder**、名前を表のとおりにする
2. Transform を表のとおりにする
3. **Capsule Collider → Is Trigger** にチェック（プレイヤーも餅も中に入れる）
4. **Add Component → Goal Post**、**Defending Team** を表のとおりにする
5. （任意）色の違う Material を入れて、どちらのゴールか分かるようにする

## 2. MatchManager

1. Hierarchy で右クリック → **Create Empty**、名前を `MatchManager`
2. **Add Component → Match Manager**
   - Config：`Assets/_Project/ScriptableObjects/GameBalanceConfig`
   - Ball：Hierarchy の `MochiBall` をドラッグ

> プレイヤーと餅の「キックオフの位置」は、Play を始めたときにシーンに置いてある位置になる。
> 今の位置（Player1 `(-4, 1, 0)`、Player2 `(4, 1, 0)`、餅 `(0, 0.5, 0)`）のままでよい。

## 3. 保存と動作確認

1. **File → Save**（シーン）
2. Play して次を確かめる
   - 始まって 3 秒間はプレイヤーが動けない（キックオフのカウントダウン）
   - 右のゴール（`Goal_B`）に餅を打ち込むと、チーム A に餅のレベル分の点が入り、餅が消え、1 秒後に中央に小さい餅が出る。プレイヤーの位置はそのまま
   - 自分のゴールに入れると相手の得点になる
   - 餅を持ったまま歩いてゴールに入っても得点にならない（打ち込む必要がある）
   - ご飯粒がゴールの中に出ない
   - `Esc`（どちらのプレイヤーでも）でポーズ／再開できる
   - 餅を 6 個育てて（Lv.3）相手に向けて打つと、相手を巻き込んで吹き飛ばし、そのまま進む
   - 3 分たつと試合が終わり、プレイヤーが動けなくなる（スコアの表示は TASK-0023 で作るので、今は見えない）
3. 終わったら Unity を閉じる（バッチモードのテストを動かすため）
