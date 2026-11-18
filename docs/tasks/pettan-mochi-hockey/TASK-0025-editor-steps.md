# TASK-0025 エディタ作業手順：試合のカメラ

CLAUDE.md の決まりで、シーンの YAML は直接書かず、この手順で Unity エディタから作る。

Cinemachine の部品（Brain・Target Group・Cinemachine Camera）は、`CameraRig` が始まったときにコードで組み立てる。
シーンでは Main Camera に部品を 1 つ付けるだけ。

画面サイズ（16:9 でブラウザに合わせる）と WebGL テンプレート、`F` キーのフルスクリーンは設定済みなので、エディタでの作業はない。

## 1. カメラに CameraRig を付ける

1. `Assets/_Project/Scenes/Match` を開く
2. Hierarchy の `Main Camera` を選ぶ
3. **Add Component → Camera Rig**
   - Config：`Assets/_Project/ScriptableObjects/GameBalanceConfig`
4. **File → Save**（シーン）

> Main Camera の Position / Rotation は今のままでよい（始まると CameraRig が角度と距離を決め直す）。

## 2. 動作確認（Match シーンを開いて Play）

- [ ] 斜め上（55°）から見下ろすカメラになり、プレイヤーと餅が全員映っている
- [ ] 2 人がリンクの端と端に離れると、カメラが少し引いて 2 人とも映る
- [ ] 2 人が近づくとカメラが寄るが、寄りすぎない
- [ ] 吹っ飛んで壁に貼り付いたプレイヤーも映っている
- [ ] ゴールで餅が消えている間も、カメラが急に動かない
- [ ] `F` でフルスクリーンになり、もう一度 `F` で戻る（エディタの Game ビューでは変わらないことがある。公開版で確認）

> カメラの角度・寄り具合・追いかける速さは、`GameBalanceConfig` の **Camera** で調整できる（どれも仮の値）。

## 3. 公開版で確認（push して CI のデプロイが終わったあと）

- [ ] ブラウザのウィンドウの大きさを変えると、ゲーム画面が 16:9 のまま広がったり縮んだりする
- [ ] 右下のボタンか `F` で全画面になる

終わったら Unity を閉じる（バッチモードのテストを動かすため）。
