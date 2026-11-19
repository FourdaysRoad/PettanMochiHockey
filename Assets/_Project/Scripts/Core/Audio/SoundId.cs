namespace MochiHockey.Core
{
    /// <summary>効果音の種類（TASK-0029）。音はすべて SoundSynth がコードで合成する。</summary>
    public enum SoundId
    {
        /// <summary>杵を振る（ひゅっ）。</summary>
        Swing,

        /// <summary>杵が当たる（ペッタン！）。</summary>
        Pettan,

        /// <summary>吹っ飛び中に壁で跳ね返る（ぼよん）。</summary>
        LaunchBounce,

        /// <summary>壁に貼り付く（べちゃっ）。</summary>
        Stuck,

        /// <summary>壁から復帰する（ポンッ！、REQ-034）。</summary>
        Pon,

        /// <summary>ダッシュ（しゃっ）。</summary>
        Dash,

        /// <summary>餅を打つ（ぱこん）。</summary>
        Shot,

        /// <summary>ご飯粒を吸収する（ぴっ）。</summary>
        RiceGet,

        /// <summary>餅のレベルが上がる。</summary>
        LevelUp,

        /// <summary>餅が壁で跳ねる（ぽふ）。</summary>
        BallBounce,

        /// <summary>ゴール。</summary>
        Goal,

        /// <summary>キックオフの笛（短い）。</summary>
        Whistle,

        /// <summary>試合終了の笛（長い）。</summary>
        FinalWhistle,
    }
}
