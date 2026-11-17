using System.Numerics;

namespace MochiHockey.Core
{
    /// <summary>ペッタン（被弾）の演出・時間に関する調整値。値は GameBalanceConfig から渡す。</summary>
    public sealed class PettanSettings
    {
        /// <summary>被弾直後に止まる秒数。</summary>
        public float HitStopDuration;


        /// <summary>吹っ飛びを延長できる上限の秒数。</summary>
        public float LaunchMaxDuration;

        /// <summary>吹っ飛びの直進速度（m/s）。</summary>
        public float LaunchSpeed;

        /// <summary>吹っ飛び中に壁で跳ね返る回数。この回数だけ跳ね返ったあと、次に当たった壁に貼り付く。</summary>
        public int LaunchWallBounces;

        /// <summary>被弾からずり落ちが始まるまでの秒数。</summary>
        public float SlideDownStartTime;

        /// <summary>被弾から復帰するまでの秒数。</summary>
        public float RecoverTime;

        /// <summary>被弾から無敵が終わるまでの秒数。</summary>
        public float InvincibleEndTime;

        /// <summary>壁沿いにずり落ちる速度（m/s）。</summary>
        public float SlideDownSpeed;

        /// <summary>壁に貼り付いている間の大きさ。</summary>
        public Vector3 StuckScale;
    }
}
