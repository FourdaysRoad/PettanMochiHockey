using System.Collections;
using MochiHockey.Core;
using MochiHockey.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MochiHockey.Tests.PlayMode
{
    /// <summary>TASK-0031：毎フレームシーンを探さずに済むよう、今いるプレイヤーの名簿（PlayerController.Active）を持つ。</summary>
    public class ActivePlayersTests
    {
        private GameBalanceConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<GameBalanceConfig>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go.name is "Player")
                {
                    Object.Destroy(go);
                }
            }

            Object.Destroy(_config);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Active_ListsPlayersWhileTheyExist()
        {
            var a = TestWorld.CreatePlayer(_config, new Vector3(-2f, 1f, 0f), Vector3.right, Team.A);
            var b = TestWorld.CreatePlayer(_config, new Vector3(2f, 1f, 0f), Vector3.left, Team.B);
            yield return null;

            Assert.That(PlayerController.Active, Does.Contain(a));
            Assert.That(PlayerController.Active, Does.Contain(b));

            Object.Destroy(a.gameObject);
            yield return null;

            Assert.That(PlayerController.Active, Has.No.Member(a));
            Assert.That(PlayerController.Active, Does.Contain(b));
        }

        [UnityTest]
        public IEnumerator Active_LeavesOutDisabledPlayers()
        {
            var a = TestWorld.CreatePlayer(_config, Vector3.zero, Vector3.right, Team.A);
            yield return null;

            a.gameObject.SetActive(false);
            Assert.That(PlayerController.Active, Has.No.Member(a));

            a.gameObject.SetActive(true);
            Assert.That(PlayerController.Active, Does.Contain(a));
        }

        [UnityTest]
        public IEnumerator Active_LeavesOutPlayersWithoutConfig()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Player";
            go.SetActive(false);
            go.AddComponent<Rigidbody>();
            var player = go.AddComponent<PlayerController>();
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("GameBalanceConfig is not assigned"));
            go.SetActive(true);
            yield return null;

            Assert.That(PlayerController.Active, Has.No.Member(player));
        }
    }
}
