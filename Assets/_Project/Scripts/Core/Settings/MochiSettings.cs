namespace MochiHockey.Core
{
    /// <summary>餅の成長に関する調整値（REQ-013）。値は GameBalanceConfig から渡す。</summary>
    public sealed class MochiSettings
    {
        /// <summary>この数以上のご飯粒で Lv.2 になる。</summary>
        public int Lv2Threshold;

        /// <summary>この数以上のご飯粒で Lv.3 になる。</summary>
        public int Lv3Threshold;

        /// <summary>レベル別の大きさ（Lv.1〜Lv.3 の 3 要素）。</summary>
        public float[] ScaleByLevel;

        /// <summary>レベル別の得点（3 要素）。</summary>
        public int[] PointsByLevel;

        /// <summary>レベル別の持ち手の移動速度の倍率（3 要素）。</summary>
        public float[] SpeedMultiplierByLevel;

        /// <summary>手放した直後に同じプレイヤーが再び吸着できるまでの秒数（REQ-024）。</summary>
        public float RegrabCooldown;
    }
}
