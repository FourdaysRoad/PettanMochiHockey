using System;

namespace MochiHockey.Core
{
    /// <summary>ダッシュに使うスタミナ（REQ-026）。最大値から始まり、時間で回復する。</summary>
    public sealed class Stamina
    {
        private readonly PlayerSettings _settings;

        public Stamina(PlayerSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Current = _settings.StaminaMax;
        }

        public float Current { get; private set; }

        /// <summary>ゲージ表示用（0〜1）。</summary>
        public float Normalized => _settings.StaminaMax > 0f ? Current / _settings.StaminaMax : 0f;

        /// <summary>残りがダッシュ 1 回分以上あれば消費して true。足りなければ何もせず false。</summary>
        public bool TryConsumeDash()
        {
            if (Current < _settings.DashCost)
            {
                return false;
            }

            Current -= _settings.DashCost;
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Delta time must not be negative.");
            }

            Current = MathF.Min(_settings.StaminaMax, Current + _settings.StaminaRegenPerSecond * deltaTime);
        }
    }
}
