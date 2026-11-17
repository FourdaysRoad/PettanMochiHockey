# pettan-mochi-hockey 準備タスク（ユーザー作業）

> **仕様**: [requirements.md](requirements.md)
> **生成日**: 2026-11-17

**【信頼性レベル凡例】**:
- 🔵 **青信号**: 要件定義書・設計文書・ユーザヒアリングで明確に必要と判明したタスク
- 🟡 **黄信号**: 要件定義書・設計文書から妥当に推測されるタスク
- 🔴 **赤信号**: 推測による予防的タスク（実装時に不要と判明する可能性あり）

## 必須（実装開始前に完了が必要）

- [ ] **Unity ライセンスファイル（.ulf）の取得** 🔵 *PRD 9.2*
  - GameCI のドキュメント「Activation」の手順に従い、Unity Personal ライセンスの `.ulf` ファイルを用意する
  - 注意: 自分の PC の `C:\ProgramData\Unity\Unity_lic.ulf` はその PC に結び付いているため、CI ではそのまま使えない（初回の CI で「machine binding doesn't match」となった）。この場合 GameCI は `UNITY_EMAIL` / `UNITY_PASSWORD` によるサインインに切り替えるので、次の項目のアカウント条件を満たすことが重要
  - 関連要件: REQ-061
- [ ] **CI でサインインできる Unity アカウントにする** 🔵 *初回 CI の結果*
  - Unity アカウントにパスワードがあること（Google / Apple で登録した場合は Unity ID のページでパスワードを設定する）
  - 二段階認証が有効だと CI からサインインできない（「Unity rejected the account credentials, or wants a second factor.」になる）。CI 用には二段階認証のないアカウントを使う
  - 関連要件: REQ-061
- [ ] **GitHub Secrets の登録** 🔵 *PRD 9.2*
  - リポジトリの Settings → Secrets and variables → Actions に `UNITY_LICENSE`（.ulf の中身）、`UNITY_EMAIL`、`UNITY_PASSWORD` を登録する（自分で入力すること）
  - `UNITY_LICENSE` には .ulf ファイルの中身（XML）を、最初から最後まで丸ごと貼り付ける
  - `UNITY_EMAIL` / `UNITY_PASSWORD` に余分な空白や改行を入れない（Secrets は中身を表示できないので、迷ったら入力し直す）
  - 登録すると `.github/workflows/ci.yml` がこの 3 つを読む
  - 関連要件: REQ-061
- [ ] **GitHub Pages の公開元を「GitHub Actions」にする** 🔵 *PRD 9.2*
  - Settings → Pages → Build and deployment → Source を「GitHub Actions」にする
  - 関連要件: REQ-061
- [ ] **Unity の WebGL Build Support モジュールを入れる** 🟡 *tech-stack.md*
  - Unity Hub → インストール → 6000.6.8f1 → モジュールを加える → WebGL Build Support
  - 関連要件: REQ-061, REQ-062

## 推奨（実装中に用意できればOK）

- [ ] **パッケージの追加（Test Framework・Cinemachine）** 🔵 *tech-stack.md*
  - Window → Package Manager から追加する
  - 必要になるフェーズ: Phase 1（Test Framework）、Phase 2（Cinemachine）
  - 関連要件: REQ-401, REQ-054
- [ ] **ゲームパッドの用意（動作確認用に 2 台以上）** 🟡 *PRD 3章*
  - 3〜4 人対戦の確認に必要
  - 必要になるフェーズ: Step 2（ルール完成）
  - 関連要件: REQ-041
- [ ] **アセットの選定とライセンス確認** 🔵 *PRD 10章*
  - WebGL 公開・公開リポジトリへの同梱が許されているかをアセットごとに確認する。Compute Shader 依存のアセットは使わない
  - 必要になるフェーズ: Step 3（ポリッシュ）
  - 関連要件: REQ-403

## 確認事項（判断が必要）

- [ ] **リポジトリを公開のままにするか** 🔵 *PRD 9.3*
  - GitHub Free で GitHub Pages を使うにはリポジトリを公開にする必要がある。有料アセットを含める場合はライセンス上の問題がないか確認する
  - 関連要件: REQ-061
- [ ] **「ピヨり（めまい）」アニメーションの使いどころ** 🔴 *PRD 4.2*
  - 例: Recover 直後の演出、または Stuck 中の顔の表現。アセット選定時に決めればよい
  - 関連要件: REQ-031
- [ ] **動作確認用 PC のスペック** 🟡 *PRD 6章*
  - 「中程度の性能の PC」の基準にするマシンを決める
  - 関連要件: 非機能要件（パフォーマンス）

---

## サマリー

| 優先度 | 件数 | 🔵 | 🟡 | 🔴 |
|--------|------|-----|-----|-----|
| 必須 | 4 | 3 | 1 | 0 |
| 推奨 | 3 | 2 | 1 | 0 |
| 確認事項 | 3 | 1 | 1 | 1 |

## 関連文書

- **要件定義書**: [requirements.md](requirements.md)
- **ヒアリング記録**: [interview-record.md](interview-record.md)
