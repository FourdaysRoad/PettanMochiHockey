# TASK-0027 仮のモデルとしてユニティちゃんを手元だけで使う

本番の 3DCG データが用意できるまで、ユニティちゃん（SD 版がおすすめ）を **手元のエディタだけ** で仮の見た目として使う。
リポジトリ・CI のビルド・公開版には入れない。公開版は、これまでどおりカプセルの見た目で動く。

## 仕組み

- `Assets/_Project/Art/LocalOnly/` は Git が無視するフォルダ（`.gitignore` 済み）。仮のモデルは必ずここに置く。
- プレイヤーの見た目は `PlayerVisual` が決める。
  - Player プレハブの Profile が空なら、`Resources/LocalPlayerVisual`（手元の仮の設定）を探す
  - 見つからなければカプセルのまま（CI・公開版はこうなる）
- ゲームの状態は `PlayerAnimatorDriver` が Animator のパラメータに渡す（下の表）。本番のモデルでも同じ名前のパラメータを用意すれば、そのまま動く。

| パラメータ | 型 | 中身 |
|---|---|---|
| `Speed` | Float | 床面の速さ（m/s）。最高 6 くらい、ダッシュ直後は 14 くらい |
| `PettanState` | Int | 0 通常 / 1 ヒットストップ / 2 吹っ飛び中 / 3 壁に貼り付き / 4 無敵（復帰直後） |
| `Swing` | Trigger | 杵を振った瞬間 |
| `Dizzy` | Bool | ピヨり中（復帰直後の無敵中） |
| `Charging` | Bool | シュートをチャージ中 |

---

## ⚠ ライセンス上の注意（必ず守る）

1. **リポジトリに入れない**
   - ユニティちゃんのファイルは `Assets/_Project/Art/LocalOnly/` の中だけに置く。ほかの場所に置いたり移したりしない。
   - Asset Store や unitypackage から読み込むと、`Assets/unity-chan!` など **Assets 直下に入ることが多い**。読み込んだらすぐ、Unity の Project ウィンドウで `LocalOnly` の中へ **ドラッグで移す**（エクスプローラーで移すと参照が切れる）。
   - 一度でもコミット・push すると、あとで消しても Git の履歴に残り、配布したことになる。
2. **コミットの前に毎回確かめる**
   - `git status` を見て、`unity-chan`・`UnityChan`・`SD_unitychan` などの名前が出ていないこと。
   - `Packages/manifest.json` が変わっていたら、コミットする前に Claude に相談する（ユニティちゃん用のシェーダーなどのパッケージが足されている可能性がある）。
   - 迷ったら、コミットを Claude に頼む（毎回中身を確かめてからコミットする）。
3. **手元のビルドを人に配らない・公開しない**
   - 手元でビルドした WebGL などには、ユニティちゃんが入る。人に渡したり Web に置いたりしない。
   - もし公開するなら、ユニティちゃんライセンス（UCL）のロゴの表示など、ライセンスの条件をすべて満たす必要がある。
   - GitHub Pages の公開版は CI がリポジトリからビルドするので、ユニティちゃんは入らない（安心してよい）。
4. **ダウンロードしたライセンスを読む**
   - 同梱のライセンス文書（UCL）と、パッケージのどのファイルが UCL の対象かを確かめる。ユニティちゃん本体以外（武器・衣装・アニメーションなど）は、Asset Store の規約が適用される場合がある。
   - ユニティちゃんのブランド価値を損なう使い方は禁止（性的な表現など）。手元のテストでも守る。
5. **本番のモデルが届いたら**
   - 本番用の `PlayerVisualProfile` をリポジトリ側（`Assets/_Project/Art/` など）に作り、Player プレハブの Profile に指定する。手元の `LocalPlayerVisual` より優先される。
   - 仮のモデルが不要になったら `LocalOnly` フォルダごと消してよい。

---

## 手順

### 1. Player プレハブに PlayerVisual を付ける（リポジトリに入る変更）

1. `Assets/_Project/Prefabs/Player` をダブルクリックしてプレハブを開く
2. ルートの `Player` を選び、**Add Component → Player Visual**
   - Profile：**空のまま**
   - Use Local Profile：**チェックを入れたまま**
3. プレハブを保存して閉じる

> この時点では見た目は変わらない（手元の設定がないのでカプセルのまま）。

### 2. ユニティちゃんを手元に入れる（リポジトリには入らない）

1. ユニティちゃんの公式配布元からダウンロードする（SD 版がおすすめ：デフォルメで、ゲームの雰囲気とカメラの引きに合い、軽い）
2. Unity で読み込む
3. **読み込んだフォルダを `Assets/_Project/Art/LocalOnly/UnityChan/` に Project ウィンドウで移す**
4. `git status` で、ユニティちゃんのファイルが出てこないことを確かめる

### 2.5 UnityChan v1.4.0 を使う場合のメモ（2026-11-18 に中身を確認）

- 読み込むと、`Assets` の直下にも `README_UnityChan_en.txt` / `README_UnityChan_jp.txt` が置かれる。これも `LocalOnly/UnityChan/` に移すこと（2026-11-18 に移動済み）。
- 同梱のライセンスは **UCL 2.02**（`LocalOnly/UnityChan/License/UCL2.02/` の PDF）。
- マテリアルは **Unity Toon Shader**（`com.unity.toonshader` パッケージ）を使う。パッケージが入っていないとピンク色になる。
- **モデルは `Models/unitychan.fbx` を使う**。`Prefabs/for Locomotion/unitychan.prefab` は、古い入力の仕組み（Input Manager）を使う操作スクリプトが付いていて、このプロジェクト（Input System のみ）では Play 中にエラーになるので使わない。
- 身長は約 1.6 m。Local Scale は `1.2` くらいからにすると、カプセル（2 m）の高さに合う。
- 杵を振るようなアニメーションは入っていない。下の表の「仮」を使い、本番では作り直す。

| 状態 | 使えそうなアニメーション（`Animations/`） |
|---|---|
| 待機（Speed 0） | `unitychan_WAIT00` |
| 走り（Speed 6） | `unitychan_RUN00_F` |
| 杵スイング（Swing、仮） | `unitychan_HANDUP00_R`（腕を振り上げる） |
| 吹っ飛び・貼り付き（PettanState 2・3） | `unitychan_DAMAGED00` / `unitychan_DAMAGED01` |
| ピヨり（Dizzy） | `unitychan_LOSE00`（がっかり）か `unitychan_REFLESH00` |

### 3. Animator Controller を作る（LocalOnly の中に）

1. `Assets/_Project/Art/LocalOnly/` で右クリック → **Create → Animator Controller**、名前を `UnityChanPlayer`
2. Animator ウィンドウの **Parameters** に、上の表の 5 つを同じ名前・型で足す
3. 状態（State）を作る（使えるアニメーションは同梱のものから選ぶ）
   - `Locomotion`：待機と走りを `Speed` で切り替える（Blend Tree：0 で待機、6 で走り）
   - `Swing`：`Swing` トリガーで攻撃系のアニメーション → 終わったら `Locomotion` へ
   - `Damage`：`PettanState` が 2 か 3 の間（吹っ飛び・貼り付き）、やられ系のアニメーション
   - `Dizzy`：`Dizzy` が true の間（ふらつく系があれば。なければ `Locomotion` のままでよい）
   - 全部そろわなくてよい。`Locomotion` だけでも動く

### 4. 手元の見た目の設定を作る

1. `Assets/_Project/Art/LocalOnly/Resources/` フォルダを作る
2. そこで右クリック → **Create → Mochi Hockey → Player Visual Profile**、名前を **`LocalPlayerVisual`**（この名前で探すので変えない）
3. Inspector で設定する
   - Model Prefab：SD ユニティちゃんのプレハブ（または FBX）
   - Animator Controller：`UnityChanPlayer`
   - Local Position：`(0, -1, 0)`（足元に合わせる）
   - Local Euler Angles：モデルが後ろを向いていたら `(0, 180, 0)`
   - Local Scale：身長がカプセル（2m）くらいになるよう調整（SD 版なら 1.5〜2 くらいから試す）

### 5. 動作確認（Match シーンで Play）

- [ ] プレイヤーがユニティちゃんになり、走るとアニメーションする
- [ ] 杵を振る（K / テンキー 2 / B）と振るアニメーションになる
- [ ] 吹っ飛び・貼り付きでやられのアニメーションになり、貼り付き中は薄くつぶれる
- [ ] 復帰後の無敵中に点滅し、頭の上で星が回る
- [ ] 足元のリング・番号はこれまでどおり出る
- [ ] **`git status` にユニティちゃんのファイルが出てこない**

終わったら Unity を閉じて、Claude に知らせる（Player プレハブの変更だけをコミットする）。
