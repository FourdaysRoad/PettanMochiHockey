# TASK-0022 エディタ作業手順：Menu シーンに画面を置く

CLAUDE.md の決まりで、シーンの YAML は直接書かず、この手順で Unity エディタから作る。
前提：TASK-0021 の手順で `Menu` シーンに `PlayerJoinManager` があること。

画面（UXML / USS）と PanelSettings はできている。シーンに置くだけ。

- `Assets/_Project/UI/Menu/Menu.uxml`、`Menu.uss`：タイトルと対戦設定の画面
- `Assets/_Project/UI/MochiPanelSettings.asset`：1920×1080 基準で画面の大きさに合わせて拡大縮小する設定

## 1. 画面を置く

1. `Assets/_Project/Scenes/Menu` を開く
2. Hierarchy で右クリック → **Create Empty**、名前を `MenuUI`
3. **Add Component → UI Document**
   - Panel Settings：`Assets/_Project/UI/MochiPanelSettings`
   - Source Asset：`Assets/_Project/UI/Menu/Menu`（UXML）
4. **Add Component → Menu Presenter**
   - Join Manager：Hierarchy の `PlayerJoinManager` をドラッグ
5. **File → Save**（シーン）

## 2. 動作確認（Menu シーンを開いて Play）

- [ ] タイトル「Pettan! MOCHI HOCKEY」が出る
- [ ] どのキー・クリック・ゲームパッドのボタンでも対戦設定へ進む（そのキーでは参加しない）
- [ ] 1P = `J`、2P = テンキー `1`、ゲームパッド = `A` で参加し、枠に番号・デバイス・チームが出る
- [ ] 1P は `A` / `D`、2P は `←` / `→`、ゲームパッドは十字キーかスティックの左右でチームが変わる（枠の色と「TEAM A / TEAM B」の文字の両方が変わる）
- [ ] 抜ける：`K` / テンキー `2` / `B`
- [ ] 下の案内が「2 人か 4 人が必要」→「チームをそろえて」→「ENTER か START で開始」と変わる
- [ ] ゲームパッドの案内（ボタンを一度押すと認識される）が出ている
- [ ] `Enter` で試合が始まり、参加した人数・チームで遊べる

終わったら Unity を閉じる（バッチモードのテストを動かすため）。

> Build Profiles の先頭は `Menu` のままでよい。公開版（GitHub Pages）も、このタイトル画面から始まるようになる。
