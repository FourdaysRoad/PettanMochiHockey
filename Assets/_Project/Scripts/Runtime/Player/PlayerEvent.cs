namespace MochiHockey.Runtime
{
    /// <summary>プレイヤーに起きたこと（効果音用、PlayerController.AnyPlayerEvent）。</summary>
    public enum PlayerEvent
    {
        /// <summary>杵を振った。</summary>
        Swung,

        /// <summary>ダッシュした。</summary>
        Dashed,

        /// <summary>叩かれて HitStop に入った。</summary>
        Hit,

        /// <summary>吹っ飛び中に壁で跳ね返った。</summary>
        LaunchBounced,

        /// <summary>壁に貼り付いた。</summary>
        Stuck,

        /// <summary>壁から復帰した（ポンッ！）。</summary>
        Recovered,
    }
}
