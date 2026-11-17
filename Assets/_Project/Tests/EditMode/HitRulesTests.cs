using System.Numerics;
using MochiHockey.Core;
using NUnit.Framework;

namespace MochiHockey.Tests.EditMode
{
    public class HitRulesTests
    {
        private const int PlayerA = 1;
        private const int PlayerB = 2;
        private const int PlayerC = 3;

        [Test]
        public void CanHit_OpponentWhoCanBeHit_ReturnsTrue()
        {
            Assert.That(HitRules.CanHit(PlayerA, Team.A, PlayerB, Team.B, targetCanBeHit: true), Is.True);
        }

        // ---- 味方・自分（REQ-006）----

        [Test]
        public void CanHit_Teammate_ReturnsFalse()
        {
            Assert.That(HitRules.CanHit(PlayerA, Team.A, PlayerC, Team.A, targetCanBeHit: true), Is.False);
        }

        [Test]
        public void CanHit_Self_ReturnsFalse()
        {
            Assert.That(HitRules.CanHit(PlayerA, Team.A, PlayerA, Team.A, targetCanBeHit: true), Is.False);
        }

        [Test]
        public void CanHit_SameIdEvenWithDifferentTeam_ReturnsFalse()
        {
            // 呼び出し側の取り違えでも、自分自身には当てない
            Assert.That(HitRules.CanHit(PlayerA, Team.A, PlayerA, Team.B, targetCanBeHit: true), Is.False);
        }

        // ---- 被弾不可状態（REQ-035）----

        [Test]
        public void CanHit_OpponentWhoCannotBeHit_ReturnsFalse()
        {
            Assert.That(HitRules.CanHit(PlayerA, Team.A, PlayerB, Team.B, targetCanBeHit: false), Is.False);
        }

        [TestCase(0.05f)] // HitStop
        [TestCase(0.3f)]  // Launched
        [TestCase(1.0f)]  // Stuck
        [TestCase(3.0f)]  // Invincible
        public void CanHit_OpponentInPettan_ReturnsFalse(float secondsAfterHit)
        {
            var target = CreateMachine();
            target.Begin(Vector2.Zero, new Vector2(1f, 0f));
            target.Tick(secondsAfterHit);

            Assert.That(HitRules.CanHit(PlayerA, Team.A, PlayerB, Team.B, target.CanBeHit), Is.False);
        }

        // ---- 同時ヒット（REQ-036）----

        [Test]
        public void CanHit_BothSwingingAtEachOtherInSameFrame_BothHit()
        {
            var a = CreateMachine();
            var b = CreateMachine();

            // スイング開始時点の状態で両方を判定してから、被弾を反映する
            var aHitsB = HitRules.CanHit(PlayerA, Team.A, PlayerB, Team.B, b.CanBeHit);
            var bHitsA = HitRules.CanHit(PlayerB, Team.B, PlayerA, Team.A, a.CanBeHit);
            if (aHitsB) b.Begin(new Vector2(1f, 0f), new Vector2(1f, 0f));
            if (bHitsA) a.Begin(new Vector2(-1f, 0f), new Vector2(-1f, 0f));

            Assert.That(aHitsB, Is.True);
            Assert.That(bHitsA, Is.True);
            Assert.That(a.State, Is.EqualTo(PettanState.HitStop));
            Assert.That(b.State, Is.EqualTo(PettanState.HitStop));
        }

        private static PettanStateMachine CreateMachine()
        {
            // 跳ね返りなし：原点から +X で 1.08 秒に壁、2.6 秒に復帰、3.6 秒まで無敵
            var settings = TestSettings.CreatePettan();
            settings.LaunchWallBounces = 0;
            return new PettanStateMachine(settings, TestSettings.CreateRink());
        }
    }
}
