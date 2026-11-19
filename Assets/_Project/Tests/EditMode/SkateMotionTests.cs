using System;
using System.Numerics;
using MochiHockey.Core;
using NUnit.Framework;

namespace MochiHockey.Tests.EditMode
{
    public class SkateMotionTests
    {
        private const float Dt = 1f / 60f;
        private const float Tolerance = 1e-3f;

        // 最高速度 6 m/s、加速度 20 m/s²、摩擦 3 /s
        private PlayerSettings _settings;
        private SkateMotion _motion;

        [SetUp]
        public void SetUp()
        {
            _settings = TestSettings.CreatePlayer();
            _motion = new SkateMotion(_settings);
        }

        [Test]
        public void NewMotion_IsStill()
        {
            Assert.That(_motion.Velocity, Is.EqualTo(Vector2.Zero));
        }

        [Test]
        public void Constructor_NullSettings_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new SkateMotion(null));
        }

        // ---- 加速（REQ-021）----

        [Test]
        public void Tick_WithInput_AcceleratesByAcceleration()
        {
            _motion.Tick(0.1f, new Vector2(1f, 0f), 1f);

            // 20 m/s² × 0.1 s
            Assert.That(_motion.Velocity.X, Is.EqualTo(2f).Within(Tolerance));
            Assert.That(_motion.Velocity.Y, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Tick_WithInputForLong_StopsAtMaxSpeed()
        {
            Run(2f, new Vector2(0f, 1f), 1f);

            Assert.That(_motion.Velocity.Y, Is.EqualTo(6f).Within(Tolerance));
        }

        [Test]
        public void Tick_HoldingLv3Mochi_MaxSpeedIs75Percent()
        {
            Run(2f, new Vector2(1f, 0f), 0.75f);

            Assert.That(_motion.Velocity.Length(), Is.EqualTo(6f * 0.75f).Within(Tolerance));
        }

        [Test]
        public void Tick_DiagonalInput_IsNotFasterThanMaxSpeed()
        {
            Run(2f, new Vector2(1f, 1f), 1f);

            Assert.That(_motion.Velocity.Length(), Is.EqualTo(6f).Within(Tolerance));
        }

        [Test]
        public void Tick_HalfStick_TargetsHalfSpeed()
        {
            Run(2f, new Vector2(0.5f, 0f), 1f);

            Assert.That(_motion.Velocity.X, Is.EqualTo(3f).Within(Tolerance));
        }

        [Test]
        public void Tick_ReversingInput_TurnsGraduallyWithInertia()
        {
            Run(2f, new Vector2(1f, 0f), 1f);

            _motion.Tick(0.1f, new Vector2(-1f, 0f), 1f);

            // 6 m/s から 20 m/s² × 0.1 s だけ逆向きに加速する（すぐには反転しない）
            Assert.That(_motion.Velocity.X, Is.EqualTo(4f).Within(Tolerance));
        }

        // ---- 慣性・摩擦 ----

        [Test]
        public void Tick_WithoutInput_SlowsDownByFriction()
        {
            Run(2f, new Vector2(1f, 0f), 1f);

            _motion.Tick(0.1f, Vector2.Zero, 1f);

            // 摩擦 3 /s で指数的に減速する: 6 × e^(-0.3)
            Assert.That(_motion.Velocity.X, Is.EqualTo(6f * MathF.Exp(-0.3f)).Within(Tolerance));
        }

        [Test]
        public void Tick_WithoutInput_KeepsSlidingInSameDirection()
        {
            Run(2f, new Vector2(1f, 0f), 1f);

            Run(0.5f, Vector2.Zero, 1f);

            Assert.That(_motion.Velocity.X, Is.GreaterThan(0f));
            Assert.That(_motion.Velocity.X, Is.LessThan(6f));
        }

        // ---- ダッシュ・停止 ----

        [Test]
        public void AddImpulse_AddsToVelocityBeyondMaxSpeed()
        {
            Run(2f, new Vector2(1f, 0f), 1f);

            _motion.AddImpulse(new Vector2(8f, 0f));

            Assert.That(_motion.Velocity.X, Is.EqualTo(14f).Within(Tolerance));
        }

        [Test]
        public void Tick_AfterDash_SlowsBackToMaxSpeedByFriction()
        {
            _motion.AddImpulse(new Vector2(14f, 0f));

            _motion.Tick(0.1f, new Vector2(1f, 0f), 1f);

            // 最高速度を超えている間は、入力があっても摩擦で減速する（加速度で急に止めない）
            Assert.That(_motion.Velocity.X, Is.EqualTo(14f * MathF.Exp(-0.3f)).Within(Tolerance));

            Run(2f, new Vector2(1f, 0f), 1f);
            Assert.That(_motion.Velocity.X, Is.EqualTo(6f).Within(Tolerance));
        }

        [Test]
        public void Stop_ResetsVelocity()
        {
            Run(1f, new Vector2(1f, 0f), 1f);

            _motion.Stop();

            Assert.That(_motion.Velocity, Is.EqualTo(Vector2.Zero));
        }

        [Test]
        public void Tick_NegativeDeltaTime_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _motion.Tick(-Dt, Vector2.Zero, 1f));
        }

        private void Run(float seconds, Vector2 input, float speedMultiplier)
        {
            var steps = (int)MathF.Round(seconds / Dt);
            for (var i = 0; i < steps; i++)
            {
                _motion.Tick(Dt, input, speedMultiplier);
            }
        }

        [Test]
        public void SetVelocity_ReplacesTheVelocity()
        {
            // 壁キックで飛び出した勢いをそのまま入れる（TASK-0034）
            var motion = new SkateMotion(TestSettings.CreatePlayer());
            motion.AddImpulse(new Vector2(3f, 0f));

            motion.SetVelocity(new Vector2(0f, -20f));

            Assert.That(motion.Velocity, Is.EqualTo(new Vector2(0f, -20f)));
        }
    }
}
