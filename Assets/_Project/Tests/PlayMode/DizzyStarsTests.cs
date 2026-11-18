using System.Collections;
using MochiHockey.Core;
using MochiHockey.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MochiHockey.Tests.PlayMode
{
    /// <summary>TASK-0027：ピヨり（復帰直後の無敵中、頭の上で星が回る）。</summary>
    public class DizzyStarsTests
    {
        private GameBalanceConfig _config;
        private PlayerController _attacker;
        private PlayerController _target;
        private WorldGaugePresenter _presenter;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            _attacker = TestWorld.CreatePlayer(_config, new Vector3(6.5f, 1f, 0f), Vector3.right, Team.A);
            _target = TestWorld.CreatePlayer(_config, new Vector3(8f, 1f, 0f), Vector3.left, Team.B);
            _presenter = new GameObject("WorldGauges").AddComponent<WorldGaugePresenter>();
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go.name is "Player" or "WorldGauges" || go.name.EndsWith("Gauge") || go.name.EndsWith("DizzyStars"))
                {
                    Object.Destroy(go);
                }
            }

            Object.Destroy(_config);
            yield return null;
        }

        [Test]
        public void NotDizzyWhileNormal()
        {
            var stars = _presenter.StarsOf(_target);

            Assert.That(stars, Is.Not.Null);
            Assert.That(stars.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator Hit_StaysHiddenUntilRecoverThenSpinsAboveHeadWhileInvincible()
        {
            _attacker.RequestSwing();
            yield return new WaitForSeconds(1f);
            Assert.That(_target.PettanState, Is.Not.EqualTo(PettanState.Normal));
            Assert.That(_presenter.StarsOf(_target).gameObject.activeSelf, Is.False, "貼り付き中は出さない");

            while (_target.PettanState != PettanState.Invincible)
            {
                yield return null;
            }

            yield return null;
            var stars = _presenter.StarsOf(_target);
            Assert.That(stars.gameObject.activeSelf, Is.True);
            Assert.That(stars.transform.position.y, Is.GreaterThan(_target.GetComponent<Collider>().bounds.max.y));
            Assert.That(stars.transform.position.x, Is.EqualTo(_target.transform.position.x).Within(0.05f));

            var angle = stars.transform.eulerAngles.y;
            yield return new WaitForSeconds(0.2f);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(angle, stars.transform.eulerAngles.y)), Is.GreaterThan(5f), "星が回っていない");
        }

        [UnityTest]
        public IEnumerator BackToNormal_HidesStars()
        {
            _attacker.RequestSwing();
            while (_target.PettanState != PettanState.Invincible)
            {
                yield return null;
            }

            while (_target.PettanState != PettanState.Normal)
            {
                yield return null;
            }

            yield return null;
            Assert.That(_presenter.StarsOf(_target).gameObject.activeSelf, Is.False);
        }

        [Test]
        public void DizzyStars_HasThreeStars()
        {
            Assert.That(_presenter.StarsOf(_target).StarCount, Is.EqualTo(3));
        }
    }
}
