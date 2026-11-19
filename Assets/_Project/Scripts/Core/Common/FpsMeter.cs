using System;

namespace MochiHockey.Core
{
    /// <summary>
    /// fps の計測（TASK-0031）。Interval 秒ごとに、その間の平均 fps・平均のフレーム時間・いちばん遅かったフレームを出す。
    /// 毎フレーム呼ぶので、メモリを確保しない。
    /// </summary>
    public sealed class FpsMeter
    {
        private readonly float _interval;
        private float _elapsed;
        private int _frames;
        private float _worst;

        public FpsMeter(float interval)
        {
            _interval = MathF.Max(0.01f, interval);
        }

        /// <summary>一度でも計測結果が出たか。</summary>
        public bool HasReading { get; private set; }

        public float Fps { get; private set; }

        public float AverageMilliseconds { get; private set; }

        /// <summary>直近の区間でいちばん遅かったフレーム（ミリ秒）。カクつきの目安。</summary>
        public float WorstMilliseconds { get; private set; }

        /// <summary>1 フレーム分の時間を足す。新しい計測結果が出たら true。</summary>
        public bool Tick(float deltaTime)
        {
            if (deltaTime <= 0f || float.IsNaN(deltaTime))
            {
                return false;
            }

            _elapsed += deltaTime;
            _frames++;
            _worst = MathF.Max(_worst, deltaTime);
            if (_elapsed < _interval)
            {
                return false;
            }

            Fps = _frames / _elapsed;
            AverageMilliseconds = _elapsed / _frames * 1000f;
            WorstMilliseconds = _worst * 1000f;
            HasReading = true;

            _elapsed = 0f;
            _frames = 0;
            _worst = 0f;
            return true;
        }
    }
}
