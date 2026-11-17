using System;
using MochiHockey.Core;
using NUnit.Framework;

namespace MochiHockey.Tests.EditMode
{
    public class MochiGrowthTests
    {
        private MochiGrowth _growth;

        [SetUp]
        public void SetUp()
        {
            // 閾値 3 / 6、大きさ 1.0 / 1.5 / 2.5、得点 1 / 2 / 3、速度 1.0 / 0.9 / 0.75
            _growth = new MochiGrowth(TestSettings.CreateMochi());
        }

        [Test]
        public void Constructor_NullSettings_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new MochiGrowth(null));
        }

        // ---- レベル（REQ-013）----

        [TestCase(0, MochiLevel.Lv1)]
        [TestCase(2, MochiLevel.Lv1)]
        [TestCase(3, MochiLevel.Lv2)]
        [TestCase(5, MochiLevel.Lv2)]
        [TestCase(6, MochiLevel.Lv3)]
        [TestCase(10, MochiLevel.Lv3)]
        public void GetLevel_ReturnsLevelForRiceCount(int riceCount, MochiLevel expected)
        {
            Assert.That(_growth.GetLevel(riceCount), Is.EqualTo(expected));
        }

        [Test]
        public void GetLevel_NegativeRiceCount_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _growth.GetLevel(-1));
        }

        [Test]
        public void GetLevel_UsesThresholdsFromSettings()
        {
            var settings = TestSettings.CreateMochi();
            settings.Lv2Threshold = 4;
            settings.Lv3Threshold = 8;
            var growth = new MochiGrowth(settings);

            Assert.That(growth.GetLevel(3), Is.EqualTo(MochiLevel.Lv1));
            Assert.That(growth.GetLevel(4), Is.EqualTo(MochiLevel.Lv2));
            Assert.That(growth.GetLevel(7), Is.EqualTo(MochiLevel.Lv2));
            Assert.That(growth.GetLevel(8), Is.EqualTo(MochiLevel.Lv3));
        }

        // ---- レベル別の値 ----

        [TestCase(MochiLevel.Lv1, 1)]
        [TestCase(MochiLevel.Lv2, 2)]
        [TestCase(MochiLevel.Lv3, 3)]
        public void GetPoints_ReturnsPointsForLevel(MochiLevel level, int expected)
        {
            Assert.That(_growth.GetPoints(level), Is.EqualTo(expected));
        }

        [TestCase(MochiLevel.Lv1, 1.0f)]
        [TestCase(MochiLevel.Lv2, 1.5f)]
        [TestCase(MochiLevel.Lv3, 2.5f)]
        public void GetScale_ReturnsScaleForLevel(MochiLevel level, float expected)
        {
            Assert.That(_growth.GetScale(level), Is.EqualTo(expected));
        }

        [TestCase(MochiLevel.Lv1, 1.0f)]
        [TestCase(MochiLevel.Lv2, 0.9f)]
        [TestCase(MochiLevel.Lv3, 0.75f)]
        public void GetSpeedMultiplier_ReturnsMultiplierForLevel(MochiLevel level, float expected)
        {
            Assert.That(_growth.GetSpeedMultiplier(level), Is.EqualTo(expected));
        }

        [TestCase(0)]
        [TestCase(4)]
        public void GetPoints_UndefinedLevel_Throws(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _growth.GetPoints((MochiLevel)value));
        }

        // ---- 成長ゲージ（REQ-053）----

        [TestCase(0, 0f)]
        [TestCase(1, 1f / 3f)]
        [TestCase(2, 2f / 3f)]
        [TestCase(3, 0f)]
        [TestCase(4, 1f / 3f)]
        [TestCase(5, 2f / 3f)]
        [TestCase(6, 1f)]
        [TestCase(20, 1f)]
        public void GetGrowthProgress_ReturnsProgressTowardNextLevel(int riceCount, float expected)
        {
            Assert.That(_growth.GetGrowthProgress(riceCount), Is.EqualTo(expected).Within(1e-5f));
        }

        [Test]
        public void GetGrowthProgress_NegativeRiceCount_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _growth.GetGrowthProgress(-1));
        }
    }
}
