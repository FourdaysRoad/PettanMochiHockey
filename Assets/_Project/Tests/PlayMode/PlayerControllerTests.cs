using System.Collections;
using MochiHockey.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MochiHockey.Tests.PlayMode
{
    public class PlayerControllerTests
    {
        private const float Tolerance = 0.01f;

        private GameBalanceConfig _config;
        private GameObject _player;
        private PlayerController _controller;

        [SetUp]
        public void SetUp()
        {
            // 仕様書の値（最高速度 6 m/s、ダッシュ 35 / 最大 100）
            _config = ScriptableObject.CreateInstance<GameBalanceConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_player != null) Object.Destroy(_player);
            Object.Destroy(_config);
        }

        [UnityTest]
        public IEnumerator NoInput_StaysStill()
        {
            CreatePlayer(Vector3.zero);

            yield return new WaitForSeconds(0.3f);

            Assert.That(_player.transform.position.x, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(_player.transform.position.z, Is.EqualTo(0f).Within(Tolerance));
        }

        [UnityTest]
        public IEnumerator MoveInput_MovesTowardInputOnFloorPlane()
        {
            CreatePlayer(Vector3.zero);
            _controller.SetMoveInput(new Vector2(1f, 0f));

            yield return new WaitForSeconds(0.5f);

            Assert.That(_player.transform.position.x, Is.GreaterThan(0.5f));
            Assert.That(_player.transform.position.z, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(_controller.PlanarVelocity.x, Is.GreaterThan(0f));
        }

        [UnityTest]
        public IEnumerator MoveInputUp_MovesAlongPositiveZ()
        {
            CreatePlayer(Vector3.zero);
            _controller.SetMoveInput(new Vector2(0f, 1f));

            yield return new WaitForSeconds(0.5f);

            Assert.That(_player.transform.position.z, Is.GreaterThan(0.5f));
            Assert.That(_player.transform.position.x, Is.EqualTo(0f).Within(Tolerance));
        }

        [UnityTest]
        public IEnumerator MoveInput_TurnsToFaceMovement()
        {
            CreatePlayer(Vector3.zero);
            _controller.SetMoveInput(new Vector2(1f, 0f));

            yield return new WaitForSeconds(0.3f);

            Assert.That(Vector3.Dot(_player.transform.forward, Vector3.right), Is.GreaterThan(0.99f));
        }

        [UnityTest]
        public IEnumerator ReleasingInput_KeepsSlidingThenSlowsDown()
        {
            CreatePlayer(Vector3.zero);
            _controller.SetMoveInput(new Vector2(1f, 0f));
            yield return new WaitForSeconds(1f);
            var speedAtRelease = _controller.PlanarVelocity.magnitude;

            _controller.SetMoveInput(Vector2.zero);
            yield return new WaitForSeconds(0.3f);

            Assert.That(_controller.PlanarVelocity.x, Is.GreaterThan(0f));
            Assert.That(_controller.PlanarVelocity.magnitude, Is.LessThan(speedAtRelease));
        }

        [UnityTest]
        public IEnumerator Dash_ConsumesStaminaAndExceedsMaxSpeed()
        {
            CreatePlayer(Vector3.zero);
            _controller.SetMoveInput(new Vector2(1f, 0f));
            yield return null;

            _controller.RequestDash();
            yield return null;

            Assert.That(_controller.PlanarVelocity.magnitude, Is.GreaterThan(_config.Player.MaxSpeed));
            Assert.That(_controller.StaminaNormalized, Is.LessThan(1f));
        }

        [UnityTest]
        public IEnumerator Dash_WithoutEnoughStamina_DoesNothing()
        {
            _config.Player.DashCost = _config.Player.StaminaMax + 1f;
            CreatePlayer(Vector3.zero);
            yield return null;

            _controller.RequestDash();
            yield return null;

            Assert.That(_controller.PlanarVelocity.magnitude, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(_controller.StaminaNormalized, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator MissingConfig_LogsErrorAndDisablesItself()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("GameBalanceConfig"));
            CreatePlayer(Vector3.zero, withConfig: false);

            yield return null;

            Assert.That(_controller.enabled, Is.False);
        }

        private void CreatePlayer(Vector3 position, bool withConfig = true)
        {
            _player = new GameObject("Player");
            _player.SetActive(false);
            _player.transform.position = position;
            _player.AddComponent<CapsuleCollider>();
            var body = _player.AddComponent<Rigidbody>();
            body.useGravity = false;
            _controller = _player.AddComponent<PlayerController>();
            if (withConfig)
            {
                _controller.Config = _config;
            }

            _player.SetActive(true);
        }
    }
}
