using System;
using System.Numerics;

namespace MochiHockey.Core
{
    /// <summary>
    /// 急な方向転換で氷しぶきを出すか（REQ-021 の演出）。
    /// 速く滑っているときに、進んでいる向きと入力の向きが TurnAngle 以上ずれたら出す。
    /// 強さは 0（出さない）か 0.5〜1（ずれが大きいほど強い。真後ろで 1）。
    /// </summary>
    public readonly struct IceSprayRule
    {
        // これより小さい入力は「手を離している」とみなす
        private const float InputDeadZone = 0.1f;

        public IceSprayRule(float minSpeed, float turnAngle)
        {
            MinSpeed = MathF.Max(0f, minSpeed);
            TurnAngle = Math.Clamp(turnAngle, 0f, 179f);
        }

        /// <summary>しぶきを出す最低の速さ（m/s）。</summary>
        public float MinSpeed { get; }

        /// <summary>しぶきを出す向きのずれ（度）。</summary>
        public float TurnAngle { get; }

        /// <param name="velocity">床面の速度（m/s）</param>
        /// <param name="input">移動の入力（長さ 1 まで）</param>
        public float Intensity(Vector2 velocity, Vector2 input)
        {
            var speed = velocity.Length();
            if (speed < MinSpeed || speed <= 0f || input.Length() < InputDeadZone)
            {
                return 0f;
            }

            var cos = Vector2.Dot(velocity / speed, Vector2.Normalize(input));
            var angle = MathF.Acos(Math.Clamp(cos, -1f, 1f)) * 180f / MathF.PI;
            if (angle < TurnAngle)
            {
                return 0f;
            }

            var t = (angle - TurnAngle) / (180f - TurnAngle);
            return 0.5f + 0.5f * Math.Clamp(t, 0f, 1f);
        }
    }
}
