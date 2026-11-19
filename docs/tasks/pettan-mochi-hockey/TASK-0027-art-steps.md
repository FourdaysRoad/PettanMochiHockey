# TASK-0027 仮の見た目（杵・臼・リンク・餅）の付け方

[asset-decisions.md](../../design/pettan-mochi-hockey/asset-decisions.md) の方針どおり、杵・臼・リンク・餅は CC0 で合うものがないので、球・円柱・立方体を組み合わせて作る。
形はコード（実行時に組み立てる）、色はマテリアルを増やさずに付けるので、作るアセットはない。どれもリポジトリに入れてよい。

| 部品 | 付ける先 | 中身 |
|---|---|---|
| **Mallet Visual** | Player プレハブ | 木の色の杵（柄＋頭）を体の右前に構え、杵スイングで右から左へ水平に振り抜く |
| **Usu Visual** | Goal_A / Goal_B | 木の色の臼（胴＋帯＋上面のくぼみ）。元の円柱は隠れ、ゴールの判定はそのまま |
| **Color Tint** | 床・壁・餅・ご飯粒 | 色とつやだけを変える |

振りの角度・戻る時間は `GameBalanceConfig` の **Mallet**（Rest Yaw / Swing Arc Angle / Swing Return Time）。振り抜く時間は **Player → Swing Active Time**。

---

## 1. Player プレハブに杵を付ける

1. `Assets/_Project/Prefabs/Player` をダブルクリックして開く
2. ルートの `Player` を選び、**Add Component → Mallet Visual**（値はそのままでよい）
3. 保存して閉じる

## 2. ゴールを臼にする（Match シーン）

1. `Match` シーンを開く
2. Hierarchy で `Goal_A` と `Goal_B` を両方選ぶ（Ctrl を押しながらクリック）
3. **Add Component → Usu Visual**（値はそのままでよい）

> Play 中だけ臼になる（編集中のシーンでは、これまでの円柱のまま見える）。

## 3. リンクと餅に色を付ける（Match シーン）

それぞれ選んで **Add Component → Color Tint** を付け、Color と Smoothness を入れる。
Color は色の欄をクリックして、Hexadecimal に下の値を入れる。

| 対象 | Color（Hex） | Smoothness | 見た目 |
|---|---|---|---|
| `Floor` | `E6F4FF` | `0.85` | 青白くつやのある氷 |
| `Walls` の下の `Wall_North` / `Wall_South` / `Wall_East` / `Wall_West`（4 つまとめて選んでよい） | `5B9BE0` | `0.4` | 青い壁 |
| `MochiBall` | `FFFBF2` | `0.35` | 少しクリーム色の白い餅 |

ご飯粒はプレハブに付ける。

1. `Assets/_Project/Prefabs/MochiRice` を開く
2. ルートの `MochiRice` を選び、**Add Component → Color Tint**：Color `FFFFFF`、Smoothness `0.6`
3. 保存して閉じる

最後に **Ctrl + S** でシーンを保存する。

## 4. 動作確認（Match シーンで Play）

- [ ] プレイヤーが杵を右前に構えている
- [ ] 杵を振る（K / テンキー 2 / B）と、右から左へ水平に振り抜いて構えに戻る
- [ ] 杵が当たれば今までどおり相手が飛ぶ（見た目の杵には当たり判定がない）
- [ ] 壁に貼り付いている間、杵も一緒に薄くつぶれる（破綻しない）
- [ ] 無敵中の点滅で、杵も一緒に点滅する
- [ ] ゴールが木の臼になり、餅を打ち込むと今までどおり得点になる
- [ ] 床が青白い氷、壁が青、餅とご飯粒が白に見える
- [ ] ユニティちゃんを使っている場合も、杵が体から大きく外れていない

色や形が気になったら、Play を止めてから Inspector の値を変えて試す（形は Mallet Visual / Usu Visual の「形」の欄）。
終わったら Unity を閉じて、Claude に知らせる。
