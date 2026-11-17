using System;
using MochiHockey.Core;
using NUnit.Framework;

namespace MochiHockey.Tests.EditMode
{
    public class StaminaTests
    {
        private const float Tolerance = 1e-4f;

        // 最大 100、ダッシュ 1 回 35、回復 20 /s
        private Stamina _stamina;

        [SetUp]
        public void SetUp()
        {
            _stamina = new Stamina(TestSettings.CreatePlayer());
        }

        [Test]
        public void NewStamina_IsFull()
        {
            Assert.That(_stamina.Current, Is.EqualTo(100f));
            Assert.That(_stamina.Normalized, Is.EqualTo(1f));
        }

        [Test]
        public void Constructor_NullSettings_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new Stamina(null));
        }

        // ---- ダッシュ（REQ-026）----

        [Test]
        public void TryConsumeDash_WithEnoughStamina_ConsumesDashCost()
        {
            Assert.That(_stamina.TryConsumeDash(), Is.True);
            Assert.That(_stamina.Current, Is.EqualTo(65f).Within(Tolerance));
            Assert.That(_stamina.Normalized, Is.EqualTo(0.65f).Within(Tolerance));
        }

        [Test]
        public void TryConsumeDash_WithLessThanDashCost_FailsAndKeepsStamina()
        {
            _stamina.TryConsumeDash(); // 65
            _stamina.TryConsumeDash(); // 30

            Assert.That(_stamina.TryConsumeDash(), Is.False);
            Assert.That(_stamina.Current, Is.EqualTo(30f).Within(Tolerance));
        }

        [Test]
        public void TryConsumeDash_WithExactlyDashCost_Succeeds()
        {
            var settings = TestSettings.CreatePlayer();
            settings.StaminaMax = 35f;
            var stamina = new Stamina(settings);

            Assert.That(stamina.TryConsumeDash(), Is.True);
            Assert.That(stamina.Current, Is.EqualTo(0f).Within(Tolerance));
        }

        // ---- 回復 ----

        [Test]
        public void Tick_RegeneratesOverTime()
        {
            _stamina.TryConsumeDash(); // 65

            _stamina.Tick(0.5f);

            Assert.That(_stamina.Current, Is.EqualTo(75f).Within(Tolerance));
        }

        [Test]
        public void Tick_DoesNotExceedMax()
        {
            _stamina.TryConsumeDash(); // 65

            _stamina.Tick(10f);

            Assert.That(_stamina.Current, Is.EqualTo(100f));
            Assert.That(_stamina.Normalized, Is.EqualTo(1f));
        }

        [Test]
        public void Tick_AfterRecovering_AllowsDashAgain()
        {
            _stamina.TryConsumeDash(); // 65
            _stamina.TryConsumeDash(); // 30
            Assert.That(_stamina.TryConsumeDash(), Is.False);

            _stamina.Tick(0.25f); // 35

            Assert.That(_stamina.TryConsumeDash(), Is.True);
        }

        [Test]
        public void Tick_NegativeDeltaTime_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _stamina.Tick(-0.1f));
        }
    }
}
