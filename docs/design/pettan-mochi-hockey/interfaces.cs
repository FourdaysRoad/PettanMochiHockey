// ============================================================================
// pettan-mochi-hockey Core 層 型定義（設計資料）
//
// 作成日: 2026-11-17
// 関連設計: architecture.md / dataflow.md
//
// ※ このファイルは設計資料であり、Unity にはコンパイルされない（docs/ 配下）。
//   実装時は Assets/_Project/Scripts/Core/ 以下にクラスごとのファイルとして作る。
//   メソッド本体は省略し、公開 API（シグネチャ）だけを示す。
//
// 方針:
// - MochiHockey.Core は noEngineReferences = true。UnityEngine を参照しない。
// - 時間はすべて Tick(float deltaTime) で進める（秒）。
// - 調整値は BalanceSettings で受け取る。Core に数値の既定値は持たせない
//   （実際の値は GameBalanceConfig に置き、テストではテスト側で値を組み立てる）。
// - ベクトルは System.Numerics.Vector2（リンクの床面 XZ 平面。X = x, Y = z）。
//
// 信頼性レベル:
// - 🔵 青信号: 要件定義書・設計文書・ユーザヒアリングを参考にした確実な型定義
// - 🟡 黄信号: 要件定義書・設計文書から妥当な推測による型定義
// - 🔴 赤信号: 要件定義書・設計文書にない推測による型定義
// ============================================================================

using System;
using System.Numerics;

namespace MochiHockey.Core
{
    // ========================================================================
    // 共通
    // ========================================================================

    /// <summary>チーム。🔵 REQ-006（1vs1 / 2vs2）</summary>
    public enum Team { A, B }

    /// <summary>乱数（テストで差し替えるため注入する）。🟡 REQ-011 のランダム出現から推測</summary>
    public interface IRandom
    {
        /// <summary>[0, 1) の乱数</summary>
        float NextFloat();
    }

    // ========================================================================
    // 設定（GameBalanceConfig から ToSettings() で作る）🔵 REQ-402
    // ========================================================================

    public sealed class BalanceSettings
    {
        public MochiSettings Mochi;         // 🔵
        public PettanSettings Pettan;       // 🔵
        public MatchSettings Match;         // 🔵
        public RiceSettings Rice;           // 🔵
        public PlayerSettings Player;       // 🔵
        public RinkBounds Rink;             // 🟡 リンク寸法もシーンに依存しない値として持つ
    }

    /// <summary>餅の成長。🔵 REQ-013（PRD 4.1 の表）</summary>
    public sealed class MochiSettings
    {
        public int Lv2Threshold;            // 3
        public int Lv3Threshold;            // 6
        public float[] ScaleByLevel;        // [1.0, 1.5, 2.5]
        public int[] PointsByLevel;         // [1, 2, 3]
        public float[] SpeedMultiplierByLevel; // [1.00, 0.90, 0.75]
        public float RegrabCooldown;        // 0.3 🔵 REQ-024
    }

    /// <summary>被弾ステート。🔵 REQ-031〜034</summary>
    public sealed class PettanSettings
    {
        public float HitStopDuration;       // 0.08
        public float LaunchMaxDuration;     // 🟡 被弾からの上限（これまでに貼り付けなければその場で復帰）
        public float LaunchSpeed;           // 🟡 直進速度（m/s）
        public int LaunchWallBounces;       // 🔵 壁で跳ね返る回数（1）。その後、次の壁に貼り付く
        public float SlideDownStartTime;    // 1.5
        public float RecoverTime;           // 2.6
        public float InvincibleEndTime;     // 3.6
        public float SlideDownSpeed;        // 🟡 壁沿いにずり落ちる速度
        public Vector3 StuckScale;          // (1.3, 1.3, 0.05)
    }

    /// <summary>試合ルール。🔵 REQ-001〜005</summary>
    public sealed class MatchSettings
    {
        public float MatchDuration;         // 180
        public float KickoffCountdown;      // 🟡 キックオフ前のカウント
        public float MochiRespawnDelay;     // 1.0
    }

    /// <summary>もち米。🔵 REQ-011</summary>
    public sealed class RiceSettings
    {
        public int MinCount;                // 5
        public int MaxCount;                // 10
        public float RefillInterval;        // 1.5
    }

    /// <summary>プレイヤー操作。🔵 REQ-021〜026（数値は 🟡 調整で決める）</summary>
    public sealed class PlayerSettings
    {
        public float MaxSpeed;              // 🟡
        public float Acceleration;          // 🟡
        public float Friction;              // 🟡
        public float StaminaMax;            // 🟡
        public float DashCost;              // 🟡
        public float StaminaRegenPerSecond; // 🟡
        public float DashImpulse;           // 🟡
        public float ChargeMaxTime;         // 1.0 🔵
        public int ChargeStages;            // 2 🔵
        public float SwingActiveTime;       // 🟡 杵の判定持続
        public float WhiffRecoveryTime;     // 🟡 空振り硬直
    }

    // ========================================================================
    // 餅 🔵 REQ-012〜015, 022, 024
    // ========================================================================

    public enum MochiLevel { Lv1 = 1, Lv2 = 2, Lv3 = 3 }

    /// <summary>もち米数 → レベル・得点・倍率。状態を持たない。🔵 REQ-013</summary>
    public sealed class MochiGrowth
    {
        public MochiGrowth(MochiSettings settings) { }
        public MochiLevel GetLevel(int riceCount) => default;      // 負数は ArgumentOutOfRangeException
        public int GetPoints(MochiLevel level) => default;
        public float GetScale(MochiLevel level) => default;
        public float GetSpeedMultiplier(MochiLevel level) => default;
        /// <summary>次のレベルまでの進捗 0〜1（Lv.3 は 1）。🔵 REQ-053 の成長ゲージ</summary>
        public float GetGrowthProgress(int riceCount) => default;
    }

    /// <summary>餅 1 個の状態。🔵</summary>
    public sealed class Mochi
    {
        public Mochi(MochiGrowth growth, MochiSettings settings) { }

        public int RiceCount { get; }                 // 🔵 落ちても維持（REQ-014）
        public MochiLevel Level { get; }
        public int? HolderId { get; }                 // null = 誰も持っていない
        public event Action<MochiLevel> LevelChanged; // 🔵 見た目・コライダー更新用

        /// <summary>吸着を試みる。再吸着禁止中の本人は失敗。🔵 REQ-022, 024</summary>
        public bool TryAttach(int playerId) => default;
        /// <summary>保持中のときだけ吸収する。🔵 REQ-012</summary>
        public bool TryAbsorbRice() => default;
        /// <summary>被弾で落とす（再吸着禁止は付かない）。🔵 REQ-032</summary>
        public void Drop() { }
        /// <summary>パス・シュートで手放す（本人に再吸着禁止が付く）。🔵 REQ-024</summary>
        public void Release() { }
        public void Tick(float deltaTime) { }
    }

    // ========================================================================
    // Pettan（被弾） 🔵 REQ-031〜036
    // ========================================================================

    public enum PettanState { Normal, HitStop, Launched, Stuck, Invincible }

    /// <summary>リンク外周の壁（軸並行の矩形）。🟡 リンク形状は矩形と仮定</summary>
    public readonly struct RinkBounds
    {
        public RinkBounds(Vector2 min, Vector2 max) { Min = min; Max = max; }
        public Vector2 Min { get; }
        public Vector2 Max { get; }
        /// <summary>origin から direction へ直進したとき最初に当たる壁。🔵 REQ-033</summary>
        public bool TryGetWallHit(Vector2 origin, Vector2 direction, out Vector2 hitPoint, out Vector2 wallNormal)
        { hitPoint = default; wallNormal = default; return default; }
        public bool Contains(Vector2 point) => default;
    }

    /// <summary>被弾後の状態と軌道。Runtime はこの値を kinematic で反映する。🔵 ヒアリング（Core で軌道計算）</summary>
    public sealed class PettanStateMachine
    {
        public PettanStateMachine(PettanSettings settings, RinkBounds rink) { }

        public PettanState State { get; }
        public float Elapsed { get; }                 // Begin からの経過秒
        public Vector2 Position { get; }              // 床面 XZ
        public float Height { get; }                  // Y（吹っ飛び中は 0、ずり落ちで下がる）
        public Vector2 WallNormal { get; }            // Stuck 中のみ有効
        public bool IsSlidingDown { get; }            // 🔵 1.5s〜2.6s
        public Vector3 Scale { get; }                 // 🔵 Stuck 中は StuckScale

        public bool CanControl { get; }               // Normal / Invincible
        public bool CanBeHit { get; }                 // Normal のみ 🟡 HitStop 中も不可とする
        public bool CollidesWithPlayers { get; }      // Launched 中は false 🔵 REQ-035
        public bool ColliderEnabled { get; }          // Stuck 中は false 🔵 REQ-035

        public event Action<PettanState> StateChanged;
        public event Action Recovered;                // 🔵 「ポンッ！」SE
        public event Action Bounced;                  // 🔵 壁で跳ね返った瞬間（SE・VFX 用）

        /// <summary>被弾開始。Normal 以外で呼ばれたら無視する。🔵</summary>
        public void Begin(Vector2 position, Vector2 launchDirection) { }
        public void Tick(float deltaTime) { }
    }

    /// <summary>杵が当たるかの判定。🔵 REQ-006, 035, 036</summary>
    public static class HitRules
    {
        /// <summary>味方・自分・被弾不可状態には当たらない。</summary>
        public static bool CanHit(int attackerId, Team attackerTeam, int targetId, Team targetTeam, bool targetCanBeHit) => default;
    }

    // ========================================================================
    // 試合 🔵 REQ-001〜005
    // ========================================================================

    public enum MatchPhase { Kickoff, Playing, MochiRespawning, SuddenDeath, Finished }

    public sealed class MatchRules
    {
        public MatchRules(MatchSettings settings, MochiGrowth growth) { }

        public MatchPhase Phase { get; }
        public float RemainingTime { get; }           // 0 以上。サドンデス中は 0
        public int GetScore(Team team) => default;
        public Team? Winner { get; }                  // Finished のときのみ

        public event Action<MatchPhase> PhaseChanged;
        public event Action<Team, int> Scored;        // 得点したチーム, 加算点
        public event Action MochiRespawnRequested;    // 🔵 中央に Lv.1（プレイヤー位置は戻さない）

        public void StartKickoff() { }
        /// <summary>ゴールに入った。得点は守る側の相手に入る（オウンゴールも同じ）。🔵 REQ-003</summary>
        public void RegisterGoal(Team defendingTeam, MochiLevel level) { }
        public void Tick(float deltaTime) { }
    }

    // ========================================================================
    // もち米 🔵 REQ-011
    // ========================================================================

    public sealed class RiceSpawnSchedule
    {
        public RiceSpawnSchedule(RiceSettings settings, RinkBounds spawnArea, IRandom random) { }
        /// <summary>今フレームで出すべき数を返す（0 以上）。最低数を下回っていれば即時に補充する 🟡</summary>
        public int Tick(float deltaTime, int currentCount) => default;
        /// <summary>出現位置。除外領域（ゴール内など）は呼び出し側が渡す。🔵</summary>
        public Vector2 PickPosition(Func<Vector2, bool> isExcluded) => default;
    }

    // ========================================================================
    // プレイヤー操作 🔵 REQ-021, 023, 026
    // ========================================================================

    /// <summary>慣性のあるスケート移動。🔵 REQ-021</summary>
    public sealed class SkateMotion
    {
        public SkateMotion(PlayerSettings settings) { }
        public Vector2 Velocity { get; }
        /// <summary>input は長さ 0〜1。speedMultiplier は餅の重さ（REQ-013）。</summary>
        public void Tick(float deltaTime, Vector2 input, float speedMultiplier) { }
        public void AddImpulse(Vector2 impulse) { }   // ダッシュ
        public void Stop() { }
    }

    /// <summary>スタミナ。🔵 REQ-026（不足時はダッシュしない 🟡）</summary>
    public sealed class Stamina
    {
        public Stamina(PlayerSettings settings) { }
        public float Current { get; }
        public float Normalized { get; }              // ゲージ表示用 0〜1
        public bool TryConsumeDash() => default;
        public void Tick(float deltaTime) { }
    }

    /// <summary>チャージショット。🔵 REQ-023（最大 1.0 秒・2 段階）</summary>
    public sealed class ShotCharge
    {
        public ShotCharge(PlayerSettings settings) { }
        public bool IsCharging { get; }
        public float Normalized { get; }              // ゲージ表示用 0〜1
        public void Begin() { }
        public void Tick(float deltaTime) { }
        /// <summary>離した時点の段階 0（ノーマル）〜 ChargeStages を返してリセット。</summary>
        public int Release() => default;
        public void Cancel() { }                      // 被弾時
    }
}

// ============================================================================
// 信頼性レベルサマリー
// - 🔵 青信号: 約 75%
// - 🟡 黄信号: 約 25%（主に調整値と、リンクを矩形とする仮定）
// - 🔴 赤信号: 0%
// 品質評価: 高品質
// ============================================================================
