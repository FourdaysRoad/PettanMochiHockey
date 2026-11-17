using System;

namespace MochiHockey.Core
{
    /// <summary>System.Random を使う IRandom（ゲーム本番用）。</summary>
    public sealed class SystemRandom : IRandom
    {
        private readonly Random _random;

        public SystemRandom()
        {
            _random = new Random();
        }

        public SystemRandom(int seed)
        {
            _random = new Random(seed);
        }

        public float NextFloat()
        {
            // double → float の丸めで 1.0 になるのを避ける
            var value = (float)_random.NextDouble();
            return value < 1f ? value : 0f;
        }
    }
}
