using System;
using System.Collections.Generic;
using System.Numerics;
using MochiHockey.Core;
using NUnit.Framework;

namespace MochiHockey.Tests.EditMode
{
    /// <summary>TASK-0011：吹っ飛びの軌道・張り付き・ずり落ち・延長と上限。</summary>
    public class PettanTrajectoryTests
    {
        private const float Tolerance = 1e-3f;

        // HitStop 0.08s、標準 0.6s、上限 1.2s、速度 12 m/s、バウンド高さ 1m、
        // ずり落ち開始 1.5s・0.5 m/s、復帰 2.6s、無敵終了 3.6s。リンクは X: -12〜12、Z: -7〜7
        private PettanSettings _settings;
        private PettanStateMachine _machine;
        private List<PettanState> _stateChanges;
        private int _recoveredCount;

        private static readonly Vector2 Right = new Vector2(1f, 0f);

        [SetUp]
        public void SetUp()
        {
            _settings = TestSettings.CreatePettan();
            CreateMachine(TestSettings.CreateRink());
        }

        // ---- 床面の直進（REQ-033）----

        [Test]
        public void Launched_MovesStraightAtLaunchSpeed()
        {
            _machine.Begin(new Vector2(-5f, 2f), Right);

            _machine.Tick(0.08f + 0.25f);

            AssertVector(_machine.Position, -5f + 12f * 0.25f, 2f);
        }

        [Test]
        public void HitStop_DoesNotMove()
        {
            _machine.Begin(new Vector2(-5f, 2f), Right);

            _machine.Tick(0.05f);

            AssertVector(_machine.Position, -5f, 2f);
            Assert.That(_machine.Height, Is.EqualTo(0f));
        }

        [Test]
        public void Launched_DirectionLengthDoesNotChangeSpeed()
        {
            _machine.Begin(Vector2.Zero, new Vector2(0f, -3f));

            _machine.Tick(0.08f + 0.25f);

            AssertVector(_machine.Position, 0f, -3f);
        }

        [Test]
        public void ReachingWall_StuckAtHitPointWithNormal()
        {
            // 原点から +X、20 m/s、壁 x = 10 → 0.08 + 0.5 秒で Stuck
            _settings.LaunchSpeed = 20f;
            CreateMachine(new RinkBounds(new Vector2(-10f, -10f), new Vector2(10f, 10f)));
            _machine.Begin(Vector2.Zero, Right);

            _machine.Tick(0.57f);
            Assert.That(_machine.State, Is.EqualTo(PettanState.Launched));

            _machine.Tick(0.02f);
            Assert.That(_machine.State, Is.EqualTo(PettanState.Stuck));
            AssertVector(_machine.Position, 10f, 0f);
            AssertVector(_machine.WallNormal, -1f, 0f);
        }

        // ---- 高さ：1 回バウンド ----

        [TestCase(0.08f, 0f)]
        [TestCase(0.21f, 1f)]   // 前半の山（0.08 + 0.13）
        [TestCase(0.34f, 0f)]   // 床でバウンド（0.08 + 0.26）
        [TestCase(0.47f, 1f)]   // 後半の山（0.08 + 0.39）
        [TestCase(0.6f, 0f)]    // 着地
        [TestCase(0.9f, 0f)]    // その後は床を滑る
        public void Launched_HeightBouncesOnceOnFloor(float seconds, float expectedHeight)
        {
            // 原点から +X なら壁まで 1 秒かかるので、0.9 秒時点でもまだ Launched
            _machine.Begin(Vector2.Zero, Right);

            _machine.Tick(seconds);

            Assert.That(_machine.State, Is.EqualTo(seconds < 0.08f ? PettanState.HitStop : PettanState.Launched));
            Assert.That(_machine.Height, Is.EqualTo(expectedHeight).Within(Tolerance));
        }

        [Test]
        public void Launched_HeightNeverGoesBelowFloor()
        {
            _machine.Begin(Vector2.Zero, Right);

            for (var i = 0; i < 60; i++)
            {
                _machine.Tick(1f / 60f);
                Assert.That(_machine.Height, Is.GreaterThanOrEqualTo(0f));
            }
        }

        // ---- 張り付き・ずり落ち（REQ-034）----

        [Test]
        public void Stuck_KeepsHeightAtWallArrival()
        {
            // 壁まで 1.2 m → 0.08 + 0.1 秒で到達。前半の山の途中（p = 0.1 / 0.26）
            _machine.Begin(new Vector2(10.8f, 0f), Right);

            _machine.Tick(0.3f);

            var p = 0.1f / 0.26f;
            Assert.That(_machine.State, Is.EqualTo(PettanState.Stuck));
            Assert.That(_machine.Height, Is.EqualTo(4f * p * (1f - p)).Within(Tolerance));
        }

        [Test]
        public void Stuck_BeforeSlideDownStart_DoesNotSlide()
        {
            _machine.Begin(new Vector2(10.8f, 0f), Right);
            _machine.Tick(0.3f);
            var arrivalHeight = _machine.Height;

            _machine.Tick(1.1f); // 1.4 秒

            Assert.That(_machine.IsSlidingDown, Is.False);
            Assert.That(_machine.Height, Is.EqualTo(arrivalHeight).Within(Tolerance));
        }

        [Test]
        public void Stuck_AfterSlideDownStart_SlidesDownAlongWall()
        {
            _machine.Begin(new Vector2(10.8f, 0f), Right);
            _machine.Tick(0.3f);
            var arrivalHeight = _machine.Height;
            var arrivalPosition = _machine.Position;

            _machine.Tick(1.7f); // 2.0 秒

            Assert.That(_machine.State, Is.EqualTo(PettanState.Stuck));
            Assert.That(_machine.IsSlidingDown, Is.True);
            Assert.That(_machine.Height, Is.EqualTo(arrivalHeight - 0.5f * 0.5f).Within(Tolerance));
            Assert.That(_machine.Position, Is.EqualTo(arrivalPosition));
        }

        [Test]
        public void Recover_ResetsHeightAndStopsSliding()
        {
            _machine.Begin(new Vector2(10.8f, 0f), Right);

            _machine.Tick(2.7f);

            Assert.That(_machine.State, Is.EqualTo(PettanState.Invincible));
            Assert.That(_machine.IsSlidingDown, Is.False);
            Assert.That(_machine.Height, Is.EqualTo(0f));
        }

        // ---- 延長（REQ-033）----

        [Test]
        public void Launched_ContinuesPastNominalDurationUntilWall()
        {
            // 原点から +X：壁まで 12 m → 0.08 + 1.0 秒で到達（上限 1.2 秒より前）
            _machine.Begin(Vector2.Zero, Right);

            _machine.Tick(0.7f);
            Assert.That(_machine.State, Is.EqualTo(PettanState.Launched));

            _machine.Tick(0.4f); // 1.1 秒
            Assert.That(_machine.State, Is.EqualTo(PettanState.Stuck));
            AssertVector(_machine.Position, 12f, 0f);
        }

        // ---- 上限 ----

        [Test]
        public void Launched_PastMaxDuration_RecoversInPlace()
        {
            // 左の壁から +X：壁まで 24 m → 2.08 秒かかるので、1.2 秒の上限で打ち切る
            _machine.Begin(new Vector2(-12f, 3f), Right);

            _machine.Tick(1.19f);
            Assert.That(_machine.State, Is.EqualTo(PettanState.Launched));
            Assert.That(_recoveredCount, Is.EqualTo(0));

            _machine.Tick(0.02f);
            Assert.That(_machine.State, Is.EqualTo(PettanState.Invincible));
            Assert.That(_recoveredCount, Is.EqualTo(1));
            AssertVector(_machine.Position, -12f + 12f * (1.2f - 0.08f), 3f);
            Assert.That(_machine.Height, Is.EqualTo(0f));
            Assert.That(_machine.WallNormal, Is.EqualTo(Vector2.Zero));
        }

        [Test]
        public void Launched_PastMaxDuration_SkipsStuckAndKeepsInvincibleLength()
        {
            _machine.Begin(new Vector2(-12f, 3f), Right);

            _machine.Tick(2.19f);
            Assert.That(_machine.State, Is.EqualTo(PettanState.Invincible));

            _machine.Tick(0.02f); // 復帰（1.2 秒）から 1.0 秒
            Assert.That(_machine.State, Is.EqualTo(PettanState.Normal));
            Assert.That(_stateChanges, Is.EqualTo(new[]
            {
                PettanState.HitStop,
                PettanState.Launched,
                PettanState.Invincible,
                PettanState.Normal,
            }));
        }

        [Test]
        public void Launched_WithZeroSpeed_RecoversAtMaxDuration()
        {
            _settings.LaunchSpeed = 0f;
            CreateMachine(TestSettings.CreateRink());
            _machine.Begin(new Vector2(1f, 1f), Right);

            _machine.Tick(1.21f);

            Assert.That(_machine.State, Is.EqualTo(PettanState.Invincible));
            AssertVector(_machine.Position, 1f, 1f);
        }

        private void CreateMachine(RinkBounds rink)
        {
            _machine = new PettanStateMachine(_settings, rink);
            _stateChanges = new List<PettanState>();
            _recoveredCount = 0;
            _machine.StateChanged += _stateChanges.Add;
            _machine.Recovered += () => _recoveredCount++;
        }

        private static void AssertVector(Vector2 actual, float x, float y)
        {
            Assert.That(actual.X, Is.EqualTo(x).Within(Tolerance), "X");
            Assert.That(actual.Y, Is.EqualTo(y).Within(Tolerance), "Y");
        }
    }
}
