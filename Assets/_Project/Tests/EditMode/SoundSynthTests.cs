using System;
using System.Linq;
using MochiHockey.Core;
using NUnit.Framework;

namespace MochiHockey.Tests.EditMode
{
    /// <summary>TASK-0029：コードで合成する効果音と BGM。</summary>
    public class SoundSynthTests
    {
        private const int Rate = 22050;

        private static readonly SoundId[] AllSounds = (SoundId[])Enum.GetValues(typeof(SoundId));

        // ---- 効果音 ----

        [TestCaseSource(nameof(AllSounds))]
        public void EverySound_HasAShortAudibleClip(SoundId id)
        {
            var samples = SoundSynth.Generate(id, Rate);

            var seconds = samples.Length / (float)Rate;
            Assert.That(seconds, Is.InRange(0.05f, 1.5f));
            Assert.That(Peak(samples), Is.InRange(0.3f, 1f), "聞こえる大きさで、割れない");
        }

        [TestCaseSource(nameof(AllSounds))]
        public void EverySound_StartsAndEndsSilentToAvoidClicks(SoundId id)
        {
            var samples = SoundSynth.Generate(id, Rate);

            Assert.That(Math.Abs(samples[0]), Is.LessThan(0.01f));
            Assert.That(Math.Abs(samples[samples.Length - 1]), Is.LessThan(0.01f));
            var tail = samples.Skip(samples.Length - Rate / 200).ToArray(); // 最後の 5 ms
            Assert.That(Peak(tail), Is.LessThan(0.05f));
        }

        [TestCaseSource(nameof(AllSounds))]
        public void EverySound_IsTheSameEveryTime(SoundId id)
        {
            Assert.That(SoundSynth.Generate(id, Rate), Is.EqualTo(SoundSynth.Generate(id, Rate)));
        }

        [Test]
        public void Sounds_AreDifferentFromEachOther()
        {
            var clips = AllSounds.Select(id => SoundSynth.Generate(id, Rate)).ToArray();
            for (var i = 0; i < clips.Length; i++)
            {
                for (var j = i + 1; j < clips.Length; j++)
                {
                    Assert.That(clips[i], Is.Not.EqualTo(clips[j]), $"{AllSounds[i]} と {AllSounds[j]}");
                }
            }
        }

        [Test]
        public void Pon_RisesInPitch()
        {
            // 「ポンッ！」は音が上がる。前半より後半のほうが波の数（ゼロをまたぐ回数）が多い
            var samples = SoundSynth.Generate(SoundId.Pon, Rate);
            var half = samples.Length / 2;

            Assert.That(ZeroCrossings(samples, half, half * 2), Is.GreaterThan(ZeroCrossings(samples, 0, half)));
        }

        [Test]
        public void Generate_WorksAtOtherSampleRates()
        {
            var low = SoundSynth.Generate(SoundId.Pettan, 11025);
            var high = SoundSynth.Generate(SoundId.Pettan, 44100);

            Assert.That(high.Length, Is.EqualTo(low.Length * 4).Within(4));
        }

        [Test]
        public void Generate_RejectsBadSampleRate()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => SoundSynth.Generate(SoundId.Pon, 0));
        }

        // ---- BGM ----

        [Test]
        public void Bgm_LengthIsWholeBarsAtTempo()
        {
            var samples = BgmSynth.Generate(Rate, 120f);

            // 8 小節 × 4 拍 × 0.5 秒 = 16 秒
            Assert.That(samples.Length, Is.EqualTo(BgmSynth.Bars * 4 * Rate / 2));
        }

        [Test]
        public void Bgm_FasterTempo_IsShorter()
        {
            Assert.That(BgmSynth.Generate(Rate, 140f).Length, Is.LessThan(BgmSynth.Generate(Rate, 120f).Length));
        }

        [Test]
        public void Bgm_IsAudibleButQuieterThanEffects()
        {
            var peak = Peak(BgmSynth.Generate(Rate, 120f));

            Assert.That(peak, Is.InRange(0.3f, 0.8f));
        }

        [Test]
        public void Bgm_LoopsWithoutAClick()
        {
            // 最後と最初がつながる（音の余韻は最初に回り込ませる）
            var samples = BgmSynth.Generate(Rate, 120f);

            Assert.That(Math.Abs(samples[samples.Length - 1] - samples[0]), Is.LessThan(0.05f));
        }

        [Test]
        public void Bgm_IsNotSilentAnywhereLong()
        {
            // 1 拍ごとに見て、無音の拍がない
            var samples = BgmSynth.Generate(Rate, 120f);
            var beat = Rate / 2;
            for (var start = 0; start + beat <= samples.Length; start += beat)
            {
                Assert.That(Peak(samples.Skip(start).Take(beat).ToArray()), Is.GreaterThan(0.05f), $"{start / beat} 拍目");
            }
        }

        [Test]
        public void Bgm_IsTheSameEveryTime()
        {
            Assert.That(BgmSynth.Generate(Rate, 120f), Is.EqualTo(BgmSynth.Generate(Rate, 120f)));
        }

        [Test]
        public void Bgm_RejectsBadArguments()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BgmSynth.Generate(0, 120f));
            Assert.Throws<ArgumentOutOfRangeException>(() => BgmSynth.Generate(Rate, 0f));
        }

        private static float Peak(float[] samples) => samples.Length == 0 ? 0f : samples.Max(Math.Abs);

        private static int ZeroCrossings(float[] samples, int from, int to)
        {
            var count = 0;
            for (var i = from + 1; i < to; i++)
            {
                if ((samples[i - 1] < 0f) != (samples[i] < 0f))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
