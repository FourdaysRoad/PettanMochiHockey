namespace MochiHockey.Runtime
{
    /// <summary>餅に起きたこと（効果音用、MochiBall.AnyBallEvent）。</summary>
    public enum BallEvent
    {
        /// <summary>打ち出された（段階は MochiBall.LastShotStage）。</summary>
        Shot,

        /// <summary>ご飯粒を吸収した（レベルは変わらない）。</summary>
        RiceAbsorbed,

        /// <summary>ご飯粒を吸収してレベルが上がった。</summary>
        LevelUp,

        /// <summary>壁などで勢いよく跳ねた。</summary>
        Bounced,
    }
}
