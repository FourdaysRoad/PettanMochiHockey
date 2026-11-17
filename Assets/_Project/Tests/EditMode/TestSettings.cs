using System.Numerics;
using MochiHockey.Core;

namespace MochiHockey.Tests.EditMode
{
    /// <summary>
    /// EditMode テスト用の設定を組み立てる。
    /// Core は既定値を持たないので、テストで使う値はここで決める（仕様書の値を基準にする）。
    /// </summary>
    public static class TestSettings
    {
        public static BalanceSettings Create()
        {
            return new BalanceSettings
            {
                Mochi = CreateMochi(),
                Pettan = CreatePettan(),
                Match = CreateMatch(),
                Rice = CreateRice(),
                Player = CreatePlayer(),
                Rink = CreateRink(),
            };
        }

        public static MochiSettings CreateMochi()
        {
            return new MochiSettings
            {
                Lv2Threshold = 3,
                Lv3Threshold = 6,
                ScaleByLevel = new[] { 1.0f, 1.5f, 2.5f },
                PointsByLevel = new[] { 1, 2, 3 },
                SpeedMultiplierByLevel = new[] { 1.0f, 0.9f, 0.75f },
                RegrabCooldown = 0.3f,
            };
        }

        public static PettanSettings CreatePettan()
        {
            return new PettanSettings
            {
                HitStopDuration = 0.08f,
                LaunchNominalDuration = 0.6f,
                LaunchMaxDuration = 1.2f,
                LaunchSpeed = 12f,
                LaunchBounceHeight = 1f,
                SlideDownStartTime = 1.5f,
                RecoverTime = 2.6f,
                InvincibleEndTime = 3.6f,
                SlideDownSpeed = 0.5f,
                StuckScale = new Vector3(1.3f, 1.3f, 0.05f),
            };
        }

        public static MatchSettings CreateMatch()
        {
            return new MatchSettings
            {
                MatchDuration = 180f,
                KickoffCountdown = 3f,
                MochiRespawnDelay = 1f,
            };
        }

        public static RiceSettings CreateRice()
        {
            return new RiceSettings
            {
                MinCount = 5,
                MaxCount = 10,
                RefillInterval = 1.5f,
            };
        }

        /// <summary>X: -12〜12、Z: -7〜7 のリンク。</summary>
        public static RinkBounds CreateRink()
        {
            return new RinkBounds(new Vector2(-12f, -7f), new Vector2(12f, 7f));
        }

        public static PlayerSettings CreatePlayer()
        {
            return new PlayerSettings
            {
                MaxSpeed = 6f,
                Acceleration = 20f,
                Friction = 3f,
                StaminaMax = 100f,
                DashCost = 35f,
                StaminaRegenPerSecond = 20f,
                DashImpulse = 8f,
                ChargeMaxTime = 1f,
                ChargeStages = 2,
                SwingActiveTime = 0.15f,
                WhiffRecoveryTime = 0.4f,
            };
        }
    }
}
