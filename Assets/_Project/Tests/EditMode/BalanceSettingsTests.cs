using System;
using MochiHockey.Core;
using NUnit.Framework;

namespace MochiHockey.Tests.EditMode
{
    public class BalanceSettingsTests
    {
        [Test]
        public void Validate_ValidSettings_DoesNotThrow()
        {
            var settings = TestSettings.Create();

            Assert.DoesNotThrow(() => settings.Validate());
        }

        [TestCase(nameof(BalanceSettings.Mochi))]
        [TestCase(nameof(BalanceSettings.Pettan))]
        [TestCase(nameof(BalanceSettings.Match))]
        [TestCase(nameof(BalanceSettings.Rice))]
        [TestCase(nameof(BalanceSettings.Player))]
        public void Validate_MissingChildSettings_Throws(string child)
        {
            var settings = TestSettings.Create();
            typeof(BalanceSettings).GetField(child).SetValue(settings, null);

            var ex = Assert.Throws<InvalidOperationException>(() => settings.Validate());
            StringAssert.Contains(child, ex.Message);
        }

        [TestCase(2)]
        [TestCase(4)]
        public void Validate_ScaleByLevelLengthNotThree_Throws(int length)
        {
            var settings = TestSettings.Create();
            settings.Mochi.ScaleByLevel = new float[length];

            var ex = Assert.Throws<InvalidOperationException>(() => settings.Validate());
            StringAssert.Contains(nameof(MochiSettings.ScaleByLevel), ex.Message);
        }

        [TestCase(2)]
        [TestCase(4)]
        public void Validate_PointsByLevelLengthNotThree_Throws(int length)
        {
            var settings = TestSettings.Create();
            settings.Mochi.PointsByLevel = new int[length];

            var ex = Assert.Throws<InvalidOperationException>(() => settings.Validate());
            StringAssert.Contains(nameof(MochiSettings.PointsByLevel), ex.Message);
        }

        [TestCase(2)]
        [TestCase(4)]
        public void Validate_SpeedMultiplierByLevelLengthNotThree_Throws(int length)
        {
            var settings = TestSettings.Create();
            settings.Mochi.SpeedMultiplierByLevel = new float[length];

            var ex = Assert.Throws<InvalidOperationException>(() => settings.Validate());
            StringAssert.Contains(nameof(MochiSettings.SpeedMultiplierByLevel), ex.Message);
        }

        [Test]
        public void Validate_LevelArrayIsNull_Throws()
        {
            var settings = TestSettings.Create();
            settings.Mochi.ScaleByLevel = null;

            var ex = Assert.Throws<InvalidOperationException>(() => settings.Validate());
            StringAssert.Contains(nameof(MochiSettings.ScaleByLevel), ex.Message);
        }

        [Test]
        public void Validate_Lv3ThresholdNotAboveLv2_Throws()
        {
            var settings = TestSettings.Create();
            settings.Mochi.Lv2Threshold = 6;
            settings.Mochi.Lv3Threshold = 6;

            Assert.Throws<InvalidOperationException>(() => settings.Validate());
        }

        [Test]
        public void Validate_RinkNotSet_Throws()
        {
            var settings = TestSettings.Create();
            settings.Rink = default;

            var ex = Assert.Throws<InvalidOperationException>(() => settings.Validate());
            StringAssert.Contains(nameof(BalanceSettings.Rink), ex.Message);
        }

        [Test]
        public void Validate_NegativeWallBounces_Throws()
        {
            var settings = TestSettings.Create();
            settings.Pettan.LaunchWallBounces = -1;

            var ex = Assert.Throws<InvalidOperationException>(() => settings.Validate());
            StringAssert.Contains(nameof(PettanSettings.LaunchWallBounces), ex.Message);
        }

        [Test]
        public void Validate_RiceMinAboveMax_Throws()
        {
            var settings = TestSettings.Create();
            settings.Rice.MinCount = 11;
            settings.Rice.MaxCount = 10;

            Assert.Throws<InvalidOperationException>(() => settings.Validate());
        }
    }
}
