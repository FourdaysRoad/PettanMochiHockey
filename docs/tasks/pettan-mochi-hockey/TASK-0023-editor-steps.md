# TASK-0023 エディタ作業手順：Match シーンに HUD を置く

CLAUDE.md の決まりで、シーンの YAML は直接書かず、この手順で Unity エディタから作る。
前提：TASK-0020 の手順で `Match` シーンに `MatchManager` があること。

画面（`Assets/_Project/UI/Match/MatchHud.uxml`、`MatchHud.uss`）はできている。シーンに置くだけ。
HUD・ポーズメニュー・リザルトは 1 つの画面にまとめてある。

## 1. HUD を置く

1. `Assets/_Project/Scenes/Match` を開く
2. Hierarchy で右クリック → **Create Empty**、名前を `MatchHud`
3. **Add Component → UI Document**
   - Panel Settings：`Assets/_Project/UI/MochiPanelSettings`
   - Source Asset：`Assets/_Project/UI/Match/MatchHud`（UXML）
4. **Add Component → Hud Presenter**
   - Match：Hierarchy の `MatchManager` をドラッグ
5. **File → Save**（シーン）

## 2. 動作確認（Match シーンを開いて Play）

- [ ] 画面上部中央に「TEAM A 0  3:00  0 TEAM B」が出て、時間が減っていく
- [ ] 始まって 3 秒間は中央に「READY...」
- [ ] ゴールすると「GOAL!」と「+点数  TEAM ○」が 1 秒出て、スコアが増える
- [ ] `Esc`（ゲームパッドは Start）で「PAUSED」のメニューが出る。もう一度 `Esc` か「Resume」で再開
- [ ] ポーズ中に `Backspace`（ゲームパッドは Select）か「Back to title」でタイトルへ戻る
- [ ] 時間切れで「TEAM ○ WINS!」と最終スコア。1 秒後から `Enter` / `A` で「もう一度」、`Esc` / `B` で「タイトルへ」
- [ ] 同点で時間切れになると、時間の代わりに「SUDDEN DEATH」

> 3 分待つのが長いときは、`GameBalanceConfig` の Match → Match Duration を一時的に 20 などにして確かめ、終わったら 180 に戻す。

> マウスでボタンを押しても反応しない場合は、Hierarchy で右クリック → **UI → Event System** を足し、Inspector の「Replace with InputSystemUIInputModule」を押す。キーボード・ゲームパッドでの操作はそのままでも動く。

終わったら Unity を閉じる（バッチモードのテストを動かすため）。
