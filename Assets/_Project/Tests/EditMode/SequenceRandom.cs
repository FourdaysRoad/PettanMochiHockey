using System;
using MochiHockey.Core;

namespace MochiHockey.Tests.EditMode
{
    /// <summary>テスト用：決めた値を順番に返す乱数。最後まで使ったら先頭に戻る。</summary>
    public sealed class SequenceRandom : IRandom
    {
        private readonly float[] _values;
        private int _index;

        public SequenceRandom(params float[] values)
        {
            if (values == null || values.Length == 0)
            {
                throw new ArgumentException("At least one value is required.", nameof(values));
            }

            _values = values;
        }

        public int CallCount { get; private set; }

        public float NextFloat()
        {
            CallCount++;
            var value = _values[_index];
            _index = (_index + 1) % _values.Length;
            return value;
        }
    }
}
