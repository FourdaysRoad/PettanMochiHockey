namespace MochiHockey.Core
{
    /// <summary>プレイヤーの移動・スタミナ・杵に関する調整値。値は GameBalanceConfig から渡す。</summary>
    public sealed class PlayerSettings
    {
        /// <summary>最高速度（m/s）。</summary>
        public float MaxSpeed;

        /// <summary>加速度（m/s²）。</summary>
        public float Acceleration;

        /// <summary>入力がないときの減速の強さ（1/s）。</summary>
        public float Friction;

        /// <summary>スタミナの最大値。</summary>
        public float StaminaMax;

        /// <summary>ダッシュ 1 回で消費するスタミナ。</summary>
        public float DashCost;

        /// <summary>1 秒あたりのスタミナ回復量。</summary>
        public float StaminaRegenPerSecond;

        /// <summary>ダッシュで加える速度（m/s）。</summary>
        public float DashImpulse;

        /// <summary>チャージが最大になるまでの秒数。</summary>
        public float ChargeMaxTime;

        /// <summary>チャージの段階数。</summary>
        public int ChargeStages;

        /// <summary>杵の当たり判定が続く秒数。</summary>
        public float SwingActiveTime;

        /// <summary>空振りしたときの硬直の秒数。</summary>
        public float WhiffRecoveryTime;
    }
}
