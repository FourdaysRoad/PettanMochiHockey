using System;

namespace MochiHockey.Core
{
    /// <summary>
    /// 効果音をコードで合成する（TASK-0029）。音のファイルを使わないので、ライセンスの確認が要らず、ビルドも増えない。
    /// サイン波・ノイズ・音程の変化・減衰を組み合わせて、まんがっぽい音にする。
    /// 同じ引数なら毎回同じ音になる（ノイズも決まった種から作る）。
    /// 音の形（周波数・長さ）は音そのもののデータなのでここに置く。音量は GameBalanceConfig の Audio。
    /// </summary>
    public static class SoundSynth
    {
        // どの音も、いちばん大きいところをこの大きさにそろえる
        private const float TargetPeak = 0.8f;

        public static float[] Generate(SoundId id, int sampleRate)
        {
            if (sampleRate <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleRate), sampleRate, "sampleRate must be positive");
            }

            var b = new Buffer(sampleRate, Length(id), Seed(id));
            switch (id)
            {
                case SoundId.Swing:
                    // ひゅっ：こもったノイズが大きくなって消える
                    b.Noise(0f, 0.18f, 0.9f, Env.Bell, 0.08f);
                    break;
                case SoundId.Pettan:
                    // ペッタン：はじける高い音 ＋ 低い「ドン」＋ 少し響く「タン」
                    b.Noise(0f, 0.04f, 1f, Env.Hit, 0.9f);
                    b.Sweep(0f, 0.25f, 180f, 70f, 1f, Env.Hit);
                    b.Sweep(0.01f, 0.12f, 340f, 300f, 0.35f, Env.Hit);
                    break;
                case SoundId.LaunchBounce:
                    // ぼよん：揺れながら下がる音
                    b.Wobble(0f, 0.22f, 240f, 120f, 18f, 0.25f, 1f);
                    b.Sweep(0f, 0.08f, 120f, 60f, 0.6f, Env.Hit);
                    break;
                case SoundId.Stuck:
                    // べちゃっ：こもったノイズ ＋ 低い音
                    b.Noise(0f, 0.3f, 1f, Env.Hit, 0.15f);
                    b.Sweep(0f, 0.2f, 110f, 60f, 0.9f, Env.Hit);
                    break;
                case SoundId.Pon:
                    // ポンッ！：短く上がる音
                    b.Sweep(0f, 0.16f, 480f, 1150f, 1f, Env.Pluck);
                    b.Sweep(0f, 0.16f, 960f, 2300f, 0.25f, Env.Pluck);
                    break;
                case SoundId.Dash:
                    // しゃっ：明るいノイズ
                    b.Noise(0f, 0.16f, 1f, Env.Bell, 0.7f);
                    break;
                case SoundId.Shot:
                    // ぱこん：短いノイズ ＋ 下がる音
                    b.Noise(0f, 0.025f, 0.8f, Env.Hit, 0.8f);
                    b.Sweep(0f, 0.12f, 420f, 180f, 1f, Env.Hit);
                    break;
                case SoundId.RiceGet:
                    // ぴっ：高い 2 つの音
                    b.Sweep(0f, 0.05f, 1320f, 1320f, 1f, Env.Pluck);
                    b.Sweep(0.035f, 0.06f, 1760f, 1760f, 0.8f, Env.Pluck);
                    break;
                case SoundId.LevelUp:
                    // 上がっていく 3 つの音（和風の音階）
                    b.Bell(0f, 0.25f, Note(74), 1f);
                    b.Bell(0.09f, 0.25f, Note(79), 1f);
                    b.Bell(0.18f, 0.3f, Note(83), 1f);
                    break;
                case SoundId.BallBounce:
                    // ぽふ：やわらかい低い音
                    b.Sweep(0f, 0.1f, 200f, 110f, 1f, Env.Hit);
                    b.Noise(0f, 0.05f, 0.3f, Env.Hit, 0.1f);
                    break;
                case SoundId.Goal:
                    // 鐘のような音が駆け上がる
                    var notes = new[] { 74, 76, 79, 81, 86 };
                    for (var i = 0; i < notes.Length; i++)
                    {
                        b.Bell(i * 0.08f, i == notes.Length - 1 ? 0.6f : 0.35f, Note(notes[i]), 1f);
                    }

                    break;
                case SoundId.Count:
                    // ピッ：高めの短い音
                    b.Sweep(0f, 0.12f, 880f, 880f, 1f, Env.Pluck);
                    b.Sweep(0f, 0.12f, 1760f, 1760f, 0.3f, Env.Pluck);
                    break;
                case SoundId.Whistle:
                    b.Whistle(0f, 0.35f, 2300f);
                    break;
                case SoundId.FinalWhistle:
                    b.Whistle(0f, 0.25f, 2300f);
                    b.Whistle(0.3f, 0.8f, 2300f);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(id), id, null);
            }

            return b.Finish(TargetPeak);
        }

        private static float Length(SoundId id)
        {
            switch (id)
            {
                case SoundId.Swing: return 0.2f;
                case SoundId.Pettan: return 0.3f;
                case SoundId.LaunchBounce: return 0.25f;
                case SoundId.Stuck: return 0.32f;
                case SoundId.Pon: return 0.18f;
                case SoundId.Dash: return 0.18f;
                case SoundId.Shot: return 0.14f;
                case SoundId.RiceGet: return 0.1f;
                case SoundId.LevelUp: return 0.5f;
                case SoundId.BallBounce: return 0.12f;
                case SoundId.Goal: return 1f;
                case SoundId.Count: return 0.14f;
                case SoundId.Whistle: return 0.38f;
                case SoundId.FinalWhistle: return 1.12f;
                default: return 0.2f;
            }
        }

        private static uint Seed(SoundId id) => 0x9E3779B9u ^ (uint)((int)id * 7919 + 1);

        /// <summary>MIDI の音番号から周波数（Hz）。69 = A4 = 440 Hz。</summary>
        internal static float Note(int midi) => 440f * MathF.Pow(2f, (midi - 69) / 12f);

        internal enum Env
        {
            /// <summary>すぐ立ち上がって速く消える（打つ音）。</summary>
            Hit,

            /// <summary>すぐ立ち上がって少し残る（はじく音）。</summary>
            Pluck,

            /// <summary>ふくらんでしぼむ（風の音）。</summary>
            Bell,
        }

        /// <summary>音を足し合わせる入れ物。</summary>
        internal sealed class Buffer
        {
            private readonly float[] _data;
            private readonly int _rate;
            private uint _noise;

            public Buffer(int rate, float seconds, uint seed)
            {
                _rate = rate;
                _data = new float[Math.Max(1, (int)(rate * seconds))];
                _noise = seed == 0 ? 1u : seed;
            }

            /// <summary>音程が from から to へ変わる音。</summary>
            public void Sweep(float start, float duration, float from, float to, float gain, Env env)
            {
                var phase = 0f;
                Each(start, duration, (i, t) =>
                {
                    var freq = from * MathF.Pow(to / from, t);
                    phase += 2f * MathF.PI * freq / _rate;
                    _data[i] += MathF.Sin(phase) * gain * Envelope(env, t);
                });
            }

            /// <summary>揺れながら変わる音（ぼよん）。</summary>
            public void Wobble(float start, float duration, float from, float to, float rate, float depth, float gain)
            {
                var phase = 0f;
                Each(start, duration, (i, t) =>
                {
                    var seconds = t * duration;
                    var freq = (from + (to - from) * t) * (1f + depth * (1f - t) * MathF.Sin(2f * MathF.PI * rate * seconds));
                    phase += 2f * MathF.PI * freq / _rate;
                    _data[i] += MathF.Sin(phase) * gain * Envelope(Env.Pluck, t);
                });
            }

            /// <summary>鐘のような音（基音と、少しずれた倍音）。</summary>
            public void Bell(float start, float duration, float freq, float gain)
            {
                Each(start, duration, (i, t) =>
                {
                    var seconds = t * duration;
                    var w = 2f * MathF.PI * freq * seconds;
                    var tone = MathF.Sin(w) + 0.4f * MathF.Sin(w * 2.76f) * MathF.Exp(-6f * t) + 0.2f * MathF.Sin(w * 5.4f) * MathF.Exp(-12f * t);
                    _data[i] += tone * gain * Envelope(Env.Pluck, t);
                });
            }

            /// <summary>笛：高い音を細かく震わせる。</summary>
            public void Whistle(float start, float duration, float freq)
            {
                var phase = 0f;
                Each(start, duration, (i, t) =>
                {
                    var seconds = t * duration;
                    var trill = 1f + 0.03f * MathF.Sin(2f * MathF.PI * 28f * seconds);
                    phase += 2f * MathF.PI * freq * trill / _rate;
                    var env = MathF.Min(1f, t * 20f) * MathF.Min(1f, (1f - t) * 10f);
                    _data[i] += (MathF.Sin(phase) + 0.15f * NextNoise()) * env;
                });
            }

            /// <summary>ノイズ。brightness が小さいほどこもった音（0〜1）。</summary>
            public void Noise(float start, float duration, float gain, Env env, float brightness)
            {
                var low = 0f;
                var alpha = Math.Clamp(brightness, 0.01f, 1f);
                Each(start, duration, (i, t) =>
                {
                    low += alpha * (NextNoise() - low);
                    _data[i] += low * gain * Envelope(env, t) / MathF.Sqrt(alpha);
                });
            }

            /// <summary>大きさをそろえ、最初と最後をなめらかにする（プチッという音を防ぐ）。</summary>
            public float[] Finish(float peak)
            {
                var max = 0f;
                foreach (var x in _data)
                {
                    max = MathF.Max(max, MathF.Abs(x));
                }

                var scale = max > 0f ? peak / max : 0f;
                var fadeIn = Math.Max(1, _rate / 1000);
                var fadeOut = Math.Max(Math.Max(1, _rate / 50), _data.Length / 4);
                for (var i = 0; i < _data.Length; i++)
                {
                    var gain = scale;
                    if (i < fadeIn)
                    {
                        gain *= i / (float)fadeIn;
                    }

                    var fromEnd = _data.Length - 1 - i;
                    if (fromEnd < fadeOut)
                    {
                        var r = fromEnd / (float)fadeOut;
                        gain *= r * r * r;
                    }

                    _data[i] *= gain;
                }

                return _data;
            }

            private void Each(float start, float duration, Action<int, float> write)
            {
                var from = (int)(start * _rate);
                var count = (int)(duration * _rate);
                for (var n = 0; n < count; n++)
                {
                    var i = from + n;
                    if (i < 0 || i >= _data.Length)
                    {
                        continue;
                    }

                    write(i, n / (float)count);
                }
            }

            private float NextNoise()
            {
                // xorshift（毎回同じ並びになる）
                _noise ^= _noise << 13;
                _noise ^= _noise >> 17;
                _noise ^= _noise << 5;
                return _noise / (float)uint.MaxValue * 2f - 1f;
            }

            private static float Envelope(Env env, float t)
            {
                switch (env)
                {
                    case Env.Hit:
                        return MathF.Min(1f, t * 200f) * MathF.Exp(-6f * t);
                    case Env.Pluck:
                        return MathF.Min(1f, t * 100f) * MathF.Exp(-3.5f * t);
                    default:
                        return MathF.Sin(MathF.PI * t);
                }
            }
        }
    }
}
