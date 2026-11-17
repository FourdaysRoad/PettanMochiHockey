using System;
using System.Numerics;

namespace MochiHockey.Core
{
    /// <summary>
    /// ご飯粒（もち米）の補充のタイミングと出現位置を決める（REQ-011）。
    /// - 最小数を下回っていれば、その場で最小数まで補充する
    /// - 最小数以上・最大数未満なら RefillInterval ごとに 1 個ずつ補充する
    /// - 最大数に達していれば出さない（その間はタイマーも進めない）
    /// </summary>
    public sealed class RiceSpawnSchedule
    {
        // 1/60 秒を積み重ねた誤差で間隔に届かないことがないようにする許容差
        private const float TimeTolerance = 1e-4f;

        // 除外領域ばかりでも無限ループにしないための試行回数の上限
        private const int MaxPickAttempts = 100;

        private readonly RiceSettings _settings;
        private readonly RinkBounds _spawnArea;
        private readonly IRandom _random;
        private float _timer;

        /// <param name="spawnArea">出現してよい範囲。壁の中に出ないよう、リンクより内側を渡す。</param>
        public RiceSpawnSchedule(RiceSettings settings, RinkBounds spawnArea, IRandom random)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            if (!spawnArea.IsValid)
            {
                throw new ArgumentException("Spawn area must have a positive size.", nameof(spawnArea));
            }

            _spawnArea = spawnArea;
        }

        /// <summary>このフレームで出すべき数（0 以上）を返す。</summary>
        /// <param name="currentCount">今リンク上にあるご飯粒の数。</param>
        public int Tick(float deltaTime, int currentCount)
        {
            if (deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Delta time must not be negative.");
            }

            if (currentCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(currentCount), currentCount, "Count must not be negative.");
            }

            if (currentCount < _settings.MinCount)
            {
                _timer = 0f;
                return _settings.MinCount - currentCount;
            }

            var room = _settings.MaxCount - currentCount;
            if (room <= 0 || _settings.RefillInterval <= 0f)
            {
                _timer = 0f;
                return 0;
            }

            _timer += deltaTime;
            var due = (int)MathF.Floor(_timer / _settings.RefillInterval + TimeTolerance);
            if (due <= 0)
            {
                return 0;
            }

            if (due >= room)
            {
                // 満杯になるので、余った時間は持ち越さない
                _timer = 0f;
                return room;
            }

            _timer = MathF.Max(0f, _timer - due * _settings.RefillInterval);
            return due;
        }

        /// <summary>出現範囲の中からランダムな位置を選ぶ。除外領域（ゴールの中など）は呼び出し側が判定する。</summary>
        /// <param name="isExcluded">true を返した位置は使わない。null なら除外しない。</param>
        public Vector2 PickPosition(Func<Vector2, bool> isExcluded)
        {
            for (var i = 0; i < MaxPickAttempts; i++)
            {
                var x = Lerp(_spawnArea.Min.X, _spawnArea.Max.X, _random.NextFloat());
                var y = Lerp(_spawnArea.Min.Y, _spawnArea.Max.Y, _random.NextFloat());
                var position = new Vector2(x, y);
                if (isExcluded == null || !isExcluded(position))
                {
                    return position;
                }
            }

            throw new InvalidOperationException(
                $"Could not find a spawn position outside the excluded areas in {MaxPickAttempts} attempts.");
        }

        private static float Lerp(float from, float to, float t) => from + (to - from) * t;
    }
}
