using MochiHockey.Core;
using NUnit.Framework;

namespace MochiHockey.Tests.EditMode
{
    /// <summary>TASK-0031：fps の計測（一定時間ごとの平均と、いちばん遅かったフレーム）。</summary>
    public class FpsMeterTests
    {
        [Test]
        public void BeforeFirstInterval_HasNoReading()
        {
            var meter = new FpsMeter(0.5f);
            meter.Tick(1f / 60f);

            Assert.That(meter.HasReading, Is.False);
        }

        [Test]
        public void SteadyFrames_ReportsAverageFps()
        {
            var meter = new FpsMeter(0.5f);
            for (var i = 0; i < 31; i++)
            {
                meter.Tick(1f / 60f);
            }

            Assert.That(meter.HasReading, Is.True);
            Assert.That(meter.Fps, Is.EqualTo(60f).Within(0.5f));
            Assert.That(meter.AverageMilliseconds, Is.EqualTo(16.67f).Within(0.1f));
        }

        [Test]
        public void ReportsTheSlowestFrameInTheInterval()
        {
            var meter = new FpsMeter(0.5f);
            for (var i = 0; i < 20; i++)
            {
                meter.Tick(1f / 60f);
            }

            meter.Tick(0.05f); // カクついた 1 フレーム
            for (var i = 0; i < 20; i++)
            {
                meter.Tick(1f / 60f);
            }

            Assert.That(meter.WorstMilliseconds, Is.EqualTo(50f).Within(0.1f));
        }

        [Test]
        public void Tick_ReturnsTrueOnlyWhenANewReadingIsReady()
        {
            var meter = new FpsMeter(0.1f);
            var updates = 0;
            for (var i = 0; i < 60; i++)
            {
                if (meter.Tick(1f / 60f))
                {
                    updates++;
                }
            }

            // 1 秒で 0.1 秒ごと → 9〜10 回
            Assert.That(updates, Is.InRange(9, 10));
        }

        [Test]
        public void NewInterval_StartsFresh()
        {
            var meter = new FpsMeter(0.5f);
            meter.Tick(0.6f); // 遅い区間（これだけで 1 区間）
            Assert.That(meter.Fps, Is.LessThan(2f));

            for (var i = 0; i < 31; i++)
            {
                meter.Tick(1f / 60f);
            }

            Assert.That(meter.Fps, Is.EqualTo(60f).Within(0.5f));
            Assert.That(meter.WorstMilliseconds, Is.EqualTo(16.67f).Within(0.1f));
        }

        [Test]
        public void IgnoresZeroAndNegativeFrames()
        {
            // ポーズ中（timeScale 0 でも unscaled なら来ないが念のため）や、おかしな値は数えない
            var meter = new FpsMeter(0.5f);
            meter.Tick(0f);
            meter.Tick(-1f);

            Assert.That(meter.HasReading, Is.False);
        }
    }
}
