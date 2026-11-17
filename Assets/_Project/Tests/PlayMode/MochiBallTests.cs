using System.Collections;
using MochiHockey.Core;
using MochiHockey.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MochiHockey.Tests.PlayMode
{
    public class MochiBallTests
    {
        private const float Tolerance = 0.05f;

        private GameBalanceConfig _config;
        private PlayerController _player;
        private MochiBall _ball;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<GameBalanceConfig>();

            // シュート速度をそのまま測れるよう、減速はなしにする
            _config.MochiBall.LinearDamping = 0f;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go.name is "Player" or "MochiBall" or "MochiRice" or "MochiRiceTemplate" or "Spawner")
                {
                    Object.Destroy(go);
                }
            }

            Object.Destroy(_config);
        }

        // ---- 吸着（REQ-022）----

        [UnityTest]
        public IEnumerator ApproachingFromFront_AttachesBall()
        {
            yield return SetUpPlayerAndBallAhead();

            _player.SetMoveInput(Vector2.right);
            yield return new WaitForSeconds(0.6f);

            Assert.That(_ball.IsHeld, Is.True);
            Assert.That(_ball.Holder, Is.SameAs(_player));
            Assert.That(_player.HeldBall, Is.SameAs(_ball));
        }

        [UnityTest]
        public IEnumerator BallHittingPlayersBack_DoesNotAttach()
        {
            // 左を向いて立っているプレイヤーの背中に、右から餅が転がってくる
            _player = TestWorld.CreatePlayer(_config, new Vector3(0f, 1f, 0f), Vector3.left);
            _ball = TestWorld.CreateBall(_config, new Vector3(2f, 0.5f, 0f));
            yield return new WaitForFixedUpdate();

            _ball.GetComponent<Rigidbody>().linearVelocity = new Vector3(-4f, 0f, 0f);
            yield return new WaitForSeconds(0.6f);

            Assert.That(_ball.IsHeld, Is.False);
        }

        [UnityTest]
        public IEnumerator HeldBall_IsDribbledInFrontOfHolder()
        {
            yield return AttachBall();

            _player.SetMoveInput(Vector2.up);
            yield return new WaitForSeconds(0.6f);

            var toBall = _ball.transform.position - _player.transform.position;
            toBall.y = 0f;
            Assert.That(_ball.IsHeld, Is.True);
            Assert.That(Vector3.Dot(toBall.normalized, _player.transform.forward), Is.GreaterThan(0.95f));
        }

        // ---- シュート（REQ-023, REQ-024）----

        [UnityTest]
        public IEnumerator QuickShoot_LaunchesForwardAtStage0Speed()
        {
            yield return AttachBall();
            _player.SetMoveInput(Vector2.zero);

            _player.BeginShoot();
            _player.ReleaseShoot();
            yield return new WaitForFixedUpdate();

            var velocity = _ball.GetComponent<Rigidbody>().linearVelocity;
            Assert.That(_ball.IsHeld, Is.False);
            Assert.That(_player.HeldBall, Is.Null);
            Assert.That(velocity.magnitude, Is.EqualTo(_config.MochiBall.ShotSpeedByStage[0]).Within(Tolerance));
            Assert.That(Vector3.Dot(velocity.normalized, _player.transform.forward), Is.GreaterThan(0.99f));
        }

        [UnityTest]
        public IEnumerator FullCharge_LaunchesAtMaxStageSpeed()
        {
            yield return AttachBall();
            _player.SetMoveInput(Vector2.zero);

            _player.BeginShoot();
            yield return new WaitForSeconds(_config.Player.ChargeMaxTime + 0.1f);
            _player.ReleaseShoot();
            yield return new WaitForFixedUpdate();

            var speed = _ball.GetComponent<Rigidbody>().linearVelocity.magnitude;
            Assert.That(speed, Is.EqualTo(_config.MochiBall.ShotSpeedByStage[_config.Player.ChargeStages]).Within(Tolerance));
        }

        [UnityTest]
        public IEnumerator ShootWithoutBall_DoesNothing()
        {
            yield return SetUpPlayerAndBallAhead();

            _player.BeginShoot();
            _player.ReleaseShoot();
            yield return new WaitForFixedUpdate();

            Assert.That(_ball.GetComponent<Rigidbody>().linearVelocity.magnitude, Is.EqualTo(0f).Within(Tolerance));
        }

        [UnityTest]
        public IEnumerator AfterShoot_ShooterCannotRegrabForCooldown()
        {
            yield return AttachBall();
            _player.SetMoveInput(Vector2.zero);
            _player.BeginShoot();
            _player.ReleaseShoot();
            yield return null;

            Assert.That(_ball.TryAttach(_player), Is.False);

            yield return new WaitForSeconds(_config.Mochi.RegrabCooldown + 0.1f);
            Assert.That(_ball.TryAttach(_player), Is.True);
        }

        [UnityTest]
        public IEnumerator AfterShoot_OtherPlayerCanGrabImmediately()
        {
            yield return AttachBall();
            var other = TestWorld.CreatePlayer(_config, new Vector3(0f, 1f, 6f), Vector3.back);
            _player.BeginShoot();
            _player.ReleaseShoot();
            yield return null;

            Assert.That(_ball.TryAttach(other), Is.True);
            Assert.That(_ball.Holder, Is.SameAs(other));
        }

        // ---- 成長（REQ-012, REQ-013）----

        [UnityTest]
        public IEnumerator AbsorbingThreeRice_GrowsToLv2Scale()
        {
            yield return AttachBall();
            var baseScale = _ball.transform.localScale.x;

            for (var i = 0; i < 3; i++)
            {
                Assert.That(_ball.TryAbsorbRice(), Is.True);
            }

            yield return null;
            Assert.That(_ball.Level, Is.EqualTo(MochiLevel.Lv2));
            Assert.That(_ball.transform.localScale.x, Is.EqualTo(baseScale * 1.5f).Within(0.001f));
            Assert.That(_player.SpeedMultiplier, Is.EqualTo(0.9f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator ReleasingGrownBall_KeepsSizeAndResetsHolderSpeed()
        {
            yield return AttachBall();
            var baseScale = _ball.transform.localScale.x;
            for (var i = 0; i < 6; i++) _ball.TryAbsorbRice();

            _player.BeginShoot();
            _player.ReleaseShoot();
            yield return null;

            Assert.That(_ball.Level, Is.EqualTo(MochiLevel.Lv3));
            Assert.That(_ball.transform.localScale.x, Is.EqualTo(baseScale * 2.5f).Within(0.001f));
            Assert.That(_player.SpeedMultiplier, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator Lv2Shot_IsFasterByLevelMultiplier()
        {
            yield return AttachBall();
            for (var i = 0; i < 3; i++) _ball.TryAbsorbRice();

            _player.BeginShoot();
            _player.ReleaseShoot();
            yield return new WaitForFixedUpdate();

            var expected = _config.MochiBall.ShotSpeedByStage[0] * _config.MochiBall.ShotSpeedMultiplierByLevel[1];
            Assert.That(_ball.GetComponent<Rigidbody>().linearVelocity.magnitude, Is.EqualTo(expected).Within(Tolerance));
        }

        [UnityTest]
        public IEnumerator HeldBallTouchingRice_AbsorbsIt()
        {
            yield return AttachBall();
            var rice = Object.Instantiate(TestWorld.CreateRiceTemplate());
            rice.name = "MochiRice";
            rice.transform.position = _ball.transform.position + _player.transform.forward * 1.5f;
            rice.gameObject.SetActive(true);

            _player.SetMoveInput(new Vector2(_player.transform.forward.x, _player.transform.forward.z));
            yield return new WaitForSeconds(0.6f);

            Assert.That(_ball.RiceCount, Is.EqualTo(1));
            Assert.That(rice.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator LooseBallTouchingRice_DoesNotAbsorb()
        {
            _ball = TestWorld.CreateBall(_config, new Vector3(0f, 0.5f, 0f));
            var rice = Object.Instantiate(TestWorld.CreateRiceTemplate());
            rice.name = "MochiRice";
            rice.transform.position = new Vector3(1.5f, 0.5f, 0f);
            rice.gameObject.SetActive(true);
            yield return new WaitForFixedUpdate();

            _ball.GetComponent<Rigidbody>().linearVelocity = new Vector3(4f, 0f, 0f);
            yield return new WaitForSeconds(0.6f);

            Assert.That(_ball.RiceCount, Is.EqualTo(0));
            Assert.That(rice.gameObject.activeSelf, Is.True);
        }

        // ---- 落とす（REQ-032）----

        [UnityTest]
        public IEnumerator Drop_ReleasesWithPopAndNoRegrabBlock()
        {
            yield return AttachBall();
            for (var i = 0; i < 3; i++) _ball.TryAbsorbRice();

            _ball.Drop();
            yield return new WaitForFixedUpdate();

            Assert.That(_ball.IsHeld, Is.False);
            Assert.That(_player.HeldBall, Is.Null);
            Assert.That(_ball.Level, Is.EqualTo(MochiLevel.Lv2));
            Assert.That(_ball.GetComponent<Rigidbody>().linearVelocity.y, Is.GreaterThan(0f));
            Assert.That(_ball.TryAttach(_player), Is.True);
        }

        // ---- 設定 ----

        [UnityTest]
        public IEnumerator LooseBall_UsesDampingFromConfig()
        {
            _config.MochiBall.LinearDamping = 0.75f;
            _ball = TestWorld.CreateBall(_config, Vector3.zero);
            yield return null;

            Assert.That(_ball.GetComponent<Rigidbody>().linearDamping, Is.EqualTo(0.75f));
        }

        [UnityTest]
        public IEnumerator MissingConfig_LogsErrorAndDisablesItself()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("GameBalanceConfig"));
            _ball = TestWorld.CreateBall(null, Vector3.zero);
            yield return null;

            Assert.That(_ball.enabled, Is.False);
        }

        private IEnumerator SetUpPlayerAndBallAhead()
        {
            _player = TestWorld.CreatePlayer(_config, new Vector3(0f, 1f, 0f), Vector3.right);
            _ball = TestWorld.CreateBall(_config, new Vector3(1.5f, 0.5f, 0f));
            yield return new WaitForFixedUpdate();
        }

        private IEnumerator AttachBall()
        {
            yield return SetUpPlayerAndBallAhead();
            Assert.That(_ball.TryAttach(_player), Is.True);
            yield return new WaitForFixedUpdate();
        }
    }
}
