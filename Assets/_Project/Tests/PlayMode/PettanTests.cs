using System.Collections;
using MochiHockey.Core;
using MochiHockey.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MochiHockey.Tests.PlayMode
{
    /// <summary>TASK-0016：杵スイング・被弾（Pettan）・空振り硬直。</summary>
    public class PettanTests
    {
        private const float Tolerance = 0.05f;

        // リンクは X: -12〜12、Z: -7〜7、吹っ飛びは 30 m/s で壁で 1 回跳ね返る（GameBalanceConfig の仮の値）
        private GameBalanceConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            _config.MochiBall.LinearDamping = 0f;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go.name is "Player" or "MochiBall")
                {
                    Object.Destroy(go);
                }
            }

            Object.Destroy(_config);
        }

        // ---- 当たる・当たらない（REQ-025, REQ-006）----

        [UnityTest]
        public IEnumerator Swing_HitsOpponentInFront()
        {
            var attacker = CreatePlayer(new Vector3(0f, 1f, 0f), Vector3.right, Team.A);
            var target = CreatePlayer(new Vector3(1.5f, 1f, 0f), Vector3.left, Team.B);
            yield return null;

            attacker.RequestSwing();
            yield return new WaitForSeconds(0.1f);

            Assert.That(target.PettanState, Is.Not.EqualTo(PettanState.Normal));
            Assert.That(attacker.PettanState, Is.EqualTo(PettanState.Normal));
        }

        [UnityTest]
        public IEnumerator Swing_DoesNotHitTeammate()
        {
            var attacker = CreatePlayer(new Vector3(0f, 1f, 0f), Vector3.right, Team.A);
            var mate = CreatePlayer(new Vector3(1.5f, 1f, 0f), Vector3.left, Team.A);
            yield return null;

            attacker.RequestSwing();
            yield return new WaitForSeconds(0.3f);

            Assert.That(mate.PettanState, Is.EqualTo(PettanState.Normal));
        }

        [UnityTest]
        public IEnumerator Swing_DoesNotHitOpponentBehind()
        {
            var attacker = CreatePlayer(new Vector3(0f, 1f, 0f), Vector3.right, Team.A);
            var target = CreatePlayer(new Vector3(-1.5f, 1f, 0f), Vector3.right, Team.B);
            yield return null;

            attacker.RequestSwing();
            yield return new WaitForSeconds(0.3f);

            Assert.That(target.PettanState, Is.EqualTo(PettanState.Normal));
        }

        [UnityTest]
        public IEnumerator BothSwingingAtEachOtherInSameFrame_BothGetHit()
        {
            var a = CreatePlayer(new Vector3(0f, 1f, 0f), Vector3.right, Team.A);
            var b = CreatePlayer(new Vector3(1.5f, 1f, 0f), Vector3.left, Team.B);
            yield return null;

            a.RequestSwing();
            b.RequestSwing();
            yield return new WaitForSeconds(0.05f);

            Assert.That(a.PettanState, Is.Not.EqualTo(PettanState.Normal));
            Assert.That(b.PettanState, Is.Not.EqualTo(PettanState.Normal));
        }

        // ---- 空振り（REQ-025）----

        [UnityTest]
        public IEnumerator Whiff_FreezesAttackerForRecoveryTime()
        {
            var attacker = CreatePlayer(new Vector3(0f, 1f, 0f), Vector3.right, Team.A);
            yield return null;

            attacker.RequestSwing();
            yield return new WaitForSeconds(_config.Player.SwingActiveTime + 0.05f);

            Assert.That(attacker.CanControl, Is.False);
            var start = attacker.transform.position;
            attacker.SetMoveInput(Vector2.up);
            yield return new WaitForSeconds(_config.Player.WhiffRecoveryTime - 0.15f);
            Assert.That(attacker.transform.position.z, Is.EqualTo(start.z).Within(Tolerance));

            yield return new WaitForSeconds(0.2f);
            Assert.That(attacker.CanControl, Is.True);
        }

        [UnityTest]
        public IEnumerator Hit_DoesNotCauseWhiffRecovery()
        {
            var attacker = CreatePlayer(new Vector3(0f, 1f, 0f), Vector3.right, Team.A);
            CreatePlayer(new Vector3(1.5f, 1f, 0f), Vector3.left, Team.B);
            yield return null;

            attacker.RequestSwing();
            yield return new WaitForSeconds(_config.Player.SwingActiveTime + 0.05f);

            Assert.That(attacker.CanControl, Is.True);
        }

        // ---- 被弾（REQ-031〜035）----

        [UnityTest]
        public IEnumerator Hit_DropsHeldBall()
        {
            var attacker = CreatePlayer(new Vector3(0f, 1f, 0f), Vector3.right, Team.A);
            var holder = CreatePlayer(new Vector3(1.5f, 1f, 0f), Vector3.right, Team.B);
            var ball = TestWorld.CreateBall(_config, new Vector3(3f, 0.5f, 0f));
            yield return null;
            Assert.That(ball.TryAttach(holder), Is.True);

            attacker.RequestSwing();
            yield return new WaitForSeconds(0.1f);

            Assert.That(ball.IsHeld, Is.False);
            Assert.That(holder.HeldBall, Is.Null);
        }

        [UnityTest]
        public IEnumerator Hit_LaunchesStraightBouncesOffWallAndSticksToNextWall()
        {
            var attacker = CreatePlayer(new Vector3(6.5f, 1f, 0f), Vector3.right, Team.A);
            var target = CreatePlayer(new Vector3(8f, 1f, 0.5f), Vector3.left, Team.B);
            yield return null;

            attacker.RequestSwing();
            yield return new WaitForSeconds(1.2f);

            // 攻撃者の正面方向（+X）へ飛び、右の壁（x = 12）で跳ね返って、左の壁（x = -12）に貼り付く
            // 距離 4 + 24 = 28 m、30 m/s → 0.08 + 0.93 秒
            Assert.That(target.PettanState, Is.EqualTo(PettanState.Stuck));
            Assert.That(target.transform.position.x, Is.EqualTo(-12f).Within(Tolerance));
            Assert.That(target.transform.position.z, Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(target.transform.position.y, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(Vector3.Dot(target.transform.forward, Vector3.right), Is.GreaterThan(0.99f));
            Assert.That(target.transform.localScale.z, Is.EqualTo(0.05f).Within(0.001f));
            Assert.That(target.GetComponent<Collider>().enabled, Is.False);
            Assert.That(target.CanControl, Is.False);
        }

        [UnityTest]
        public IEnumerator Hit_RecoversAfterRecoverTimeWithNormalScale()
        {
            var attacker = CreatePlayer(new Vector3(6.5f, 1f, 0f), Vector3.right, Team.A);
            var target = CreatePlayer(new Vector3(8f, 1f, 0f), Vector3.left, Team.B);
            yield return null;

            attacker.RequestSwing();
            yield return new WaitForSeconds(2.8f);

            Assert.That(target.PettanState, Is.EqualTo(PettanState.Invincible));
            Assert.That(target.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(target.GetComponent<Collider>().enabled, Is.True);
            Assert.That(target.GetComponent<Rigidbody>().isKinematic, Is.False);
            Assert.That(target.CanControl, Is.True);
        }

        [UnityTest]
        public IEnumerator Invincible_BlinksAndEndsVisible()
        {
            var attacker = CreatePlayer(new Vector3(6.5f, 1f, 0f), Vector3.right, Team.A);
            var target = CreatePlayer(new Vector3(8f, 1f, 0f), Vector3.left, Team.B);
            var renderer = target.GetComponent<Renderer>();
            yield return null;

            attacker.RequestSwing();
            while (target.PettanState != PettanState.Invincible)
            {
                yield return null;
            }

            var sawHidden = false;
            while (target.PettanState == PettanState.Invincible)
            {
                sawHidden |= !renderer.enabled;
                yield return null;
            }

            Assert.That(sawHidden, Is.True);
            Assert.That(renderer.enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator Launched_PassesThroughOtherPlayers()
        {
            var attacker = CreatePlayer(new Vector3(0f, 1f, 0f), Vector3.right, Team.A);
            var target = CreatePlayer(new Vector3(1.5f, 1f, 0f), Vector3.left, Team.B);
            var bystander = CreatePlayer(new Vector3(4f, 1f, 0f), Vector3.left, Team.A);
            yield return null;

            // 距離 10.5 + 24 = 34.5 m、30 m/s → 0.08 + 1.15 秒で左の壁
            attacker.RequestSwing();
            yield return new WaitForSeconds(1.4f);

            // 右の壁で跳ね返って左の壁へ。行きも帰りも bystander のいる x = 4 を通るが、押さない
            Assert.That(target.PettanState, Is.EqualTo(PettanState.Stuck));
            Assert.That(target.transform.position.x, Is.EqualTo(-12f).Within(Tolerance));
            Assert.That(bystander.transform.position.x, Is.EqualTo(4f).Within(Tolerance));
        }

        [UnityTest]
        public IEnumerator PlayerInPettan_CannotAttachBall()
        {
            var attacker = CreatePlayer(new Vector3(0f, 1f, 0f), Vector3.right, Team.A);
            var target = CreatePlayer(new Vector3(1.5f, 1f, 0f), Vector3.left, Team.B);
            var ball = TestWorld.CreateBall(_config, new Vector3(0f, 0.5f, 6f));
            yield return null;

            attacker.RequestSwing();
            yield return new WaitForSeconds(0.3f);

            Assert.That(ball.TryAttach(target), Is.False);
        }

        [UnityTest]
        public IEnumerator PlayerInPettan_CannotBeHitAgain()
        {
            var attacker = CreatePlayer(new Vector3(6.5f, 1f, 0f), Vector3.right, Team.A);
            var target = CreatePlayer(new Vector3(8f, 1f, 0f), Vector3.left, Team.B);
            yield return null;
            attacker.RequestSwing();
            yield return new WaitForSeconds(1f);
            var elapsedState = target.PettanState;

            target.ReceiveHit(attacker);
            yield return null;

            Assert.That(target.PettanState, Is.EqualTo(elapsedState));
        }

        private PlayerController CreatePlayer(Vector3 position, Vector3 facing, Team team)
        {
            return TestWorld.CreatePlayer(_config, position, facing, team);
        }
    }
}
