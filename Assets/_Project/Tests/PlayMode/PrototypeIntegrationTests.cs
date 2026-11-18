using System.Collections;
using MochiHockey.Core;
using MochiHockey.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MochiHockey.Tests.PlayMode
{
    /// <summary>
    /// TASK-0017：Match シーンで、育てる → 叩かれて落とす → 吹っ飛んで跳ね返り貼り付く → 復帰、を通しで確かめる。
    /// </summary>
    public class PrototypeIntegrationTests
    {
        [UnityTearDown]
        public IEnumerator UnloadMatchScene()
        {
            var match = SceneManager.GetSceneByName("Match");
            if (!match.isLoaded)
            {
                yield break;
            }

            var empty = SceneManager.CreateScene("EmptyAfterPrototypeIntegrationTests");
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(match);
        }

        [UnityTest]
        public IEnumerator GrowBall_GetHit_DropIt_BounceAndStick_ThenRecover()
        {
            yield return SceneManager.LoadSceneAsync("Match", LoadSceneMode.Single);
            var p1 = Player("Player1");
            var p2 = Player("Player2");
            var ball = Object.FindFirstObjectByType<MochiBall>();
            yield return new WaitForFixedUpdate();

            // P1 が餅を持ってご飯粒を 3 個吸収 → Lv.2（1.5 倍、速度 90%）
            Place(p1, new Vector3(-4f, 1f, 0f), Vector3.right);
            Place(ball.GetComponent<Rigidbody>(), new Vector3(-2.5f, 0.5f, 0f));
            yield return new WaitForFixedUpdate();
            Assert.That(ball.TryAttach(p1), Is.True);
            for (var i = 0; i < 3; i++) ball.TryAbsorbRice();
            Assert.That(ball.Level, Is.EqualTo(MochiLevel.Lv2));
            Assert.That(p1.SpeedMultiplier, Is.EqualTo(0.9f).Within(0.001f));

            // P2 が P1 の背後（左側）から右向きに杵を振る（被弾する側の向きは関係ない）
            Place(p2, new Vector3(-5.2f, 1f, 0f), Vector3.right);
            yield return new WaitForFixedUpdate();
            p2.RequestSwing();
            yield return null;
            yield return null;

            // P1 は餅を落とし（Lv.2 のまま）、右へ吹っ飛ぶ
            Assert.That(p1.PettanState, Is.Not.EqualTo(PettanState.Normal));
            Assert.That(ball.IsHeld, Is.False);
            Assert.That(ball.Level, Is.EqualTo(MochiLevel.Lv2));
            Assert.That(p1.SpeedMultiplier, Is.EqualTo(1f));

            // 右の壁（x = 12）で跳ね返り、左の壁（x = -12）に貼り付く：16 + 24 = 40 m、30 m/s → 約 1.41 秒
            yield return new WaitForSeconds(1.7f);
            Assert.That(p1.PettanState, Is.EqualTo(PettanState.Stuck));
            Assert.That(p1.transform.position.x, Is.EqualTo(-12f).Within(0.05f));

            // 2.6 秒で復帰し、操作できるようになる
            yield return new WaitForSeconds(1.1f);
            Assert.That(p1.PettanState, Is.EqualTo(PettanState.Invincible));
            Assert.That(p1.CanControl, Is.True);
            Assert.That(p1.transform.localScale, Is.EqualTo(Vector3.one));
        }

        [UnityTest]
        public IEnumerator Shoot_TravelsAcrossRinkAndShooterCannotRegrabImmediately()
        {
            yield return SceneManager.LoadSceneAsync("Match", LoadSceneMode.Single);
            var p1 = Player("Player1");
            var ball = Object.FindFirstObjectByType<MochiBall>();
            yield return new WaitForFixedUpdate();

            Place(p1, new Vector3(-4f, 1f, 0f), Vector3.right);
            Place(ball.GetComponent<Rigidbody>(), new Vector3(-2.5f, 0.5f, 0f));
            yield return new WaitForFixedUpdate();
            Assert.That(ball.TryAttach(p1), Is.True);

            p1.BeginShoot();
            p1.ReleaseShoot();
            yield return null;
            Assert.That(ball.TryAttach(p1), Is.False);

            yield return new WaitForSeconds(0.3f);
            Assert.That(ball.transform.position.x, Is.GreaterThan(0f));
        }

        private static PlayerController Player(string name)
        {
            var player = GameObject.Find(name).GetComponent<PlayerController>();
            player.GetComponent<PlayerInputReader>().enabled = false;
            return player;
        }

        private static void Place(PlayerController player, Vector3 position, Vector3 facing)
        {
            var body = player.GetComponent<Rigidbody>();
            var rotation = Quaternion.LookRotation(facing, Vector3.up);
            body.position = position;
            body.rotation = rotation;
            player.transform.SetPositionAndRotation(position, rotation);
            body.linearVelocity = Vector3.zero;
            player.SetMoveInput(Vector2.zero);
        }

        private static void Place(Rigidbody body, Vector3 position)
        {
            body.position = position;
            body.transform.position = position;
            body.linearVelocity = Vector3.zero;
        }
    }
}
