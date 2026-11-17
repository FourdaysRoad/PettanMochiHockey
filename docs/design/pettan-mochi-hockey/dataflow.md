# pettan-mochi-hockey データフロー図

**作成日**: 2026-11-17
**関連アーキテクチャ**: [architecture.md](architecture.md)
**関連要件定義**: [requirements.md](../../spec/pettan-mochi-hockey/requirements.md)

**【信頼性レベル凡例】**:
- 🔵 **青信号**: EARS要件定義書・設計文書・ユーザヒアリングを参考にした確実なフロー
- 🟡 **黄信号**: EARS要件定義書・設計文書・ユーザヒアリングから妥当な推測によるフロー
- 🔴 **赤信号**: EARS要件定義書・設計文書・ユーザヒアリングにない推測によるフロー

---

## システム全体のデータフロー 🔵

**信頼性**: 🔵 *architecture.md・PRD 7.1*

```mermaid
flowchart LR
    Dev[ゲームパッド / キーボード] --> PI[PlayerInput]
    PI --> PC[PlayerController]
    Cfg[GameBalanceConfig] -->|ToSettings| Settings[BalanceSettings]
    Settings --> Core
    subgraph Core[MochiHockey.Core]
        SM[SkateMotion / Stamina / ShotCharge]
        PSM[PettanStateMachine]
        Mochi[Mochi / MochiGrowth]
        MR[MatchRules]
        RS[RiceSpawnSchedule]
    end
    PC -->|Tick 入力| SM
    SM -->|目標速度| PC
    PC -->|被弾時 Begin / Tick| PSM
    PSM -->|位置・向き・スケール| PC
    MB[MochiBall] <-->|もち米吸収・保持者| Mochi
    GP[GoalPost] -->|ゴール通知| MM[MatchManager]
    MM -->|RegisterGoal / Tick| MR
    MR -->|スコア・残り時間・フェーズ| HUD[HUD / Menu]
    RSp[MochiRiceSpawner] -->|Tick| RS
    PC --> RB[Rigidbody / Transform]
    MB --> RB
```

## フレーム内の処理順 🟡

**信頼性**: 🟡 *入力遅延要件（3フレーム以内）から妥当な推測*

1. `Update`: `PlayerInput` から入力を読む（移動・ボタン押下／長押し）
2. `Update`: `MatchManager` が `MatchRules.Tick(dt)` を呼ぶ（ポーズ中は dt=0）
3. `Update`: 各 `PlayerController` が `SkateMotion` / `Stamina` / `ShotCharge` / `PettanStateMachine` を `Tick(dt)`
4. `FixedUpdate`: 通常時は Rigidbody に目標速度を反映、被弾中は kinematic で Core の位置を `MovePosition`
5. `OnTrigger*` / `OnCollision*`: 杵ヒット、餅の吸着、もち米吸収、ゴール侵入を Core に通知
6. `LateUpdate`: HUD・ワールドゲージ・カメラを更新

- Script Execution Order: `MatchManager` → `PlayerController` → `MochiBall` → Presenter 類 🟡

## 主要機能のデータフロー

### 機能1: 杵スイング → Pettan → 復帰 🔵

**信頼性**: 🔵 *REQ-025, 031〜036・ヒアリング*

```mermaid
sequenceDiagram
    participant A as 攻撃者 PlayerController
    participant H as MalletHitbox
    participant R as HitRules(Core)
    participant V as 被弾者 PlayerController
    participant P as PettanStateMachine(Core)
    participant M as MochiBall / Mochi

    A->>H: スイング開始（判定を有効化）
    H->>R: CanHit(攻撃者チーム, 被弾者チーム, 被弾者のPettan状態)
    alt 当たる
        R-->>H: true
        H->>V: OnMalletHit(攻撃者の正面方向)
        V->>P: Begin(現在位置, 方向)
        P-->>V: HitStop 開始イベント
        V->>M: 保持していれば Drop()（サイズ維持で軽く跳ねる）
        loop 毎フレーム
            V->>P: Tick(dt)
            P-->>V: 位置・高さ・向き・スケール・状態
        end
        Note over P: 0.08s Launched → 壁到達で Stuck<br/>1.5s SlideDown → 2.6s Recover（ポンッ！SE）→ 3.6s 通常
    else 当たらない（味方・張り付き中・無敵中）
        R-->>H: false
    end
    alt 誰にも当たらなかった
        A->>A: 空振り硬直
    end
```

**詳細ステップ**:
1. 同じフレームで 2 人が互いに当てた場合は、両方の `Begin` を呼ぶ（`HitRules` はスイング開始時点の状態で判定する） 🔵 *REQ-036*
2. Lv.3 シュートの巻き込みも、同じ `Begin` を呼ぶ（方向はシュート方向） 🔵 *REQ-015*
3. 吹っ飛びは水平に直進し、壁で `LaunchWallBounces` 回（既定 1 回）跳ね返ってから次の壁に貼り付く。被弾から `LaunchMaxDuration` を超えても貼り付けなければその場で Recover 🔵 *REQ-033・ユーザー指示（2026-11-18）*

### 機能2: ドリブル → もち米吸収 → 成長 🔵

**信頼性**: 🔵 *REQ-012〜014, 022, 024*

```mermaid
sequenceDiagram
    participant PC as PlayerController
    participant MB as MochiBall
    participant MO as Mochi(Core)
    participant RS as MochiRiceSpawner

    PC->>MB: 正面で接触
    MB->>MO: TryAttach(プレイヤーID)
    MO-->>MB: 再吸着禁止中でなければ成功
    MB->>RS: もち米と接触（保持中のみ）
    RS->>MO: AddRice()
    MO-->>MB: レベル変化イベント（Lv.1→2 など）
    MB->>MB: スケールとコライダーを更新
    MB->>PC: 速度倍率を通知（100% / 90% / 75%）
```

### 機能3: シュート → ゴール → 再出現 🔵

**信頼性**: 🔵 *REQ-003, 004, 023, 024・ヒアリング*

```mermaid
sequenceDiagram
    participant PC as PlayerController
    participant SC as ShotCharge(Core)
    participant MB as MochiBall
    participant G as GoalPost
    participant MM as MatchManager
    participant MR as MatchRules(Core)

    PC->>SC: 長押し中 Tick(dt)
    PC->>SC: 離す → 段階（0〜2）を取得
    PC->>MB: Shoot(方向, 段階)
    MB->>MB: 再吸着禁止 0.3 秒を開始
    MB->>G: ゴールに侵入
    G->>MM: OnGoal(ゴールを守るチーム, 餅のレベル)
    MM->>MR: RegisterGoal(守るチーム, レベル)
    MR-->>MM: 相手チームに 1〜3 点、サドンデス中なら試合終了
    MM->>MB: 餅を消す
    MM->>MR: Tick で 1.0 秒待ち
    MR-->>MM: 再出現イベント
    MM->>MB: 中央に Lv.1 の小餅を出す（プレイヤー位置は戻さない）
```

### 機能4: 参加受付 → 試合開始 🔵

**信頼性**: 🔵 *REQ-041〜043, 051・ヒアリング（2 シーン構成）*

```mermaid
sequenceDiagram
    participant U as プレイヤー
    participant J as PlayerJoinManager
    participant S as MatchSession
    participant MM as MatchManager

    U->>J: Menu シーンでボタンを押す
    J->>S: 参加者追加（デバイス、番号 1P〜4P、チーム）
    U->>J: 全員揃ったら開始
    J->>MM: Match シーンをロード
    MM->>S: 参加者一覧を取得
    MM->>MM: PlayerInput を生成してデバイスをペアリング、自陣の初期位置に配置
    MM->>MM: キックオフ（中央に Lv.1 の小餅）
```

## 状態管理フロー

### 試合フェーズ（MatchRules） 🔵

**信頼性**: 🔵 *REQ-001〜005*

```mermaid
stateDiagram-v2
    [*] --> Kickoff
    Kickoff --> Playing: 開始カウント終了
    Playing --> MochiRespawning: ゴール
    MochiRespawning --> Playing: 1.0 秒経過
    Playing --> Finished: 時間切れ（得点差あり）
    Playing --> SuddenDeath: 時間切れ（同点）
    MochiRespawning --> SuddenDeath: 待ち中に時間切れ（同点）
    MochiRespawning --> Finished: 待ち中に時間切れ（得点差あり）
    SuddenDeath --> Finished: どちらかが得点
    Finished --> [*]
```

- サドンデス開始時は、餅が場になければ中央に出し直す 🟡 *妥当な推測*
- ポーズは `MatchRules` の外（Runtime）で dt=0 として扱う 🟡

### 被弾ステート（PettanStateMachine） 🔵

**信頼性**: 🔵 *REQ-031〜035*

```mermaid
stateDiagram-v2
    [*] --> Normal
    Normal --> HitStop: Begin
    HitStop --> Launched: 0.08s
    Launched --> Stuck: 壁到達
    Launched --> Invincible: LaunchMaxDuration 超過（Recover）
    Stuck --> Stuck: 1.5s〜 SlideDown
    Stuck --> Invincible: 2.6s（Recover イベント）
    Invincible --> Normal: 3.6s
```

| 状態 | 操作 | 被弾 | コライダー | スケール |
|---|---|---|---|---|
| Normal | 可 | する | 有効 | 1.0 |
| HitStop | 不可 | しない | 有効 | 1.0 |
| Launched | 不可 | しない | 他プレイヤーとは衝突しない | 1.0 |
| Stuck | 不可 | しない | 無効 | (1.3, 1.3, 0.05) |
| Invincible | 可 | しない | 有効 | 1.0（点滅） |

- HitStop 中の被弾可否は要件に明記がないため「しない」とする 🟡

## エラーハンドリング 🟡

**信頼性**: 🟡 *WebGL 環境から妥当な推測*

- **ゲームパッドの切断**: そのプレイヤーの入力をゼロ扱いにし、試合を自動でポーズ。再接続で再開
- **`GameBalanceConfig` 未設定**: Runtime 起動時に `Debug.LogError` を出して `MatchManager` を無効にする（Core に既定値は持たせない。数値は GameBalanceConfig だけに置くため）
- **Core の不正値（負の dt、負のもち米数など）**: `ArgumentOutOfRangeException` を投げ、EditMode テストで検出する

## 関連文書

- **アーキテクチャ**: [architecture.md](architecture.md)
- **型定義（C#）**: [interfaces.cs](interfaces.cs)
- **要件定義**: [requirements.md](../../spec/pettan-mochi-hockey/requirements.md)

## 信頼性レベルサマリー

- 🔵 青信号: 12件 (63%)
- 🟡 黄信号: 7件 (37%)
- 🔴 赤信号: 0件 (0%)

**品質評価**: 高品質（🟡 は実装時に確認すればよい細部）
