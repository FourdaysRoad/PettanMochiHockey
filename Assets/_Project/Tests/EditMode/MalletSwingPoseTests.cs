using MochiHockey.Core;
using NUnit.Framework;

namespace MochiHockey.Tests.EditMode
{
    /// <summary>TASK-0027：杵の見た目の振り（構え → 横に振り抜く → 構えに戻る）の角度。</summary>
    public class MalletSwingPoseTests
    {
        // 構えは右前（+60 度）。150 度振って左前（-90 度）まで振り抜き、0.15 秒で振って 0.2 秒で戻る
        private static readonly MalletSwingPose Pose = new MalletSwingPose(60f, 150f, 0.15f, 0.2f);

        [Test]
        public void BeforeSwing_IsAtRest()
        {
            Assert.That(Pose.YawAt(-0.1f), Is.EqualTo(60f));
        }

        [Test]
        public void AtStart_IsAtRest()
        {
            Assert.That(Pose.YawAt(0f), Is.EqualTo(60f).Within(1e-4f));
        }

        [Test]
        public void AtEndOfSweep_IsFullySwungThrough()
        {
            Assert.That(Pose.YawAt(0.15f), Is.EqualTo(-90f).Within(1e-4f));
        }

        [Test]
        public void DuringSweep_MovesFromRestTowardSwungThrough()
        {
            var early = Pose.YawAt(0.05f);
            var late = Pose.YawAt(0.1f);

            Assert.That(early, Is.LessThan(60f));
            Assert.That(late, Is.LessThan(early));
            Assert.That(late, Is.GreaterThan(-90f));
        }

        [Test]
        public void Sweep_StartsFastAndSlowsDown()
        {
            // 振り始めが速く、振り抜く手前でゆっくりになる（前半の方が大きく動く）
            var firstHalf = 60f - Pose.YawAt(0.075f);
            var secondHalf = Pose.YawAt(0.075f) - (-90f);

            Assert.That(firstHalf, Is.GreaterThan(secondHalf));
        }

        [Test]
        public void DuringReturn_MovesBackTowardRest()
        {
            var yaw = Pose.YawAt(0.25f);

            Assert.That(yaw, Is.GreaterThan(-90f));
            Assert.That(yaw, Is.LessThan(60f));
        }

        [Test]
        public void AfterReturn_IsAtRest()
        {
            Assert.That(Pose.YawAt(0.35f), Is.EqualTo(60f).Within(1e-4f));
            Assert.That(Pose.YawAt(5f), Is.EqualTo(60f));
        }

        [Test]
        public void IsSwinging_OnlyBetweenStartAndReturn()
        {
            Assert.That(Pose.IsSwinging(-0.01f), Is.False);
            Assert.That(Pose.IsSwinging(0f), Is.True);
            Assert.That(Pose.IsSwinging(0.3f), Is.True);
            Assert.That(Pose.IsSwinging(Pose.Duration), Is.False);
            Assert.That(Pose.IsSwinging(0.36f), Is.False);
        }

        [Test]
        public void ZeroTimes_JumpsWithoutDividingByZero()
        {
            var instant = new MalletSwingPose(60f, 150f, 0f, 0f);

            Assert.That(instant.YawAt(0f), Is.EqualTo(60f));
            Assert.That(float.IsNaN(instant.YawAt(0f)), Is.False);
            Assert.That(instant.IsSwinging(0f), Is.False);
        }
    }
}
