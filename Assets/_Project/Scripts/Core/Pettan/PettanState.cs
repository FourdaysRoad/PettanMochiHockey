namespace MochiHockey.Core
{
    /// <summary>被弾後の状態（REQ-031）。</summary>
    public enum PettanState
    {
        /// <summary>通常。操作でき、杵で叩かれる。</summary>
        Normal,

        /// <summary>被弾直後の一瞬の停止。</summary>
        HitStop,

        /// <summary>攻撃者の正面方向へ吹っ飛んでいる。</summary>
        Launched,

        /// <summary>壁に貼り付いている（後半はずり落ちる）。</summary>
        Stuck,

        /// <summary>復帰後の無敵。操作はできる。</summary>
        Invincible,
    }
}
