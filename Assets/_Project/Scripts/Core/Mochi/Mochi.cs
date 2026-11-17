using System;

namespace MochiHockey.Core
{
    /// <summary>
    /// 餅 1 個の状態（累計のご飯粒の数・持っているプレイヤー・再吸着禁止）。
    /// レベルと大きさは、落としたり手放したりしても維持する（REQ-014）。
    /// </summary>
    public sealed class Mochi
    {
        // 浮動小数の足し引きで 0.3 - 0.29 - 0.01 がわずかに正になっても禁止が残らないようにする許容差
        private const float CooldownTolerance = 1e-5f;

        private readonly MochiGrowth _growth;
        private readonly MochiSettings _settings;

        private int? _cooldownPlayerId;
        private float _cooldownRemaining;

        public Mochi(MochiGrowth growth, MochiSettings settings)
        {
            _growth = growth ?? throw new ArgumentNullException(nameof(growth));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Level = _growth.GetLevel(0);
        }

        /// <summary>累計のご飯粒の数。落としても減らない。</summary>
        public int RiceCount { get; private set; }

        public MochiLevel Level { get; private set; }

        /// <summary>持っているプレイヤー。null なら誰も持っていない。</summary>
        public int? HolderId { get; private set; }

        /// <summary>レベルが変わったとき（見た目・コライダーの更新用）。</summary>
        public event Action<MochiLevel> LevelChanged;

        /// <summary>
        /// 吸着を試みる（REQ-022）。ほかの人が持っているとき、
        /// または手放した直後の本人（REQ-024）は失敗する。
        /// </summary>
        public bool TryAttach(int playerId)
        {
            if (HolderId.HasValue)
            {
                return HolderId.Value == playerId;
            }

            if (IsBlocked(playerId))
            {
                return false;
            }

            HolderId = playerId;
            return true;
        }

        /// <summary>持たれているときだけご飯粒を吸収する（REQ-012）。</summary>
        public bool TryAbsorbRice()
        {
            if (!HolderId.HasValue)
            {
                return false;
            }

            RiceCount++;
            var level = _growth.GetLevel(RiceCount);
            if (level != Level)
            {
                Level = level;
                LevelChanged?.Invoke(level);
            }

            return true;
        }

        /// <summary>被弾で落とす（REQ-032）。再吸着禁止は付かない。</summary>
        public void Drop()
        {
            HolderId = null;
        }

        /// <summary>パス・シュートで手放す。手放した本人に再吸着禁止が付く（REQ-024）。</summary>
        public void Release()
        {
            if (!HolderId.HasValue)
            {
                return;
            }

            _cooldownPlayerId = HolderId;
            _cooldownRemaining = _settings.RegrabCooldown;
            HolderId = null;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Delta time must not be negative.");
            }

            if (_cooldownPlayerId.HasValue)
            {
                _cooldownRemaining -= deltaTime;
                if (_cooldownRemaining <= CooldownTolerance)
                {
                    _cooldownPlayerId = null;
                }
            }
        }

        private bool IsBlocked(int playerId) => _cooldownPlayerId == playerId;
    }
}
