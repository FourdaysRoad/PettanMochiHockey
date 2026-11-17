using System;
using MochiHockey.Core;
using NUnit.Framework;

namespace MochiHockey.Tests.EditMode
{
    public class ShotChargeTests
    {
        private const float Tolerance = 1e-5f;

        // 最大 1.0 秒・2 段階（0.5 秒ごとに 1 段階）
        private ShotCharge _charge;

        [SetUp]
        public void SetUp()
        {
            _charge = new ShotCharge(TestSettings.CreatePlayer());
        }

        [Test]
        public void NewCharge_IsNotCharging()
        {
            Assert.That(_charge.IsCharging, Is.False);
            Assert.That(_charge.Normalized, Is.EqualTo(0f));
        }

        [Test]
        public void Constructor_NullSettings_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ShotCharge(null));
        }

        // ---- 段階（REQ-023）----

        [Test]
        public void Release_RightAfterBegin_ReturnsStage0()
        {
            _charge.Begin();

            Assert.That(_charge.Release(), Is.EqualTo(0));
        }

        [TestCase(0.49f, 0)]
        [TestCase(0.5f, 1)]
        [TestCase(0.99f, 1)]
        [TestCase(1.0f, 2)]
        [TestCase(3.0f, 2)]
        public void Release_ReturnsStageForHoldTime(float holdSeconds, int expectedStage)
        {
            _charge.Begin();
            _charge.Tick(holdSeconds);

            Assert.That(_charge.Release(), Is.EqualTo(expectedStage));
        }

        [Test]
        public void Release_AfterManySmallTicks_ReachesStageAtBoundary()
        {
            _charge.Begin();
            for (var i = 0; i < 30; i++)
            {
                _charge.Tick(1f / 60f); // 合計 0.5 秒
            }

            Assert.That(_charge.Release(), Is.EqualTo(1));
        }

        [Test]
        public void Release_UsesStagesFromSettings()
        {
            var settings = TestSettings.CreatePlayer();
            settings.ChargeMaxTime = 1.5f;
            settings.ChargeStages = 3;
            var charge = new ShotCharge(settings);

            charge.Begin();
            charge.Tick(1.0f);

            Assert.That(charge.Release(), Is.EqualTo(2));
        }

        [Test]
        public void Release_ResetsCharge()
        {
            _charge.Begin();
            _charge.Tick(1f);

            _charge.Release();

            Assert.That(_charge.IsCharging, Is.False);
            Assert.That(_charge.Normalized, Is.EqualTo(0f));
            Assert.That(_charge.Release(), Is.EqualTo(0));
        }

        [Test]
        public void Release_WithoutBegin_ReturnsStage0()
        {
            Assert.That(_charge.Release(), Is.EqualTo(0));
        }

        // ---- ゲージ ----

        [Test]
        public void Normalized_GrowsWithHoldTimeAndCapsAtOne()
        {
            _charge.Begin();
            Assert.That(_charge.IsCharging, Is.True);

            _charge.Tick(0.25f);
            Assert.That(_charge.Normalized, Is.EqualTo(0.25f).Within(Tolerance));

            _charge.Tick(2f);
            Assert.That(_charge.Normalized, Is.EqualTo(1f));
        }

        [Test]
        public void Tick_WhenNotCharging_DoesNothing()
        {
            _charge.Tick(1f);
            _charge.Begin();

            Assert.That(_charge.Release(), Is.EqualTo(0));
        }

        [Test]
        public void Begin_WhileCharging_KeepsProgress()
        {
            _charge.Begin();
            _charge.Tick(0.6f);

            _charge.Begin();

            Assert.That(_charge.Release(), Is.EqualTo(1));
        }

        // ---- 被弾でキャンセル ----

        [Test]
        public void Cancel_ResetsCharge()
        {
            _charge.Begin();
            _charge.Tick(1f);

            _charge.Cancel();

            Assert.That(_charge.IsCharging, Is.False);
            Assert.That(_charge.Normalized, Is.EqualTo(0f));
            Assert.That(_charge.Release(), Is.EqualTo(0));
        }

        [Test]
        public void Tick_NegativeDeltaTime_Throws()
        {
            _charge.Begin();

            Assert.Throws<ArgumentOutOfRangeException>(() => _charge.Tick(-0.1f));
        }
    }
}
