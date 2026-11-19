# TASK-0028 エディタでの作業：演出（ヒットストップ・Pettan!!!・氷しぶき）

## できること

| 演出 | 中身 | 付ける先 |
|---|---|---|
| ヒットストップ | 杵が当たると、叩かれた側だけでなく **叩いた側も 0.08 秒止まる**（杵の見た目も振り抜く途中で止まる） | なし（コードだけ） |
| Pettan!!! | 叩かれたプレイヤーの頭の上に、赤と黄色のトゲトゲの吹き出し「Pettan!!!」がポンと出て消える。体の位置に白い衝撃波の輪が広がる | Match シーンの **PettanVfx** |
| 氷しぶき | 速く滑っているときに急に向きを変える（70 度以上）と、足元から氷の粒が後ろへ飛ぶ。ダッシュした瞬間にもまとめて飛ぶ | Player プレハブの **Skate Vfx** |

- GOAL! の表示は、これまでどおり HUD に出る（変更なし）。
- 時間・大きさ・量・しきい値は `GameBalanceConfig` の **Vfx**、ヒットストップの長さは **Pettan → Hit Stop Duration**。
- 吹き出しと衝撃波はワールド空間の uGUI、氷しぶきは Particle System。Compute Shader・VFX Graph は使っていない。

---

## 1. 氷しぶきのマテリアルを作る

1. Unity を開く
2. 上のメニュー **Tools → Mochi Hockey → Create VFX Materials**
3. Console に `Created Assets/_Project/VFX/IceSpray.mat.` と出て、`Assets/_Project/VFX/IceSpray` ができたことを確かめる

## 2. Player プレハブに氷しぶきを付ける

1. `Assets/_Project/Prefabs/Player` をダブルクリックして開く
2. ルートの `Player` を選び、**Add Component → Skate Vfx**
3. **Material** に `Assets/_Project/VFX/IceSpray` をドラッグする（ほかの値はそのままでよい）
4. 保存して閉じる

## 3. Match シーンに Pettan!!! の演出を置く

1. `Match` シーンを開く
2. Hierarchy の何もないところで右クリック → **Create Empty**、名前を `PettanVfx`
3. **Add Component → Pettan Vfx**
4. **Config** に `Assets/_Project/ScriptableObjects/GameBalanceConfig` をドラッグする
5. **Ctrl + S** でシーンを保存する

## 4. 動作確認（Match シーンで Play）

- [ ] 杵が当たった瞬間、叩いた側も一瞬止まり、手応えがある
- [ ] 叩かれた側の頭の上に「Pettan!!!」の吹き出しがポンと出て、すぐ消える
- [ ] 白い輪（衝撃波）が広がって消える
- [ ] 吹き出しと輪がカメラの方を向いている（斜めから見てもつぶれない）
- [ ] 2 人が同時に叩き合ったとき、吹き出しが 2 つ出る
- [ ] 走りながら逆の方向へ切り返すと、足元から白い粒が後ろへ飛ぶ
- [ ] まっすぐ走っているとき・止まっているときは出ない
- [ ] ダッシュの瞬間に粒がまとめて飛ぶ
- [ ] 粒がピンク色ではない（ピンクなら手順 2 の Material を確かめる）
- [ ] ポーズ中は吹き出しも粒も止まる

量や大きさが気になったら、Play を止めて `GameBalanceConfig` の Vfx の値を変えて試す。
公開版（ブラウザ）で fps が落ちないかは、push のあと公開版で確かめる。

終わったら Unity を閉じて、Claude に知らせる。
