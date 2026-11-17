using System;

namespace MochiHockey.Core
{
    /// <summary>
    /// 累計のご飯粒の数から、餅のレベル・得点・大きさ・速度倍率・成長ゲージを求める（REQ-013, REQ-053）。
    /// </summary>
    public sealed class MochiGrowth
    {
        private readonly MochiSettings _settings;

        public MochiGrowth(MochiSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public MochiLevel GetLevel(int riceCount)
        {
            RequireNonNegative(riceCount);

            if (riceCount >= _settings.Lv3Threshold) return MochiLevel.Lv3;
            if (riceCount >= _settings.Lv2Threshold) return MochiLevel.Lv2;
            return MochiLevel.Lv1;
        }

        public int GetPoints(MochiLevel level) => _settings.PointsByLevel[IndexOf(level)];

        public float GetScale(MochiLevel level) => _settings.ScaleByLevel[IndexOf(level)];

        public float GetSpeedMultiplier(MochiLevel level) => _settings.SpeedMultiplierByLevel[IndexOf(level)];

        /// <summary>次のレベルまでの進捗（0〜1）。Lv.3 は常に 1。</summary>
        public float GetGrowthProgress(int riceCount)
        {
            switch (GetLevel(riceCount))
            {
                case MochiLevel.Lv1:
                    return Progress(riceCount, 0, _settings.Lv2Threshold);
                case MochiLevel.Lv2:
                    return Progress(riceCount, _settings.Lv2Threshold, _settings.Lv3Threshold);
                default:
                    return 1f;
            }
        }

        private static float Progress(int value, int from, int to) => (float)(value - from) / (to - from);

        private static int IndexOf(MochiLevel level)
        {
            if (level < MochiLevel.Lv1 || level > MochiLevel.Lv3)
            {
                throw new ArgumentOutOfRangeException(nameof(level), level, "Unknown mochi level.");
            }

            return (int)level - 1;
        }

        private static void RequireNonNegative(int riceCount)
        {
            if (riceCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(riceCount), riceCount, "Rice count must not be negative.");
            }
        }
    }
}
