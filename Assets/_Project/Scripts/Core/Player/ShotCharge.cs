using System;

namespace MochiHockey.Core
{
    /// <summary>
    /// チャージショット（REQ-023）。長押しした時間から段階を求める。
    /// 段階は ChargeMaxTime を ChargeStages 等分した区切りごとに 1 つ上がる
    /// （最大 1.0 秒・2 段階なら 0.5 秒で段階 1、1.0 秒で段階 2）。
    /// </summary>
    public sealed class ShotCharge
    {
        // 1/60 秒を 30 回足して 0.49999 になっても、0.5 秒の区切りを越えたとみなすための許容差
        private const float BoundaryTolerance = 1e-4f;

        private readonly PlayerSettings _settings;
        private float _elapsed;

        public ShotCharge(PlayerSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public bool IsCharging { get; private set; }

        /// <summary>ゲージ表示用（0〜1）。チャージしていないときは 0。</summary>
        public float Normalized =>
            IsCharging && _settings.ChargeMaxTime > 0f ? MathF.Min(1f, _elapsed / _settings.ChargeMaxTime) : 0f;

        /// <summary>ボタンを押した。すでにチャージ中なら何もしない。</summary>
        public void Begin()
        {
            if (IsCharging)
            {
                return;
            }

            IsCharging = true;
            _elapsed = 0f;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Delta time must not be negative.");
            }

            if (IsCharging)
            {
                _elapsed += deltaTime;
            }
        }

        /// <summary>ボタンを離した。段階 0（ノーマル）〜 ChargeStages を返してリセットする。</summary>
        public int Release()
        {
            var stage = CurrentStage();
            Cancel();
            return stage;
        }

        /// <summary>被弾などでチャージを捨てる。</summary>
        public void Cancel()
        {
            IsCharging = false;
            _elapsed = 0f;
        }

        private int CurrentStage()
        {
            if (!IsCharging || _settings.ChargeStages <= 0 || _settings.ChargeMaxTime <= 0f)
            {
                return 0;
            }

            var stageDuration = _settings.ChargeMaxTime / _settings.ChargeStages;
            var stage = (int)MathF.Floor(_elapsed / stageDuration + BoundaryTolerance);
            return Math.Min(stage, _settings.ChargeStages);
        }
    }
}
