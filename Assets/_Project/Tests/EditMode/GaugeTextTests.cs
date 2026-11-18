using System.Linq;
using MochiHockey.Core;
using MochiHockey.Runtime;
using NUnit.Framework;

namespace MochiHockey.Tests.EditMode
{
    /// <summary>TASK-0024：ゲージの文字と、プレイヤーの番号ごとの色。</summary>
    public class GaugeTextTests
    {
        [TestCase(MochiLevel.Lv1, "Lv.1")]
        [TestCase(MochiLevel.Lv2, "Lv.2")]
        [TestCase(MochiLevel.Lv3, "Lv.3")]
        public void LevelLabel(MochiLevel level, string expected)
        {
            Assert.That(UiText.LevelLabel(level), Is.EqualTo(expected));
        }

        [TestCase(1, "x1")]
        [TestCase(3, "x3")]
        public void MultiplierLabel(int points, string expected)
        {
            Assert.That(UiText.MultiplierLabel(points), Is.EqualTo(expected));
        }

        [TestCase(1, "1P")]
        [TestCase(4, "4P")]
        [TestCase(0, "?")]
        public void PlayerLabel(int number, string expected)
        {
            Assert.That(UiText.PlayerLabel(number), Is.EqualTo(expected));
        }

        [Test]
        public void PlayerColors_AreDifferentForEachNumber()
        {
            var colors = Enumerable.Range(1, 4).Select(PlayerColors.For).ToArray();

            Assert.That(colors.Distinct().Count(), Is.EqualTo(4));
        }

        [Test]
        public void PlayerColors_UnknownNumber_IsGray()
        {
            Assert.That(PlayerColors.For(0), Is.EqualTo(PlayerColors.Unknown));
            Assert.That(PlayerColors.For(9), Is.EqualTo(PlayerColors.Unknown));
        }
    }
}
