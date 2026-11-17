using System;
using System.Numerics;
using MochiHockey.Core;
using NUnit.Framework;

namespace MochiHockey.Tests.EditMode
{
    public class RiceSpawnScheduleTests
    {
        private const float Tolerance = 1e-4f;

        // 最小 5 個・最大 10 個・1.5 秒ごと。出現範囲は X: -10〜10、Z: -5〜5
        private static readonly RinkBounds SpawnArea = new RinkBounds(new Vector2(-10f, -5f), new Vector2(10f, 5f));

        private RiceSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = TestSettings.CreateRice();
        }

        [Test]
        public void Constructor_NullArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => new RiceSpawnSchedule(null, SpawnArea, new SequenceRandom(0f)));
            Assert.Throws<ArgumentNullException>(() => new RiceSpawnSchedule(_settings, SpawnArea, null));
        }

        [Test]
        public void Constructor_InvalidSpawnArea_Throws()
        {
            Assert.Throws<ArgumentException>(() => new RiceSpawnSchedule(_settings, default, new SequenceRandom(0f)));
        }

        // ---- 補充の数とタイミング（REQ-011）----

        [Test]
        public void Tick_BelowMin_RefillsToMinImmediately()
        {
            var schedule = Create();

            Assert.That(schedule.Tick(0f, 3), Is.EqualTo(2));
        }

        [Test]
        public void Tick_AtStartWithNone_SpawnsMinCount()
        {
            var schedule = Create();

            Assert.That(schedule.Tick(0f, 0), Is.EqualTo(5));
        }

        [Test]
        public void Tick_BetweenMinAndMax_SpawnsOneEveryInterval()
        {
            var schedule = Create();

            Assert.That(schedule.Tick(1.4f, 7), Is.EqualTo(0));
            Assert.That(schedule.Tick(0.1f, 7), Is.EqualTo(1));
            Assert.That(schedule.Tick(1.4f, 8), Is.EqualTo(0));
            Assert.That(schedule.Tick(0.1f, 8), Is.EqualTo(1));
        }

        [Test]
        public void Tick_ManySmallSteps_SpawnsAtInterval()
        {
            var schedule = Create();
            var spawned = 0;

            for (var i = 0; i < 90; i++)
            {
                spawned += schedule.Tick(1f / 60f, 7); // 合計 1.5 秒
            }

            Assert.That(spawned, Is.EqualTo(1));
        }

        [Test]
        public void Tick_LongStep_SpawnsSeveralButNotBeyondMax()
        {
            var schedule = Create();

            Assert.That(schedule.Tick(4.5f, 6), Is.EqualTo(3));
            Assert.That(schedule.Tick(30f, 9), Is.EqualTo(1));
        }

        [Test]
        public void Tick_AtMax_SpawnsNothing()
        {
            var schedule = Create();

            Assert.That(schedule.Tick(10f, 10), Is.EqualTo(0));
            Assert.That(schedule.Tick(10f, 12), Is.EqualTo(0));
        }

        [Test]
        public void Tick_TimerDoesNotRunWhileFull()
        {
            var schedule = Create();
            schedule.Tick(10f, 10);

            // 満杯から 1 個減ったら、そこから 1.5 秒待つ
            Assert.That(schedule.Tick(1.4f, 9), Is.EqualTo(0));
            Assert.That(schedule.Tick(0.1f, 9), Is.EqualTo(1));
        }

        [Test]
        public void Tick_UsesIntervalFromSettings()
        {
            _settings.RefillInterval = 0.5f;
            var schedule = Create();

            Assert.That(schedule.Tick(0.5f, 7), Is.EqualTo(1));
        }

        [Test]
        public void Tick_NegativeArguments_Throw()
        {
            var schedule = Create();

            Assert.Throws<ArgumentOutOfRangeException>(() => schedule.Tick(-0.1f, 7));
            Assert.Throws<ArgumentOutOfRangeException>(() => schedule.Tick(0.1f, -1));
        }

        // ---- 出現位置 ----

        [Test]
        public void PickPosition_MapsRandomValuesIntoSpawnArea()
        {
            var schedule = Create(new SequenceRandom(0.25f, 0.5f));

            var position = schedule.PickPosition(_ => false);

            Assert.That(position.X, Is.EqualTo(-5f).Within(Tolerance));
            Assert.That(position.Y, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void PickPosition_AlwaysInsideSpawnArea()
        {
            var schedule = Create(new SequenceRandom(0f, 0.999f, 0.5f, 0f, 0.999f, 0.999f));

            for (var i = 0; i < 3; i++)
            {
                Assert.That(SpawnArea.Contains(schedule.PickPosition(_ => false)), Is.True);
            }
        }

        [Test]
        public void PickPosition_SkipsExcludedPositions()
        {
            // 1 つ目 (-5, 0) はゴール内として除外、2 つ目 (5, 0) を返す
            var schedule = Create(new SequenceRandom(0.25f, 0.5f, 0.75f, 0.5f));

            var position = schedule.PickPosition(p => p.X < 0f);

            Assert.That(position.X, Is.EqualTo(5f).Within(Tolerance));
            Assert.That(position.Y, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void PickPosition_NullExclusion_AcceptsAnyPosition()
        {
            var schedule = Create(new SequenceRandom(0.25f, 0.5f));

            Assert.That(schedule.PickPosition(null).X, Is.EqualTo(-5f).Within(Tolerance));
        }

        [Test]
        public void PickPosition_EverythingExcluded_ThrowsInsteadOfLoopingForever()
        {
            var random = new SequenceRandom(0.5f);
            var schedule = Create(random);

            Assert.Throws<InvalidOperationException>(() => schedule.PickPosition(_ => true));
            Assert.That(random.CallCount, Is.LessThan(1000));
        }

        private RiceSpawnSchedule Create(IRandom random = null)
        {
            return new RiceSpawnSchedule(_settings, SpawnArea, random ?? new SequenceRandom(0.5f));
        }
    }
}
