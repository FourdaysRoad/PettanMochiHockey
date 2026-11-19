using System;

namespace MochiHockey.Core
{
    /// <summary>
    /// 杵の見た目の振り（REQ-025 の見た目）。振り始めからの経過時間から、杵の水平の角度（度、+ が右）を求める。
    /// 構え（RestYaw）から ArcAngle だけ左へ振り抜き（SweepTime、振り始めが速い）、ReturnTime で構えに戻る。
    /// 当たり判定は MalletHitbox が別に持つので、ここは見た目だけ。
    /// </summary>
    public readonly struct MalletSwingPose
    {
        public MalletSwingPose(float restYaw, float arcAngle, float sweepTime, float returnTime)
        {
            RestYaw = restYaw;
            ArcAngle = arcAngle;
            SweepTime = MathF.Max(0f, sweepTime);
            ReturnTime = MathF.Max(0f, returnTime);
        }

        /// <summary>構えの角度（度）。</summary>
        public float RestYaw { get; }

        /// <summary>振り抜く角度（度）。</summary>
        public float ArcAngle { get; }

        public float SweepTime { get; }

        public float ReturnTime { get; }

        /// <summary>振り始めてから構えに戻るまでの秒数。</summary>
        public float Duration => SweepTime + ReturnTime;

        public bool IsSwinging(float elapsed) => elapsed >= 0f && elapsed < Duration;

        public float YawAt(float elapsed)
        {
            if (!IsSwinging(elapsed))
            {
                return RestYaw;
            }

            var end = RestYaw - ArcAngle;
            if (elapsed < SweepTime)
            {
                var t = elapsed / SweepTime;
                var eased = 1f - (1f - t) * (1f - t); // 振り始めが速く、振り抜く手前でゆっくり
                return RestYaw + (end - RestYaw) * eased;
            }

            var back = (elapsed - SweepTime) / ReturnTime;
            return end + (RestYaw - end) * back;
        }
    }
}
