namespace MochiHockey.Core
{
    /// <summary>試合の進行に関する調整値。値は GameBalanceConfig から渡す。</summary>
    public sealed class MatchSettings
    {
        /// <summary>試合時間（秒）。</summary>
        public float MatchDuration;

        /// <summary>キックオフ前のカウントダウン（秒）。</summary>
        public float KickoffCountdown;

        /// <summary>ゴール後に餅が中央に戻るまでの秒数。</summary>
        public float MochiRespawnDelay;
    }
}
