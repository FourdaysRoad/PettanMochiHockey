using System;
using System.Collections.Generic;
using System.Numerics;
using MochiHockey.Core;
using NUnit.Framework;

namespace MochiHockey.Tests.EditMode
{
    /// <summary>
    /// 吹っ飛びの軌道（TASK-0011、変更：床ではねずに水平に直進し、壁で跳ね返ってから次の壁に貼り付く）・
    /// 張り付き・ずり落ち・延長と上限。
    /// </summary>
    public class PettanTrajectoryTests
    {
        private const float Tolerance = 1e-3f;

        // HitStop 0.08s、上限 1.2s、速度 12 m/s、跳ね返り 1 回、
        // ずり落ち開始 1.5s・0.5 m/s、復帰 2.6s、無敵終了 3.6s。リンクは X: -12〜12、Z: -7〜7
        private PettanSettings _settings;
        private PettanStateMachine _machine;
        private List<PettanState> _stateChanges;
        private int _recoveredCount;
        private int _bouncedCount;

        private static readonly Vector2 Right = new Vector2(1f, 0f);
        private static readonly RinkBounds SmallRink = new RinkBounds(new Vector2(-5f, -5f), new Vector2(5f, 5f));

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
        }

        [Test]
        public void Launched_DirectionLengthDoesNotChangeSpeed()
        {
            _machine.Begin(Vector2.Zero, new Vector2(0f, -3f));

            _machine.Tick(0.08f + 0.25f);

            AssertVector(_machine.Position, 0f, -3f);
        }

        [Test]
        public void Launched_FliesLevelWithoutLeavingFloorHeight()
        {
            _machine.Begin(Vector2.Zero, Right);

            for (var i = 0; i < 60; i++)
            {
                _machine.Tick(1f / 60f);
                Assert.That(_machine.Height, Is.EqualTo(0f));
            }
        }

        // ---- 壁で跳ね返る ----

        [Test]
        public void FirstWall_BouncesBackInsteadOfSticking()
        {
            // 右の壁まで 3 m（0.08 + 0.25 秒）。そこから 0.1 秒ぶん（1.2 m）戻る
            _machine.Begin(new Vector2(9f, 0f), Right);

            _machine.Tick(0.43f);

            Assert.That(_machine.State, Is.EqualTo(PettanState.Launched));
            AssertVector(_machine.Position, 10.8f, 0f);
            Assert.That(_bouncedCount, Is.EqualTo(1));
        }

        [Test]
        public void SecondWall_SticksThereWithItsNormal()
        {
            // 小さいリンクで原点から +X、20 m/s：右の壁まで 5 m、跳ね返って左の壁まで 10 m → 0.08 + 0.75 秒
            _settings.LaunchSpeed = 20f;
            CreateMachine(SmallRink);
            _machine.Begin(Vector2.Zero, Right);

            _machine.Tick(0.82f);
            Assert.That(_machine.State, Is.EqualTo(PettanState.Launched));

            _machine.Tick(0.02f);
            Assert.That(_machine.State, Is.EqualTo(PettanState.Stuck));
            AssertVector(_machine.Position, -5f, 0f);
            AssertVector(_machine.WallNormal, 1f, 0f);
            Assert.That(_bouncedCount, Is.EqualTo(1));
        }

        [Test]
        public void Diagonal_ReflectsOffWallLikeABall()
        {
            // (1, 1) 方向：上の壁 (7, 7) で跳ね返り (1, -1) へ → 右の壁 (12, 2) に貼り付く。距離 12√2、20 m/s
            _settings.LaunchSpeed = 20f;
            CreateMachine(TestSettings.CreateRink());
            _machine.Begin(Vector2.Zero, new Vector2(1f, 1f));

            _machine.Tick(1f);

            Assert.That(_machine.State, Is.EqualTo(PettanState.Stuck));
            AssertVector(_machine.Position, 12f, 2f);
            AssertVector(_machine.WallNormal, -1f, 0f);
        }

        [Test]
        public void NoBounces_SticksToFirstWall()
        {
            _settings.LaunchWallBounces = 0;
            CreateMachine(TestSettings.CreateRink());
            _machine.Begin(Vector2.Zero, Right);

            _machine.Tick(1.1f); // 0.08 + 12 / 12

            Assert.That(_machine.State, Is.EqualTo(PettanState.Stuck));
            AssertVector(_machine.Position, 12f, 0f);
            Assert.That(_bouncedCount, Is.EqualTo(0));
        }

        [Test]
        public void TwoBounces_SticksToThirdWall()
        {
            // 5 m → 10 m → 10 m で、3 回目の壁（右）に貼り付く
            _settings.LaunchWallBounces = 2;
            _settings.LaunchSpeed = 20f;
            _settings.LaunchMaxDuration = 2f;
            CreateMachine(SmallRink);
            _machine.Begin(Vector2.Zero, Right);

            _machine.Tick(1.4f); // 0.08 + 25 / 20 = 1.33

            Assert.That(_machine.State, Is.EqualTo(PettanState.Stuck));
            AssertVector(_machine.Position, 5f, 0f);
            AssertVector(_machine.WallNormal, -1f, 0f);
            Assert.That(_bouncedCount, Is.EqualTo(2));
        }

        [Test]
        public void Bounced_IsRaisedForEveryBounceEvenInOneLargeStep()
        {
            _settings.LaunchWallBounces = 2;
            _settings.LaunchSpeed = 20f;
            _settings.LaunchMaxDuration = 2f;
            CreateMachine(SmallRink);
            _machine.Begin(Vector2.Zero, Right);

            _machine.Tick(10f);

            Assert.That(_bouncedCount, Is.EqualTo(2));
        }

        // ---- 張り付き・ずり落ち（REQ-034）----

        [Test]
        public void Stuck_BeforeSlideDownStart_StaysAtFloorHeight()
        {
            StickQuickly();

            _machine.Tick(1.4f - _machine.Elapsed);

            Assert.That(_machine.State, Is.EqualTo(PettanState.Stuck));
            Assert.That(_machine.IsSlidingDown, Is.False);
            Assert.That(_machine.Height, Is.EqualTo(0f));
        }

        [Test]
        public void Stuck_AfterSlideDownStart_SlidesDownAlongWall()
        {
            StickQuickly();
            var stuckPosition = _machine.Position;

            _machine.Tick(2.0f - _machine.Elapsed);

            Assert.That(_machine.State, Is.EqualTo(PettanState.Stuck));
            Assert.That(_machine.IsSlidingDown, Is.True);
            Assert.That(_machine.Height, Is.EqualTo(-0.5f * 0.5f).Within(Tolerance));
            Assert.That(_machine.Position, Is.EqualTo(stuckPosition));
        }

        [Test]
        public void Recover_ResetsHeightAndStopsSliding()
        {
            StickQuickly();

            _machine.Tick(2.7f - _machine.Elapsed);

            Assert.That(_machine.State, Is.EqualTo(PettanState.Invincible));
            Assert.That(_machine.IsSlidingDown, Is.False);
            Assert.That(_machine.Height, Is.EqualTo(0f));
        }

        // ---- 延長と上限（REQ-033）----

        [Test]
        public void Launched_PastMaxDuration_RecoversInPlace()
        {
            // 原点から +X：右の壁まで 12 m、跳ね返って左の壁まで 24 m → 3.08 秒かかるので、1.2 秒の上限で打ち切る
            _machine.Begin(Vector2.Zero, Right);

            _machine.Tick(1.19f);
            Assert.That(_machine.State, Is.EqualTo(PettanState.Launched));
            Assert.That(_recoveredCount, Is.EqualTo(0));

            _machine.Tick(0.02f);
            Assert.That(_machine.State, Is.EqualTo(PettanState.Invincible));
            Assert.That(_recoveredCount, Is.EqualTo(1));
            Assert.That(_bouncedCount, Is.EqualTo(1));
            AssertVector(_machine.Position, 12f - (12f * (1.2f - 0.08f) - 12f), 0f);
            Assert.That(_machine.WallNormal, Is.EqualTo(Vector2.Zero));
        }

        [Test]
        public void Launched_PastMaxDuration_SkipsStuckAndKeepsInvincibleLength()
        {
            _machine.Begin(Vector2.Zero, Right);

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
            Assert.That(_bouncedCount, Is.EqualTo(0));
        }

        [Test]
        public void Begin_AgainAfterNormal_ResetsBounceCount()
        {
            _settings.LaunchSpeed = 20f;
            CreateMachine(SmallRink);
            _machine.Begin(Vector2.Zero, Right);
            _machine.Tick(10f);

            _machine.Begin(Vector2.Zero, Right);
            _machine.Tick(10f);

            Assert.That(_bouncedCount, Is.EqualTo(2));
            Assert.That(_stateChanges, Has.Exactly(2).EqualTo(PettanState.Stuck));
        }

        /// <summary>小さいリンク・20 m/s・跳ね返りなしで、0.33 秒に右の壁へ貼り付かせる。</summary>
        private void StickQuickly()
        {
            _settings.LaunchSpeed = 20f;
            _settings.LaunchWallBounces = 0;
            CreateMachine(SmallRink);
            _machine.Begin(Vector2.Zero, Right);
            _machine.Tick(0.4f);
            Assert.That(_machine.State, Is.EqualTo(PettanState.Stuck));
        }

        private void CreateMachine(RinkBounds rink)
        {
            _machine = new PettanStateMachine(_settings, rink);
            _stateChanges = new List<PettanState>();
            _recoveredCount = 0;
            _bouncedCount = 0;
            _machine.StateChanged += _stateChanges.Add;
            _machine.Recovered += () => _recoveredCount++;
            _machine.Bounced += () => _bouncedCount++;
        }

        private static void AssertVector(Vector2 actual, float x, float y)
        {
            Assert.That(actual.X, Is.EqualTo(x).Within(Tolerance), "X");
            Assert.That(actual.Y, Is.EqualTo(y).Within(Tolerance), "Y");
        }
    }
}
