using System;
using System.Numerics;

namespace MochiHockey.Core
{
    /// <summary>
    /// リンク外周の壁。床面 XZ の軸に沿った矩形として扱う（X = x、Y = z）。
    /// 吹っ飛び（REQ-033）の行き先の壁と、その法線を求めるのに使う。
    /// </summary>
    public readonly struct RinkBounds
    {
        public RinkBounds(Vector2 min, Vector2 max)
        {
            if (!(min.X < max.X) || !(min.Y < max.Y))
            {
                throw new ArgumentException($"Rink min {min} must be below max {max} on both axes.");
            }

            Min = min;
            Max = max;
        }

        public Vector2 Min { get; }
        public Vector2 Max { get; }

        /// <summary>大きさを持つリンクか（default のままではないか）。</summary>
        public bool IsValid => Min.X < Max.X && Min.Y < Max.Y;

        /// <summary>壁の上も含めて、リンクの中にあるか。</summary>
        public bool Contains(Vector2 point) =>
            point.X >= Min.X && point.X <= Max.X && point.Y >= Min.Y && point.Y <= Max.Y;

        /// <summary>
        /// origin から direction へまっすぐ進んだとき、最初に当たる壁の位置と、壁の内向きの法線を求める。
        /// direction がゼロ、または origin がリンクの外なら false。
        /// 角にちょうど届く場合は X 側（左右）の壁を返す。
        /// </summary>
        public bool TryGetWallHit(Vector2 origin, Vector2 direction, out Vector2 hitPoint, out Vector2 wallNormal)
        {
            hitPoint = default;
            wallNormal = default;

            if (direction == Vector2.Zero || !Contains(origin))
            {
                return false;
            }

            var tX = float.PositiveInfinity;
            var normalX = Vector2.Zero;
            if (direction.X > 0f)
            {
                tX = (Max.X - origin.X) / direction.X;
                normalX = new Vector2(-1f, 0f);
            }
            else if (direction.X < 0f)
            {
                tX = (Min.X - origin.X) / direction.X;
                normalX = new Vector2(1f, 0f);
            }

            var tY = float.PositiveInfinity;
            var normalY = Vector2.Zero;
            if (direction.Y > 0f)
            {
                tY = (Max.Y - origin.Y) / direction.Y;
                normalY = new Vector2(0f, -1f);
            }
            else if (direction.Y < 0f)
            {
                tY = (Min.Y - origin.Y) / direction.Y;
                normalY = new Vector2(0f, 1f);
            }

            var hitsSideWall = tX <= tY;
            var t = hitsSideWall ? tX : tY;
            wallNormal = hitsSideWall ? normalX : normalY;

            // 計算誤差で壁の外にはみ出さないように丸める
            hitPoint = Vector2.Clamp(origin + direction * t, Min, Max);
            return true;
        }
    }
}
