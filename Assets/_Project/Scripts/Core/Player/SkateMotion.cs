using System;
using System.Numerics;

namespace MochiHockey.Core
{
    /// <summary>
    /// 氷の上を滑る、慣性のある移動（REQ-021）。床面 XZ の速度だけを扱う。
    /// - 入力があるとき：目標速度（入力 × 最高速度 × 速度倍率）へ加速度で近づく
    /// - 入力がないとき、または最高速度を超えているとき（ダッシュ直後）：摩擦で指数的に減速する
    /// </summary>
    public sealed class SkateMotion
    {
        // 正規化したベクトルの長さの誤差で「最高速度を超えた」と判定しないための許容差
        private const float SpeedTolerance = 1e-4f;

        private readonly PlayerSettings _settings;

        public SkateMotion(PlayerSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>速度（m/s）。X = x、Y = z。</summary>
        public Vector2 Velocity { get; private set; }

        /// <param name="input">スティック入力。長さが 1 を超える場合は 1 に丸める。</param>
        /// <param name="speedMultiplier">持っている餅のレベルによる速度倍率。</param>
        public void Tick(float deltaTime, Vector2 input, float speedMultiplier)
        {
            if (deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Delta time must not be negative.");
            }

            var maxSpeed = _settings.MaxSpeed * speedMultiplier;
            var hasInput = input.LengthSquared() > 0f;

            if (!hasInput || Velocity.Length() > maxSpeed + SpeedTolerance)
            {
                Velocity *= MathF.Exp(-_settings.Friction * deltaTime);
                return;
            }

            var target = ClampLength(input, 1f) * maxSpeed;
            Velocity = MoveTowards(Velocity, target, _settings.Acceleration * deltaTime);
        }

        /// <summary>速度を瞬間的に足す（ダッシュ）。最高速度を超えてよい。</summary>
        public void AddImpulse(Vector2 impulse)
        {
            Velocity += impulse;
        }

        /// <summary>速度を置き換える（壁キックで飛び出したときなど）。最高速度を超えてよい。</summary>
        public void SetVelocity(Vector2 velocity)
        {
            Velocity = velocity;
        }

        public void Stop()
        {
            Velocity = Vector2.Zero;
        }

        private static Vector2 ClampLength(Vector2 v, float maxLength)
        {
            var length = v.Length();
            return length > maxLength ? v * (maxLength / length) : v;
        }

        private static Vector2 MoveTowards(Vector2 current, Vector2 target, float maxDelta)
        {
            var delta = target - current;
            var distance = delta.Length();
            return distance <= maxDelta ? target : current + delta * (maxDelta / distance);
        }
    }
}
