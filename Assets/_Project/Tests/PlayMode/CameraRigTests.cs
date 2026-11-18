using System.Collections;
using System.Linq;
using MochiHockey.Core;
using MochiHockey.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MochiHockey.Tests.PlayMode
{
    /// <summary>TASK-0025：全員と餅が画面に収まるカメラと、ズームの範囲。</summary>
    public class CameraRigTests
    {
        private GameBalanceConfig _config;
        private Camera _camera;
        private CameraRig _rig;
        private MochiBall _ball;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            _config.Camera.Damping = 0f;
            _config.MochiBall.LinearDamping = 0f;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go.name is "Player" or "MochiBall" or "RigCamera" or "CameraTargets" or "MatchCamera")
                {
                    Object.Destroy(go);
                }
            }

            Object.Destroy(_config);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayersInOppositeCorners_AreAllOnScreen()
        {
            var players = new[]
            {
                TestWorld.CreatePlayer(_config, new Vector3(-11f, 1f, 6f), Vector3.right, Team.A),
                TestWorld.CreatePlayer(_config, new Vector3(11f, 1f, -6f), Vector3.left, Team.B),
                TestWorld.CreatePlayer(_config, new Vector3(-11f, 1f, -6f), Vector3.right, Team.A),
                TestWorld.CreatePlayer(_config, new Vector3(11f, 1f, 6f), Vector3.left, Team.B),
            };
            yield return CreateRig();
            yield return new WaitForSeconds(0.5f);

            foreach (var target in players.Select(p => p.transform).Append(_ball.transform))
            {
                var v = _camera.WorldToViewportPoint(target.position);
                Assert.That(v.z, Is.GreaterThan(0f), $"{target.name} がカメラの後ろ");
                Assert.That(v.x, Is.InRange(0f, 1f), $"{target.name} が画面の左右からはみ出している");
                Assert.That(v.y, Is.InRange(0f, 1f), $"{target.name} が画面の上下からはみ出している");
            }

            Assert.That(DistanceToGroup(), Is.LessThanOrEqualTo(_config.Camera.MaxDistance + 0.1f));
        }

        [UnityTest]
        public IEnumerator PlayersBunchedTogether_DoNotZoomInPastMinimum()
        {
            TestWorld.CreatePlayer(_config, new Vector3(-0.5f, 1f, 0f), Vector3.right, Team.A);
            TestWorld.CreatePlayer(_config, new Vector3(0.5f, 1f, 0f), Vector3.left, Team.B);
            yield return CreateRig();
            yield return new WaitForSeconds(0.5f);

            Assert.That(DistanceToGroup(), Is.GreaterThanOrEqualTo(_config.Camera.MinDistance - 0.1f));
        }

        [UnityTest]
        public IEnumerator Camera_LooksDownAtConfiguredAngle()
        {
            TestWorld.CreatePlayer(_config, new Vector3(-4f, 1f, 0f), Vector3.right, Team.A);
            yield return CreateRig();
            yield return new WaitForSeconds(0.2f);

            Assert.That(_camera.transform.eulerAngles.x, Is.EqualTo(_config.Camera.Pitch).Within(0.5f));
        }

        [UnityTest]
        public IEnumerator HiddenBall_IsDroppedFromFramingAndAddedBackWhenRespawned()
        {
            TestWorld.CreatePlayer(_config, new Vector3(-4f, 1f, 0f), Vector3.right, Team.A);
            yield return CreateRig();

            Assert.That(_rig.IsFraming(_ball.transform), Is.True);

            _ball.Hide();
            yield return null;
            Assert.That(_rig.IsFraming(_ball.transform), Is.False);

            _ball.ResetTo(Vector3.up * 0.5f);
            yield return null;
            Assert.That(_rig.IsFraming(_ball.transform), Is.True);
        }

        [UnityTest]
        public IEnumerator PlayerSpawnedLater_IsAddedToFraming()
        {
            TestWorld.CreatePlayer(_config, new Vector3(-4f, 1f, 0f), Vector3.right, Team.A);
            yield return CreateRig();

            var late = TestWorld.CreatePlayer(_config, new Vector3(4f, 1f, 0f), Vector3.left, Team.B);
            yield return null;

            Assert.That(_rig.IsFraming(late.transform), Is.True);
        }

        private IEnumerator CreateRig()
        {
            _ball = TestWorld.CreateBall(_config, new Vector3(0f, 0.5f, 0f));
            var go = new GameObject("RigCamera");
            go.SetActive(false);
            go.tag = "MainCamera";
            go.transform.SetPositionAndRotation(new Vector3(0f, 18f, -10f), Quaternion.Euler(60f, 0f, 0f));
            _camera = go.AddComponent<Camera>();
            _rig = go.AddComponent<CameraRig>();
            _rig.Config = _config;
            go.SetActive(true);
            yield return null;
            yield return null;
        }

        /// <summary>カメラから、映している全員の真ん中までの距離。</summary>
        private float DistanceToGroup()
        {
            return Vector3.Distance(_camera.transform.position, _rig.TargetGroup.transform.position);
        }
    }
}
