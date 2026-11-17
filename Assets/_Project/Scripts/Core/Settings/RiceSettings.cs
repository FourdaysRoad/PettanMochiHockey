namespace MochiHockey.Core
{
    /// <summary>ご飯粒の出現に関する調整値。値は GameBalanceConfig から渡す。</summary>
    public sealed class RiceSettings
    {
        /// <summary>リンク上のご飯粒の最小数。これを下回ると補充する。</summary>
        public int MinCount;

        /// <summary>リンク上のご飯粒の最大数。</summary>
        public int MaxCount;

        /// <summary>補充の間隔（秒）。</summary>
        public float RefillInterval;
    }
}
