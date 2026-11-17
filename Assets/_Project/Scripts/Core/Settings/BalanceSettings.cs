using System;

namespace MochiHockey.Core
{
    /// <summary>
    /// Core が使う調整値をまとめたもの（REQ-402）。
    /// Core は数値の既定値を持たない。Runtime の GameBalanceConfig.ToSettings() で作って渡す。
    /// リンクの寸法（RinkBounds）は TASK-0009 で追加する。
    /// </summary>
    public sealed class BalanceSettings
    {
        /// <summary>餅のレベルの数（Lv.1〜Lv.3）。レベル別の配列はこの長さでなければならない。</summary>
        public const int MochiLevelCount = 3;

        public MochiSettings Mochi;
        public PettanSettings Pettan;
        public MatchSettings Match;
        public RiceSettings Rice;
        public PlayerSettings Player;

        /// <summary>
        /// 値の組み合わせが正しいかを調べる。おかしければ InvalidOperationException を投げる。
        /// </summary>
        public void Validate()
        {
            Require(Mochi, nameof(Mochi));
            Require(Pettan, nameof(Pettan));
            Require(Match, nameof(Match));
            Require(Rice, nameof(Rice));
            Require(Player, nameof(Player));

            RequireLevelArray(Mochi.ScaleByLevel, nameof(MochiSettings.ScaleByLevel));
            RequireLevelArray(Mochi.PointsByLevel, nameof(MochiSettings.PointsByLevel));
            RequireLevelArray(Mochi.SpeedMultiplierByLevel, nameof(MochiSettings.SpeedMultiplierByLevel));

            if (Mochi.Lv3Threshold <= Mochi.Lv2Threshold)
            {
                throw new InvalidOperationException(
                    $"{nameof(MochiSettings.Lv3Threshold)} ({Mochi.Lv3Threshold}) must be greater than " +
                    $"{nameof(MochiSettings.Lv2Threshold)} ({Mochi.Lv2Threshold}).");
            }

            if (Rice.MinCount > Rice.MaxCount)
            {
                throw new InvalidOperationException(
                    $"{nameof(RiceSettings.MinCount)} ({Rice.MinCount}) must not exceed " +
                    $"{nameof(RiceSettings.MaxCount)} ({Rice.MaxCount}).");
            }
        }

        private static void Require(object child, string name)
        {
            if (child == null)
            {
                throw new InvalidOperationException($"{name} settings are missing.");
            }
        }

        private static void RequireLevelArray(Array array, string name)
        {
            if (array == null || array.Length != MochiLevelCount)
            {
                var actual = array == null ? "null" : array.Length.ToString();
                throw new InvalidOperationException(
                    $"{name} must have {MochiLevelCount} elements (one per level), but has {actual}.");
            }
        }
    }
}
