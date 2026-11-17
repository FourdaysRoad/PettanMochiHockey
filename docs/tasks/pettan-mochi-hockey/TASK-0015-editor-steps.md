# TASK-0015 エディタ作業手順：餅とご飯粒を Match シーンに置く

CLAUDE.md の決まりで、シーンとプレハブの YAML は直接書かず、この手順で Unity エディタから作る。
前提：TASK-0014 の手順で `Match` シーンができていること。

## 1. ご飯粒のプレハブ

1. `Match` シーンを開く
2. Hierarchy で右クリック → **3D Object → Sphere**、名前を `MochiRice`
3. Transform：Position `(0, 0.3, 0)`、Scale `(0.3, 0.3, 0.3)`
4. **Sphere Collider → Is Trigger** にチェック
5. **Add Component → Mochi Rice**
6. （任意）白っぽい Material を作って Mesh Renderer に入れる
7. Hierarchy の `MochiRice` を Project ウィンドウの `Assets/_Project/Prefabs` にドラッグしてプレハブにする
8. Hierarchy の `MochiRice` を削除する（シーンにはスポナーが出す）

## 2. 餅

1. Hierarchy で右クリック → **3D Object → Sphere**、名前を `MochiBall`
2. Transform：Position `(0, 0.5, 0)`、Scale `(1, 1, 1)`
3. **Sphere Collider → Material** に `Assets/_Project/Art/Ice` を入れる
4. **Add Component → Rigidbody**
   - Interpolate：`Interpolate`
   - Collision Detection：`Continuous Dynamic`
   - Linear Damping は触らなくてよい（`GameBalanceConfig` の値で上書きされる）
5. **Add Component → Mochi Ball**
   - Config：`Assets/_Project/ScriptableObjects/GameBalanceConfig`
6. （任意）白い Material を作って Mesh Renderer に入れる

## 3. ご飯粒のスポナー

1. Hierarchy で右クリック → **Create Empty**、名前を `RiceSpawner`
2. Transform：Position `(0, 0.3, 0)`（ご飯粒はこの高さに出る）
3. **Add Component → Mochi Rice Spawner**
   - Config：`Assets/_Project/ScriptableObjects/GameBalanceConfig`
   - Rice Prefab：`Assets/_Project/Prefabs/MochiRice`

## 4. 保存と動作確認

1. **File → Save**（シーン）
2. Play して次を確かめる
   - ご飯粒が 5 個出て、1.5 秒ごとに 10 個まで増える
   - 餅に正面から近づくと体の前にくっついて一緒に動く（後ろからは付かない）
   - P1 は `J`、P2 はテンキー `1` で打ち出す。長押しするほど速い（最大 1 秒）
   - 打った直後は、打った本人はすぐには拾えない（相手はすぐ拾える）
   - 持ったままご飯粒に触れると吸収され、3 個で 1.5 倍、6 個で 2.5 倍になり、持ち手が遅くなる
   - 持っていない餅がご飯粒に触れても何も起きない
3. 終わったら Unity を閉じる（バッチモードのテストを動かすため）
