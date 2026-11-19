# Pettan! MOCHI HOCKEY

**もち米を集めて餅をデカく育てろ！ 相手を杵でしばいて壁にペッタンコにして横取りしろ！**

氷のリンクで遊ぶ、2〜4 人のローカル対戦ホッケーゲームです（1 vs 1 / 2 vs 2）。ブラウザ（PC）ですぐ遊べます。

### ▶ 遊ぶ：https://fourdaysroad.github.io/PettanMochiHockey/

- 対応ブラウザ：PC の Chrome / Edge（最新版）。Firefox でも動きます
- 1 台の PC で遊ぶローカル対戦です（オンライン対戦はありません）。3〜4 人で遊ぶにはゲームパッドが必要です

---

## 遊び方

1. **集める・育てる**：餅をドリブルしながら、リンクに落ちている「もち米」に触れると餅が育つ（Lv.1 → Lv.2 → Lv.3 鏡餅）
2. **狙う・運ぶ**：大きい餅ほど得点が高い（1・2・3 点）。ただし持っている人は遅くなり、狙われやすい
3. **殴る・奪う**：杵で相手を叩くと、相手は吹っ飛んで壁にペッタンコに貼り付き、持っていた餅を落とす
4. **決める**：餅を相手の「臼」のゴールに打ち込む

- 試合は 3 分。同点なら、次に点を取ったチームが勝つサドンデス
- 壁から復帰した直後は少しの間無敵（頭の上で星が回ります）
- Lv.3 の鏡餅のシュートは、ゴール前の相手を巻き込んで吹き飛ばす

## 操作

| 操作 | キーボード 1P | キーボード 2P | ゲームパッド |
|---|---|---|---|
| 移動（氷の上を滑る） | W A S D | ↑ ← ↓ → | 左スティック |
| パス／シュート（長押しでチャージ） | J | テンキー 1 | A（下のボタン） |
| 杵スイング | K | テンキー 2 | B（右のボタン） |
| ダッシュ（スタミナを使う） | L | テンキー 3 | RB / R1 |
| 壁キック（壁の近くでダッシュ長押し → 狙って離す） | L 長押し | テンキー 3 長押し | RB / R1 長押し |
| ポーズ | Esc | Esc | Start |

- 餅は、正面から近づくと杵の先にくっついて自動でドリブルします
- **壁キック**：壁の近くでダッシュを長押しすると壁に張り付き、矢印が出ます。スティック（移動キー）で向きを決めて離すと、まっすぐ飛びます。飛んだ先の壁に着いたら、すぐダッシュを押すと次の壁キックへ。続けるほど速くなります（餅を持っている間はできません）
- **F**：フルスクリーンの切り替え（ページ右下のボタンでも可）
- **F3**：fps の表示

## 試合の始め方

1. ページを開き、ロードが終わったら何かキーを押す（ここから音が鳴ります）
2. 参加する人がそれぞれボタンを押す（1P = J、2P = テンキー 1、ゲームパッド = A）
3. 左右に倒してチームを選ぶ（2 人なら 1 vs 1、4 人なら 2 vs 2）
4. Enter（または Start）で試合開始

> ゲームパッドが出てこないときは、ページを開いたあとにゲームパッドのボタンを一度押してください（ブラウザの決まりで、押すまで認識されません）。

---

## 開発

- Unity **6000.6.8f1**（URP / C#）、ビルドは WebGL
- `main` に push すると、GitHub Actions（GameCI）がテスト → WebGL ビルド → GitHub Pages への公開まで行います
- ゲームのロジックは `MochiHockey.Core`（UnityEngine に依存しない）にあり、EditMode テストで確かめています
- 調整用の数値はすべて `Assets/_Project/ScriptableObjects/GameBalanceConfig` にあります（[試遊シート](docs/tasks/pettan-mochi-hockey/TASK-0032-playtest.md)）

テストの実行（Unity を閉じた状態で）：

```bash
Unity -batchmode -runTests -testPlatform EditMode -projectPath . -testResults results.xml
```

```bash
Unity -batchmode -runTests -testPlatform PlayMode -projectPath . -testResults results-playmode.xml
```

ドキュメント：[仕様](docs/spec/pettan-mochi-hockey.md) / [設計](docs/design/pettan-mochi-hockey/) / [タスク](docs/tasks/pettan-mochi-hockey/overview.md)

### 素材について

- 杵・臼・リンク・餅の見た目は、Unity の球・円柱・立方体をコードで組み合わせて作っています
- 効果音と BGM は、すべてコードで合成しています（音のファイルはありません）
- プレイヤーは、本番のモデルが届くまでカプセルの見た目です
- `Assets/_Project/Art/LocalOnly/` は手元だけで試す仮の素材の置き場で、Git には入りません（[手順](docs/tasks/pettan-mochi-hockey/TASK-0027-unitychan-steps.md)）
