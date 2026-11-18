using System.Collections;
using System.Linq;
using MochiHockey.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MochiHockey.Tests.PlayMode
{
    public class MochiRiceSpawnerTests
    {
        private GameBalanceConfig _config;
        private MochiRice _template;
        private MochiRiceSpawner _spawner;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            _template = TestWorld.CreateRiceTemplate();
        }

        [TearDown]
        public void TearDown()
        {
            if (_spawner != null) Object.Destroy(_spawner.gameObject);
            Object.Destroy(_template.gameObject);
            Object.Destroy(_config);
        }

        [UnityTest]
        public IEnumerator Start_SpawnsMinCount()
        {
            CreateSpawner();

            yield return null;
            yield return null;

            Assert.That(_spawner.ActiveCount, Is.EqualTo(_config.Rice.MinCount));
        }

        [UnityTest]
        public IEnumerator OverTime_RefillsUpToMaxAndStops()
        {
            _config.Rice.RefillInterval = 0.05f;
            CreateSpawner();

            yield return new WaitForSeconds(1f);

            Assert.That(_spawner.ActiveCount, Is.EqualTo(_config.Rice.MaxCount));
        }

        [UnityTest]
        public IEnumerator SpawnedRice_IsInsideRinkAwayFromWallsAtSpawnerHeight()
        {
            _config.Rice.RefillInterval = 0.05f;
            CreateSpawner();
            yield return new WaitForSeconds(1f);

            var halfX = _config.Rink.Size.x / 2f - _config.Rice.SpawnMargin;
            var halfZ = _config.Rink.Size.y / 2f - _config.Rice.SpawnMargin;
            foreach (var rice in ActiveRice())
            {
                var p = rice.transform.position;
                Assert.That(Mathf.Abs(p.x), Is.LessThanOrEqualTo(halfX + 0.001f));
                Assert.That(Mathf.Abs(p.z), Is.LessThanOrEqualTo(halfZ + 0.001f));
                Assert.That(p.y, Is.EqualTo(_spawner.transform.position.y).Within(0.001f));
            }
        }

        [UnityTest]
        public IEnumerator SpawnedRice_IsNeverInsideGoal()
        {
            // 右半分を覆う大きなゴール
            var goal = TestWorld.CreateGoal(new Vector3(6f, 0.5f, 0f), MochiHockey.Core.Team.B);
            goal.transform.localScale = new Vector3(12f, 2f, 20f);
            Physics.SyncTransforms(); // 大きさの変更をコライダーの範囲にすぐ反映する
            _config.Rice.RefillInterval = 0.05f;
            CreateSpawner();
            yield return new WaitForSeconds(1f);

            foreach (var rice in ActiveRice())
            {
                Assert.That(goal.ContainsOnFloor(rice.transform.position), Is.False);
            }

            Object.Destroy(goal.gameObject);
        }

        [UnityTest]
        public IEnumerator AbsorbedRice_ReturnsToPoolAndIsReused()
        {
            CreateSpawner();
            yield return null;
            yield return null;
            var first = ActiveRice().First();

            _spawner.Release(first);
            Assert.That(_spawner.ActiveCount, Is.EqualTo(_config.Rice.MinCount - 1));
            Assert.That(first.gameObject.activeSelf, Is.False);

            yield return null; // 最小数を下回ったのですぐ補充される
            Assert.That(_spawner.ActiveCount, Is.EqualTo(_config.Rice.MinCount));
            Assert.That(_spawner.PoolSize, Is.EqualTo(_config.Rice.MinCount));
        }

        private void CreateSpawner()
        {
            var go = new GameObject("Spawner");
            go.SetActive(false);
            go.transform.position = new Vector3(0f, 0.4f, 0f);
            _spawner = go.AddComponent<MochiRiceSpawner>();
            _spawner.Config = _config;
            _spawner.RicePrefab = _template;
            go.SetActive(true);
        }

        private MochiRice[] ActiveRice()
        {
            return Object.FindObjectsByType<MochiRice>(FindObjectsSortMode.None)
                .Where(r => r.gameObject.activeInHierarchy && r != _template)
                .ToArray();
        }
    }
}
