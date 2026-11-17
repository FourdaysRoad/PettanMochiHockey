using System;
using System.Collections.Generic;
using System.Numerics;
using MochiHockey.Core;
using NUnit.Framework;

namespace MochiHockey.Tests.EditMode
{
    /// <summary>TASK-0010：状態遷移と各状態のフラグ。</summary>
    public class PettanStateMachineTests
    {
        private const float Dt = 1f / 60f;
        private const float Tolerance = 1e-3f;

        // HitStop 0.08s、吹っ飛び 12 m/s、復帰 2.6s、無敵終了 3.6s。リンクは X: -12〜12、Z: -7〜7
        private PettanSettings _settings;
        private PettanStateMachine _machine;
        private List<PettanState> _stateChanges;
        private int _recoveredCount;

        // 0.08 + 4.96 / 12 = 0.5 秒で右の壁に届く位置
        private static readonly Vector2 HitPosition = new Vector2(12f - 12f * 0.42f, 1f);
        private static readonly Vector2 Right = new Vector2(1f, 0f);

        [SetUp]
        public void SetUp()
        {
            _settings = TestSettings.CreatePettan();
            _machine = new PettanStateMachine(_settings, TestSettings.CreateRink());
            _stateChanges = new List<PettanState>();
            _recoveredCount = 0;
            _machine.StateChanged += _stateChanges.Add;
            _machine.Recovered += () => _recoveredCount++;
        }

        [Test]
        public void Constructor_NullSettings_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new PettanStateMachine(null, TestSettings.CreateRink()));
        }

        [Test]
        public void Constructor_InvalidRink_Throws()
        {
            Assert.Throws<ArgumentException>(() => new PettanStateMachine(_settings, default));
        }

        // ---- 初期状態 ----

        [Test]
        public void NewMachine_IsNormal()
        {
            Assert.That(_machine.State, Is.EqualTo(PettanState.Normal));
            Assert.That(_machine.Elapsed, Is.EqualTo(0f));
            AssertFlags(canControl: true, canBeHit: true, collidesWithPlayers: true, colliderEnabled: true);
            Assert.That(_machine.Scale, Is.EqualTo(Vector3.One));
        }

        // ---- 時刻ごとの状態（REQ-031）----

        [TestCase(0.05f, PettanState.HitStop)]
        [TestCase(0.3f, PettanState.Launched)]
        [TestCase(1.0f, PettanState.Stuck)]
        [TestCase(2.0f, PettanState.Stuck)]
        [TestCase(3.0f, PettanState.Invincible)]
        [TestCase(3.7f, PettanState.Normal)]
        public void State_AtTimeAfterBegin_FollowsSequence(float seconds, PettanState expected)
        {
            _machine.Begin(HitPosition, Right);

            Run(seconds);

            Assert.That(_machine.State, Is.EqualTo(expected));
        }

        [Test]
        public void Begin_EntersHitStopAndRaisesStateChanged()
        {
            _machine.Begin(HitPosition, Right);

            Assert.That(_machine.State, Is.EqualTo(PettanState.HitStop));
            Assert.That(_machine.Position, Is.EqualTo(HitPosition));
            Assert.That(_stateChanges, Is.EqualTo(new[] { PettanState.HitStop }));
        }

        [Test]
        public void Tick_HitStopDuration_EntersLaunched()
        {
            _machine.Begin(HitPosition, Right);

            _machine.Tick(0.079f);
            Assert.That(_machine.State, Is.EqualTo(PettanState.HitStop));

            _machine.Tick(0.002f);
            Assert.That(_machine.State, Is.EqualTo(PettanState.Launched));
        }

        [Test]
        public void Tick_ReachingWall_EntersStuckAtWall()
        {
            _machine.Begin(HitPosition, Right);

            _machine.Tick(0.49f);
            Assert.That(_machine.State, Is.EqualTo(PettanState.Launched));

            _machine.Tick(0.02f);
            Assert.That(_machine.State, Is.EqualTo(PettanState.Stuck));
            Assert.That(_machine.Position.X, Is.EqualTo(12f).Within(Tolerance));
            Assert.That(_machine.Position.Y, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(_machine.WallNormal, Is.EqualTo(new Vector2(-1f, 0f)));
        }

        [Test]
        public void Tick_RecoverTime_RaisesRecoveredOnceAndEntersInvincible()
        {
            _machine.Begin(HitPosition, Right);

            Run(2.59f);
            Assert.That(_recoveredCount, Is.EqualTo(0));

            Run(0.05f);
            Assert.That(_machine.State, Is.EqualTo(PettanState.Invincible));
            Assert.That(_recoveredCount, Is.EqualTo(1));

            Run(2f);
            Assert.That(_recoveredCount, Is.EqualTo(1));
        }

        [Test]
        public void Tick_InvincibleEndTime_ReturnsToNormal()
        {
            _machine.Begin(HitPosition, Right);

            Run(3.59f);
            Assert.That(_machine.State, Is.EqualTo(PettanState.Invincible));

            Run(0.05f);
            Assert.That(_machine.State, Is.EqualTo(PettanState.Normal));
            Assert.That(_machine.Elapsed, Is.EqualTo(0f));
        }

        [Test]
        public void StateChanged_ReportsWholeSequenceInOrder()
        {
            _machine.Begin(HitPosition, Right);

            Run(4f);

            Assert.That(_stateChanges, Is.EqualTo(new[]
            {
                PettanState.HitStop,
                PettanState.Launched,
                PettanState.Stuck,
                PettanState.Invincible,
                PettanState.Normal,
            }));
        }

        [Test]
        public void Tick_OneLargeStep_PassesThroughEveryState()
        {
            _machine.Begin(HitPosition, Right);

            _machine.Tick(10f);

            Assert.That(_machine.State, Is.EqualTo(PettanState.Normal));
            Assert.That(_stateChanges, Is.EqualTo(new[]
            {
                PettanState.HitStop,
                PettanState.Launched,
                PettanState.Stuck,
                PettanState.Invincible,
                PettanState.Normal,
            }));
            Assert.That(_recoveredCount, Is.EqualTo(1));
        }

        [Test]
        public void Elapsed_CountsSecondsSinceBegin()
        {
            _machine.Begin(HitPosition, Right);

            _machine.Tick(0.25f);
            _machine.Tick(0.5f);

            Assert.That(_machine.Elapsed, Is.EqualTo(0.75f).Within(Tolerance));
        }

        // ---- 状態ごとのフラグ（REQ-035）----

        [Test]
        public void HitStop_CannotControlOrBeHit()
        {
            _machine.Begin(HitPosition, Right);

            AssertFlags(canControl: false, canBeHit: false, collidesWithPlayers: true, colliderEnabled: true);
            Assert.That(_machine.Scale, Is.EqualTo(Vector3.One));
        }

        [Test]
        public void Launched_PassesThroughPlayers()
        {
            _machine.Begin(HitPosition, Right);
            Run(0.3f);

            AssertFlags(canControl: false, canBeHit: false, collidesWithPlayers: false, colliderEnabled: true);
            Assert.That(_machine.Scale, Is.EqualTo(Vector3.One));
        }

        [Test]
        public void Stuck_DisablesColliderAndUsesStuckScale()
        {
            _machine.Begin(HitPosition, Right);
            Run(1f);

            AssertFlags(canControl: false, canBeHit: false, collidesWithPlayers: false, colliderEnabled: false);
            Assert.That(_machine.Scale, Is.EqualTo(new Vector3(1.3f, 1.3f, 0.05f)));
        }

        [Test]
        public void Invincible_CanControlButCannotBeHit()
        {
            _machine.Begin(HitPosition, Right);
            Run(3f);

            AssertFlags(canControl: true, canBeHit: false, collidesWithPlayers: true, colliderEnabled: true);
            Assert.That(_machine.Scale, Is.EqualTo(Vector3.One));
        }

        [Test]
        public void BackToNormal_CanControlAndBeHitAgain()
        {
            _machine.Begin(HitPosition, Right);
            Run(4f);

            AssertFlags(canControl: true, canBeHit: true, collidesWithPlayers: true, colliderEnabled: true);
        }

        // ---- Begin の受け付け ----

        [TestCase(0.05f)]
        [TestCase(0.3f)]
        [TestCase(1.0f)]
        [TestCase(3.0f)]
        public void Begin_WhenNotNormal_IsIgnored(float seconds)
        {
            _machine.Begin(HitPosition, Right);
            Run(seconds);
            var state = _machine.State;
            var elapsed = _machine.Elapsed;
            var changes = _stateChanges.Count;

            _machine.Begin(Vector2.Zero, new Vector2(0f, 1f));

            Assert.That(_machine.State, Is.EqualTo(state));
            Assert.That(_machine.Elapsed, Is.EqualTo(elapsed));
            Assert.That(_stateChanges.Count, Is.EqualTo(changes));
        }

        [Test]
        public void Begin_AfterReturningToNormal_StartsAgain()
        {
            _machine.Begin(HitPosition, Right);
            Run(4f);

            _machine.Begin(Vector2.Zero, new Vector2(-1f, 0f));

            Assert.That(_machine.State, Is.EqualTo(PettanState.HitStop));
            Assert.That(_machine.Position, Is.EqualTo(Vector2.Zero));
        }

        [Test]
        public void Begin_ZeroDirection_Throws()
        {
            Assert.Throws<ArgumentException>(() => _machine.Begin(HitPosition, Vector2.Zero));
        }

        [Test]
        public void Tick_InNormal_DoesNothing()
        {
            _machine.Tick(1f);

            Assert.That(_machine.State, Is.EqualTo(PettanState.Normal));
            Assert.That(_stateChanges, Is.Empty);
        }

        [Test]
        public void Tick_NegativeDeltaTime_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _machine.Tick(-Dt));
        }

        private void Run(float seconds)
        {
            var steps = (int)MathF.Round(seconds / Dt);
            for (var i = 0; i < steps; i++)
            {
                _machine.Tick(Dt);
            }
        }

        private void AssertFlags(bool canControl, bool canBeHit, bool collidesWithPlayers, bool colliderEnabled)
        {
            Assert.That(_machine.CanControl, Is.EqualTo(canControl), nameof(_machine.CanControl));
            Assert.That(_machine.CanBeHit, Is.EqualTo(canBeHit), nameof(_machine.CanBeHit));
            Assert.That(_machine.CollidesWithPlayers, Is.EqualTo(collidesWithPlayers), nameof(_machine.CollidesWithPlayers));
            Assert.That(_machine.ColliderEnabled, Is.EqualTo(colliderEnabled), nameof(_machine.ColliderEnabled));
        }
    }
}
