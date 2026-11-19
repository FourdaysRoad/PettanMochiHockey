using System.Numerics;
using MochiHockey.Core;
using NUnit.Framework;

namespace MochiHockey.Tests.EditMode
{
    /// <summary>TASK-0028：急な方向転換で氷しぶきを出すかどうか（REQ-021 の演出）。</summary>
    public class IceSprayRuleTests
    {
        // 速さ 3 m/s 以上で、進んでいる向きと入力の向きが 70 度以上ずれたら出す
        private static readonly IceSprayRule Rule = new IceSprayRule(3f, 70f);

        [Test]
        public void GoingStraight_NoSpray()
        {
            Assert.That(Rule.Intensity(new Vector2(6f, 0f), new Vector2(1f, 0f)), Is.EqualTo(0f));
        }

        [Test]
        public void GentleTurn_NoSpray()
        {
            // 45 度くらいのゆるい曲がり
            Assert.That(Rule.Intensity(new Vector2(6f, 0f), new Vector2(0.7f, 0.7f)), Is.EqualTo(0f));
        }

        [Test]
        public void SharpTurn_Sprays()
        {
            Assert.That(Rule.Intensity(new Vector2(6f, 0f), new Vector2(0f, 1f)), Is.GreaterThan(0f));
        }

        [Test]
        public void Reversing_SpraysTheMost()
        {
            var sideways = Rule.Intensity(new Vector2(6f, 0f), new Vector2(0f, 1f));
            var reverse = Rule.Intensity(new Vector2(6f, 0f), new Vector2(-1f, 0f));

            Assert.That(reverse, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(reverse, Is.GreaterThan(sideways));
        }

        [Test]
        public void Intensity_IsAtLeastHalfWhenSpraying()
        {
            // しきい値ちょうど越えでも、目に見える量を出す
            var justOver = Rule.Intensity(new Vector2(6f, 0f), Direction(71f));

            Assert.That(justOver, Is.GreaterThanOrEqualTo(0.5f));
            Assert.That(justOver, Is.LessThan(0.6f));
        }

        [Test]
        public void TooSlow_NoSpray()
        {
            Assert.That(Rule.Intensity(new Vector2(2f, 0f), new Vector2(-1f, 0f)), Is.EqualTo(0f));
        }

        [Test]
        public void NoInput_NoSpray()
        {
            // 手を離して滑っているだけ
            Assert.That(Rule.Intensity(new Vector2(6f, 0f), Vector2.Zero), Is.EqualTo(0f));
            Assert.That(Rule.Intensity(new Vector2(6f, 0f), new Vector2(-0.05f, 0f)), Is.EqualTo(0f));
        }

        [Test]
        public void SmallStickInput_StillCountsDirection()
        {
            // スティックを少しだけ倒しても向きで判定する
            Assert.That(Rule.Intensity(new Vector2(6f, 0f), new Vector2(-0.3f, 0f)), Is.EqualTo(1f).Within(1e-4f));
        }

        private static Vector2 Direction(float degrees)
        {
            var radians = degrees * System.MathF.PI / 180f;
            return new Vector2(System.MathF.Cos(radians), System.MathF.Sin(radians));
        }
    }
}
