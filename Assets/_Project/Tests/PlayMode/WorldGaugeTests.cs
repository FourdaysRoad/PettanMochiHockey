using System.Collections;
using MochiHockey.Core;
using MochiHockey.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MochiHockey.Tests.PlayMode
{
    /// <summary>TASK-0024：餅の頭上のゲージと、プレイヤーの足元のリング・スタミナ・チャージ。</summary>
    public class WorldGaugeTests
    {
        private GameBalanceConfig _config;
        private Camera _camera;
        private PlayerController _p1;
        private PlayerController _p2;
        private MochiBall _ball;
        private WorldGaugePresenter _presenter;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            _config.MochiBall.LinearDamping = 0f;
            _camera = new GameObject("GaugeCamera").AddComponent<Camera>();
            _camera.tag = "MainCamera";
            _camera.transform.SetPositionAndRotation(new Vector3(0f, 18f, -10f), Quaternion.Euler(60f, 0f, 0f));

            _p1 = TestWorld.CreatePlayer(_config, new Vector3(-4f, 1f, 0f), Vector3.right, Team.A);
            _p1.Number = 1;
            _p2 = TestWorld.CreatePlayer(_config, new Vector3(4f, 1f, 0f), Vector3.left, Team.B);
            _p2.Number = 2;
            _ball = TestWorld.CreateBall(_config, new Vector3(0f, 0.5f, 0f));
            _presenter = new GameObject("WorldGauges").AddComponent<WorldGaugePresenter>();
            _presenter.Ball = _ball;
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go.name is "Player" or "MochiBall" or "WorldGauges" or "GaugeCamera" || go.name.EndsWith("Gauge"))
                {
                    Object.Destroy(go);
                }
            }

            Object.Destroy(_config);

            // Destroy はフレームの終わりなので、次のテストが古い餅やプレイヤーを見つけないよう 1 フレーム待つ
            yield return null;
        }

        // ---- 餅の頭上 ----

        [Test]
        public void BallGauge_ShowsLevelAndMultiplierAboveBall()
        {
            var gauge = _presenter.BallGauge;

            Assert.That(gauge, Is.Not.Null);
            Assert.That(gauge.LevelText.text, Is.EqualTo("Lv.1"));
            Assert.That(gauge.MultiplierText.text, Is.EqualTo("x1"));
            Assert.That(gauge.transform.position.y, Is.GreaterThan(_ball.transform.position.y));
            Assert.That(gauge.GetComponent<Canvas>().renderMode, Is.EqualTo(RenderMode.WorldSpace));
        }

        [UnityTest]
        public IEnumerator BallGauge_FollowsGrowth()
        {
            Assert.That(_ball.TryAttach(_p1), Is.True);
            _ball.TryAbsorbRice();
            yield return null;
            Assert.That(_presenter.BallGauge.GrowthFill.fillAmount, Is.EqualTo(1f / 3f).Within(0.01f));

            _ball.TryAbsorbRice();
            _ball.TryAbsorbRice();
            yield return null;

            Assert.That(_presenter.BallGauge.LevelText.text, Is.EqualTo("Lv.2"));
            Assert.That(_presenter.BallGauge.MultiplierText.text, Is.EqualTo("x2"));
            Assert.That(_presenter.BallGauge.GrowthFill.fillAmount, Is.EqualTo(0f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator BallGauge_FacesCamera()
        {
            yield return null;

            var gauge = _presenter.BallGauge.transform;
            Assert.That(Vector3.Dot(gauge.forward, _camera.transform.forward), Is.GreaterThan(0.99f));
        }

        [UnityTest]
        public IEnumerator BallGauge_HidesWhileBallIsHidden()
        {
            _ball.Hide();
            yield return null;
            Assert.That(_presenter.BallGauge.gameObject.activeSelf, Is.False);

            _ball.ResetTo(new Vector3(0f, 0.5f, 0f));
            yield return null;
            Assert.That(_presenter.BallGauge.gameObject.activeSelf, Is.True);
        }

        // ---- 足元 ----

        [Test]
        public void PlayerGauges_ShowNumberAndColorPerPlayer()
        {
            var g1 = _presenter.GaugeOf(_p1);
            var g2 = _presenter.GaugeOf(_p2);

            Assert.That(g1.NumberText.text, Is.EqualTo("1P"));
            Assert.That(g2.NumberText.text, Is.EqualTo("2P"));
            Assert.That(g1.Ring.color, Is.EqualTo(PlayerColors.For(1)));
            Assert.That(g2.Ring.color, Is.EqualTo(PlayerColors.For(2)));
        }

        [UnityTest]
        public IEnumerator PlayerGauge_FollowsPlayerOnFloor()
        {
            _p1.SetMoveInput(Vector2.up);
            yield return new WaitForSeconds(0.4f);

            // テストはゲージが追いかける前（LateUpdate の前）に見ることがあるので、1 フレームで進む分のずれは許す
            // （CI のようにフレームが重いと、1 フレームで 0.1 m 以上進む）
            var gauge = _presenter.GaugeOf(_p1).transform.position;
            var oneFrame = _p1.PlanarVelocity.magnitude * (Time.deltaTime + Time.fixedDeltaTime);
            Assert.That(gauge.x, Is.EqualTo(_p1.transform.position.x).Within(0.05f + oneFrame));
            Assert.That(gauge.z, Is.EqualTo(_p1.transform.position.z).Within(0.05f + oneFrame));
            Assert.That(gauge.y, Is.LessThan(0.2f));
        }

        [UnityTest]
        public IEnumerator PlayerGauge_StaminaDropsAfterDash()
        {
            Assert.That(_presenter.GaugeOf(_p1).StaminaFill.fillAmount, Is.EqualTo(1f));

            _p1.SetMoveInput(Vector2.up);
            _p1.RequestDash();
            yield return null;
            yield return null;

            Assert.That(_presenter.GaugeOf(_p1).StaminaFill.fillAmount, Is.LessThan(1f));
        }

        [UnityTest]
        public IEnumerator PlayerGauge_ChargeShowsOnlyWhileCharging()
        {
            var gauge = _presenter.GaugeOf(_p1);
            Assert.That(gauge.ChargeBar.activeSelf, Is.False);

            _p1.BeginShoot();
            yield return new WaitForSeconds(0.3f);
            Assert.That(gauge.ChargeBar.activeSelf, Is.True);
            Assert.That(gauge.ChargeFill.fillAmount, Is.GreaterThan(0.1f));

            _p1.ReleaseShoot();
            yield return null;
            Assert.That(gauge.ChargeBar.activeSelf, Is.False);
        }
    }
}
