namespace MochiHockey.Core
{
    /// <summary>試合の流れ（REQ-001〜005）。</summary>
    public enum MatchPhase
    {
        /// <summary>試合開始前のカウントダウン。試合時間は進まない。</summary>
        Kickoff,

        /// <summary>試合中。試合時間が減っていく。</summary>
        Playing,

        /// <summary>ゴール後、餅が中央に出し直されるのを待っている。</summary>
        MochiRespawning,

        /// <summary>同点で時間切れになった後の延長。次に得点したチームが勝つ。</summary>
        SuddenDeath,

        /// <summary>試合終了。</summary>
        Finished,
    }
}
