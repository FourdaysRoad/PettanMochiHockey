using System;

namespace MochiHockey.Core
{
    /// <summary>
    /// 試合の BGM をコードで合成する（TASK-0029）。和風の音階（ヨナ抜きに近い D の陽音階）の旋律を琴のような音で、
    /// 三味線のような低音と、太鼓（ドン・カッ）を合わせた 8 小節のループ。
    /// 音の余韻はループの頭に回り込ませるので、つなぎ目でプチッといわない。
    /// 旋律・音色は曲そのもののデータなのでここに置く。テンポ・音量は GameBalanceConfig の Audio。
    /// </summary>
    public static class BgmSynth
    {
        public const int Bars = 8;
        private const int BeatsPerBar = 4;
        private const int StepsPerBeat = 2; // 8 分音符
        private const float TargetPeak = 0.6f;

        // D の陽音階（D E G A B）を 2 オクターブ弱
        private static readonly int[] Scale = { 62, 64, 67, 69, 71, 74, 76, 79 };

        // 旋律：1 小節 8 個（8 分音符）。数字は Scale の番号、-1 は休み
        private static readonly int[] Melody =
        {
            5, -1, 4, 5, 6, -1, 5, 4,
            3, -1, 4, -1, 2, -1, -1, -1,
            3, 4, 5, -1, 4, 3, 2, -1,
            1, -1, 2, 3, 0, -1, -1, -1,
            5, -1, 6, 7, 6, -1, 5, -1,
            4, 5, 4, 3, 2, -1, -1, -1,
            3, -1, 2, -1, 3, 4, 5, -1,
            4, -1, 2, 1, 0, -1, -1, -1,
        };

        // 低音：1 小節 4 個（拍ごと）。MIDI の音番号
        private static readonly int[] Bass =
        {
            50, 57, 50, 57,
            43, 50, 43, 50,
            47, 54, 45, 52,
            45, 52, 50, 57,
            50, 57, 50, 57,
            43, 50, 47, 54,
            45, 52, 45, 52,
            45, 52, 50, 50,
        };

        public static float[] Generate(int sampleRate, float tempo)
        {
            if (sampleRate <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleRate), sampleRate, "sampleRate must be positive");
            }

            if (tempo <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(tempo), tempo, "tempo must be positive");
            }

            var beat = 60f / tempo;
            var step = beat / StepsPerBeat;
            var length = (int)Math.Round(Bars * BeatsPerBar * beat * sampleRate);
            var data = new float[length];
            var noise = 0x2545F491u;

            for (var i = 0; i < Melody.Length; i++)
            {
                if (Melody[i] >= 0)
                {
                    Koto(data, sampleRate, i * step, SoundSynth.Note(Scale[Melody[i]]), 0.35f);
                }
            }

            for (var i = 0; i < Bass.Length; i++)
            {
                Shamisen(data, sampleRate, i * beat, SoundSynth.Note(Bass[i]), 0.22f);
            }

            for (var bar = 0; bar < Bars; bar++)
            {
                for (var b = 0; b < BeatsPerBar; b++)
                {
                    var time = (bar * BeatsPerBar + b) * beat;
                    Don(data, sampleRate, time, b % 2 == 0 ? 0.6f : 0.35f);
                }

                // 小節の最後の 8 分で「カッ」
                Ka(data, sampleRate, (bar * BeatsPerBar + BeatsPerBar) * beat - step, 0.2f, ref noise);
            }

            var max = 0f;
            foreach (var x in data)
            {
                max = MathF.Max(max, MathF.Abs(x));
            }

            if (max > 0f)
            {
                var scale = TargetPeak / max;
                for (var i = 0; i < data.Length; i++)
                {
                    data[i] *= scale;
                }
            }

            return data;
        }

        /// <summary>琴：はじいたあと長めに響く。</summary>
        private static void Koto(float[] data, int rate, float start, float freq, float gain)
        {
            Add(data, rate, start, 1f, t =>
            {
                var w = 2f * MathF.PI * freq * t;
                var tone = MathF.Sin(w) + 0.5f * MathF.Sin(2f * w) * MathF.Exp(-8f * t) + 0.25f * MathF.Sin(3f * w) * MathF.Exp(-14f * t);
                return tone * Attack(t) * MathF.Exp(-t / 0.35f) * gain;
            });
        }

        /// <summary>三味線：倍音が多く、すぐ消える。</summary>
        private static void Shamisen(float[] data, int rate, float start, float freq, float gain)
        {
            Add(data, rate, start, 0.4f, t =>
            {
                var w = 2f * MathF.PI * freq * t;
                var tone = MathF.Sin(w) + 0.6f * MathF.Sin(2f * w) + 0.4f * MathF.Sin(3f * w) + 0.2f * MathF.Sin(5f * w);
                return tone * Attack(t) * MathF.Exp(-t / 0.12f) * gain;
            });
        }

        /// <summary>太鼓の「ドン」：低い音が下がりながら消える。</summary>
        private static void Don(float[] data, int rate, float start, float gain)
        {
            var phase = 0f;
            var previous = 0f;
            Add(data, rate, start, 0.35f, t =>
            {
                var freq = 55f + 45f * MathF.Exp(-t * 25f);
                phase += 2f * MathF.PI * freq * (t - previous);
                previous = t;
                return MathF.Sin(phase) * Attack(t) * MathF.Exp(-t / 0.12f) * gain;
            });
        }

        /// <summary>太鼓の縁の「カッ」：短い明るいノイズ。</summary>
        private static void Ka(float[] data, int rate, float start, float gain, ref uint noise)
        {
            var count = (int)(0.04f * rate);
            var offset = (int)Math.Round(start * rate);
            var previous = 0f;
            for (var n = 0; n < count; n++)
            {
                noise ^= noise << 13;
                noise ^= noise >> 17;
                noise ^= noise << 5;
                var white = noise / (float)uint.MaxValue * 2f - 1f;
                var bright = white - previous; // 高い音だけ残す
                previous = white;
                var t = n / (float)rate;
                data[Wrap(offset + n, data.Length)] += bright * Attack(t) * MathF.Exp(-t / 0.01f) * gain;
            }
        }

        /// <summary>音を足す。ループの最後をはみ出した余韻は頭に回り込ませる。</summary>
        private static void Add(float[] data, int rate, float start, float duration, Func<float, float> sample)
        {
            var offset = (int)Math.Round(start * rate);
            var count = (int)(duration * rate);
            for (var n = 0; n < count; n++)
            {
                data[Wrap(offset + n, data.Length)] += sample(n / (float)rate);
            }
        }

        private static float Attack(float t) => MathF.Min(1f, t * 300f);

        private static int Wrap(int index, int length) => ((index % length) + length) % length;
    }
}
