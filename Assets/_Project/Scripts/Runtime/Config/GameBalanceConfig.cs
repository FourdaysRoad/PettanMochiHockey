using System;
using MochiHockey.Core;
using UnityEngine;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// ゲームの調整値の置き場（REQ-402）。数値はここ（とこのアセット）にだけ置き、
    /// ToSettings() で Core 用の BalanceSettings に変換して渡す。
    /// 初期値は仕様書（docs/spec/pettan-mochi-hockey.md）の値。
    /// 「仮」と書いた項目は仕様書に値がないため、遊びながら調整する。
    /// </summary>
    [CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "Mochi Hockey/Game Balance Config")]
    public sealed class GameBalanceConfig : ScriptableObject
    {
        public MochiConfig Mochi = new MochiConfig();
        public PettanConfig Pettan = new PettanConfig();
        public MatchConfig Match = new MatchConfig();
        public RiceConfig Rice = new RiceConfig();
        public PlayerConfig Player = new PlayerConfig();
        public RinkConfig Rink = new RinkConfig();

        /// <summary>Core 用の設定に変換する。値がおかしければ InvalidOperationException を投げる。</summary>
        public BalanceSettings ToSettings()
        {
            var settings = new BalanceSettings
            {
                Mochi = new MochiSettings
                {
                    Lv2Threshold = Mochi.Lv2Threshold,
                    Lv3Threshold = Mochi.Lv3Threshold,
                    ScaleByLevel = CopyOf(Mochi.ScaleByLevel),
                    PointsByLevel = CopyOf(Mochi.PointsByLevel),
                    SpeedMultiplierByLevel = CopyOf(Mochi.SpeedMultiplierByLevel),
                    RegrabCooldown = Mochi.RegrabCooldown,
                },
                Pettan = new PettanSettings
                {
                    HitStopDuration = Pettan.HitStopDuration,
                    LaunchNominalDuration = Pettan.LaunchNominalDuration,
                    LaunchMaxDuration = Pettan.LaunchMaxDuration,
                    LaunchSpeed = Pettan.LaunchSpeed,
                    LaunchBounceHeight = Pettan.LaunchBounceHeight,
                    SlideDownStartTime = Pettan.SlideDownStartTime,
                    RecoverTime = Pettan.RecoverTime,
                    InvincibleEndTime = Pettan.InvincibleEndTime,
                    SlideDownSpeed = Pettan.SlideDownSpeed,
                    StuckScale = new System.Numerics.Vector3(
                        Pettan.StuckScale.x, Pettan.StuckScale.y, Pettan.StuckScale.z),
                },
                Match = new MatchSettings
                {
                    MatchDuration = Match.MatchDuration,
                    KickoffCountdown = Match.KickoffCountdown,
                    MochiRespawnDelay = Match.MochiRespawnDelay,
                },
                Rice = new RiceSettings
                {
                    MinCount = Rice.MinCount,
                    MaxCount = Rice.MaxCount,
                    RefillInterval = Rice.RefillInterval,
                },
                Player = new PlayerSettings
                {
                    MaxSpeed = Player.MaxSpeed,
                    Acceleration = Player.Acceleration,
                    Friction = Player.Friction,
                    StaminaMax = Player.StaminaMax,
                    DashCost = Player.DashCost,
                    StaminaRegenPerSecond = Player.StaminaRegenPerSecond,
                    DashImpulse = Player.DashImpulse,
                    ChargeMaxTime = Player.ChargeMaxTime,
                    ChargeStages = Player.ChargeStages,
                    SwingActiveTime = Player.SwingActiveTime,
                    WhiffRecoveryTime = Player.WhiffRecoveryTime,
                },
                Rink = ToRinkBounds(Rink.Size),
            };

            settings.Validate();
            return settings;
        }

        /// <summary>原点を中心にした矩形にする。大きさが 0 以下なら default のままにして Validate で弾く。</summary>
        private static RinkBounds ToRinkBounds(Vector2 size)
        {
            if (size.x <= 0f || size.y <= 0f)
            {
                return default;
            }

            var half = new System.Numerics.Vector2(size.x / 2f, size.y / 2f);
            return new RinkBounds(-half, half);
        }

        private static T[] CopyOf<T>(T[] source) => source == null ? null : (T[])source.Clone();

        [Serializable]
        public sealed class MochiConfig
        {
            [Tooltip("この数以上のご飯粒で Lv.2")]
            [Min(1)] public int Lv2Threshold = 3;

            [Tooltip("この数以上のご飯粒で Lv.3")]
            [Min(1)] public int Lv3Threshold = 6;

            [Tooltip("レベル別の大きさ（Lv.1, Lv.2, Lv.3）")]
            public float[] ScaleByLevel = { 1.0f, 1.5f, 2.5f };

            [Tooltip("レベル別の得点（Lv.1, Lv.2, Lv.3）")]
            public int[] PointsByLevel = { 1, 2, 3 };

            [Tooltip("レベル別の持ち手の速度倍率（Lv.1, Lv.2, Lv.3）")]
            public float[] SpeedMultiplierByLevel = { 1.0f, 0.9f, 0.75f };

            [Tooltip("手放した直後に再び吸着できるまでの秒数")]
            [Min(0f)] public float RegrabCooldown = 0.3f;
        }

        [Serializable]
        public sealed class PettanConfig
        {
            [Tooltip("被弾直後に止まる秒数")]
            [Min(0f)] public float HitStopDuration = 0.08f;

            [Tooltip("吹っ飛びの標準の秒数（壁に届かなければ延長する）")]
            [Min(0f)] public float LaunchNominalDuration = 0.6f;

            [Tooltip("仮：吹っ飛びを延長できる上限の秒数")]
            [Min(0f)] public float LaunchMaxDuration = 1.2f;

            [Tooltip("仮：吹っ飛びの直進速度（m/s）")]
            [Min(0f)] public float LaunchSpeed = 12f;

            [Tooltip("仮：吹っ飛び中に 1 回バウンドする高さ（m）")]
            [Min(0f)] public float LaunchBounceHeight = 1f;

            [Tooltip("被弾からずり落ちが始まるまでの秒数")]
            [Min(0f)] public float SlideDownStartTime = 1.5f;

            [Tooltip("被弾から復帰するまでの秒数")]
            [Min(0f)] public float RecoverTime = 2.6f;

            [Tooltip("被弾から無敵が終わるまでの秒数")]
            [Min(0f)] public float InvincibleEndTime = 3.6f;

            [Tooltip("仮：壁沿いにずり落ちる速度（m/s）")]
            [Min(0f)] public float SlideDownSpeed = 0.5f;

            [Tooltip("壁に貼り付いている間の大きさ")]
            public Vector3 StuckScale = new Vector3(1.3f, 1.3f, 0.05f);
        }

        [Serializable]
        public sealed class MatchConfig
        {
            [Tooltip("試合時間（秒）")]
            [Min(1f)] public float MatchDuration = 180f;

            [Tooltip("仮：キックオフ前のカウントダウン（秒）")]
            [Min(0f)] public float KickoffCountdown = 3f;

            [Tooltip("ゴール後に餅が中央に戻るまでの秒数")]
            [Min(0f)] public float MochiRespawnDelay = 1f;
        }

        [Serializable]
        public sealed class RiceConfig
        {
            [Tooltip("リンク上のご飯粒の最小数（下回ると補充）")]
            [Min(0)] public int MinCount = 5;

            [Tooltip("リンク上のご飯粒の最大数")]
            [Min(0)] public int MaxCount = 10;

            [Tooltip("補充の間隔（秒）")]
            [Min(0f)] public float RefillInterval = 1.5f;
        }

        [Serializable]
        public sealed class PlayerConfig
        {
            [Tooltip("仮：最高速度（m/s）")]
            [Min(0f)] public float MaxSpeed = 6f;

            [Tooltip("仮：加速度（m/s²）")]
            [Min(0f)] public float Acceleration = 20f;

            [Tooltip("仮：入力がないときの減速の強さ（1/s）")]
            [Min(0f)] public float Friction = 3f;

            [Tooltip("仮：スタミナの最大値")]
            [Min(0f)] public float StaminaMax = 100f;

            [Tooltip("仮：ダッシュ 1 回の消費スタミナ")]
            [Min(0f)] public float DashCost = 35f;

            [Tooltip("仮：1 秒あたりのスタミナ回復量")]
            [Min(0f)] public float StaminaRegenPerSecond = 20f;

            [Tooltip("仮：ダッシュで加える速度（m/s）")]
            [Min(0f)] public float DashImpulse = 8f;

            [Tooltip("チャージが最大になるまでの秒数")]
            [Min(0f)] public float ChargeMaxTime = 1f;

            [Tooltip("チャージの段階数")]
            [Min(1)] public int ChargeStages = 2;

            [Tooltip("仮：杵の当たり判定が続く秒数")]
            [Min(0f)] public float SwingActiveTime = 0.15f;

            [Tooltip("仮：空振りしたときの硬直の秒数")]
            [Min(0f)] public float WhiffRecoveryTime = 0.4f;
        }

        [Serializable]
        public sealed class RinkConfig
        {
            [Tooltip("仮：リンクの内側の大きさ（x = 横幅、y = 奥行き。m）。原点が中心")]
            public Vector2 Size = new Vector2(24f, 14f);
        }
    }
}
