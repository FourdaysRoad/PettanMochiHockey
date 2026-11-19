using System.Linq;
using MochiHockey.Core;
using MochiHockey.Runtime;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

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

        // ---- 毎フレーム呼ばれるので、同じ値なら新しい文字列を作らない（TASK-0031）----

        [Test]
        public void Labels_ReuseTheSameStringForTheSameValue()
        {
            Assert.That(ReferenceEquals(UiText.LevelLabel(MochiLevel.Lv2), UiText.LevelLabel(MochiLevel.Lv2)), Is.True);
            Assert.That(ReferenceEquals(UiText.MultiplierLabel(3), UiText.MultiplierLabel(3)), Is.True);
            Assert.That(ReferenceEquals(UiText.PlayerLabel(2), UiText.PlayerLabel(2)), Is.True);
            Assert.That(ReferenceEquals(UiText.Number(12), UiText.Number(12)), Is.True);
        }

        [Test]
        public void Labels_DoNotAllocateForCommonValues()
        {
            // 最初の 1 回は使い回す文字列などを作るので、先に一通り呼んでおく
            Warm();

            Assert.That(() =>
            {
                UiText.LevelLabel(MochiLevel.Lv3);
                UiText.MultiplierLabel(2);
                UiText.PlayerLabel(4);
                UiText.Number(7);
            }, Is.Not.AllocatingGCMemory());
        }

        private static void Warm()
        {
            UiText.LevelLabel(MochiLevel.Lv3);
            UiText.MultiplierLabel(2);
            UiText.PlayerLabel(4);
            UiText.Number(7);
        }

        [TestCase(0, "0")]
        [TestCase(12, "12")]
        [TestCase(250, "250")]
        [TestCase(-1, "-1")]
        public void Number_ShowsTheValue(int value, string expected)
        {
            Assert.That(UiText.Number(value), Is.EqualTo(expected));
        }

        [TestCase(12, "x12")]
        public void MultiplierLabel_LargeValues(int points, string expected)
        {
            Assert.That(UiText.MultiplierLabel(points), Is.EqualTo(expected));
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
