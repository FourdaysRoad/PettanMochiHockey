using System.Collections;
using System.Collections.Generic;
using MochiHockey.Core;
using MochiHockey.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MochiHockey.Tests.PlayMode
{
    /// <summary>
    /// TASK-0034：壁キック（REQ-028）と連鎖（REQ-029）をプレイヤーの操作につないだもの。
    /// リンクは 24 × 14（右の壁は x = 12）。テストでは壁のコライダーを置かず、Core のリンクの大きさで判定する。
    /// </summary>
    public class WallKickPlayTests
    {
        private const float Tolerance = 0.05f;
        private static readonly Vector3 NearRightWall = new Vector3(11.3f, 1f, 0f);

        private GameBalanceConfig _config;
        private readonly List<PlayerEvent> _events = new List<PlayerEvent>();

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            _events.Clear();
            PlayerController.AnyPlayerEvent += OnPlayerEvent;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            PlayerController.AnyPlayerEvent -= OnPlayerEvent;
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go.name is "Player" or "MochiBall" or "WorldGauges" || go.name.EndsWith("Gauge")
                    || go.name.EndsWith("DizzyStars") || go.name.EndsWith("WallKick"))
                {
                    Object.Destroy(go);
                }
            }

            Object.Destroy(_config);
            yield return null;
        }

        private void OnPlayerEvent(PlayerController player, PlayerEvent e) => _events.Add(e);

        // ---- 押し方 ----

        [UnityTest]
        public IEnumerator ShortPressNearWall_IsANormalDash()
        {
            var player = TestWorld.CreatePlayer(_config, NearRightWall, Vector3.left, Team.A);
            yield return null;

            player.PressDash();
            yield return null;
            player.ReleaseDash();
            yield return null;
            yield return null;

            Assert.That(_events, Does.Contain(PlayerEvent.Dashed));
            Assert.That(_events, Has.No.Member(PlayerEvent.WallKicked));
            Assert.That(player.WallKickPhase, Is.EqualTo(WallKickPhase.None));
        }

        [UnityTest]
        public IEnumerator PressAwayFromWall_DashesRightAway()
        {
            var player = TestWorld.CreatePlayer(_config, new Vector3(0f, 1f, 0f), Vector3.right, Team.A);
            yield return null;

            player.PressDash();
            yield return null;

            Assert.That(_events, Does.Contain(PlayerEvent.Dashed), "壁から遠ければ、離すのを待たずにダッシュ");
        }

        [UnityTest]
        public IEnumerator LongPressNearWall_ClingsToTheWallAndAims()
        {
            var player = TestWorld.CreatePlayer(_config, NearRightWall, Vector3.left, Team.A);
            yield return null;

            yield return HoldUntilAiming(player);

            Assert.That(player.WallKickPhase, Is.EqualTo(WallKickPhase.Aiming));
            Assert.That(player.transform.position.x, Is.EqualTo(12f - 0.5f).Within(Tolerance), "壁にぴったり付く");
            Assert.That(player.PlanarVelocity.magnitude, Is.LessThan(Tolerance), "止まっている");

            player.SetMoveInput(new Vector2(-1f, 1f).normalized);
            yield return null;
            Assert.That(player.WallKickAim.x, Is.EqualTo(-0.707f).Within(0.01f));
            Assert.That(player.WallKickAim.y, Is.EqualTo(0.707f).Within(0.01f));
        }

        // ---- 飛ぶ・連鎖 ----

        [UnityTest]
        public IEnumerator Release_FliesStraightAndClingsToTheOtherWall()
        {
            var player = TestWorld.CreatePlayer(_config, NearRightWall, Vector3.left, Team.A);
            yield return null;
            yield return HoldUntilAiming(player);

            player.SetMoveInput(Vector2.left);
            yield return null;
            player.ReleaseDash();
            player.SetMoveInput(Vector2.zero);
            yield return null;
            yield return new WaitForFixedUpdate();

            Assert.That(_events, Does.Contain(PlayerEvent.WallKicked));
            Assert.That(player.WallKickPhase, Is.EqualTo(WallKickPhase.Flying));
            Assert.That(player.WallKickChain, Is.EqualTo(1));
            Assert.That(player.GetComponent<Rigidbody>().linearVelocity.x, Is.EqualTo(-_config.WallKick.BaseSpeed).Within(0.1f));

            yield return WaitForPhase(player, WallKickPhase.Clinging, 3f);
            Assert.That(player.transform.position.x, Is.EqualTo(-12f + 0.5f).Within(Tolerance), "反対の壁に張り付く");
            Assert.That(player.transform.position.z, Is.EqualTo(0f).Within(Tolerance), "まっすぐ飛ぶ");
        }

        [UnityTest]
        public IEnumerator Chain_PressWhileClinging_KicksAgainFaster()
        {
            var player = TestWorld.CreatePlayer(_config, NearRightWall, Vector3.left, Team.A);
            yield return null;
            yield return KickAcross(player, Vector2.left);
            yield return WaitForPhase(player, WallKickPhase.Clinging, 3f);

            player.PressDash();
            yield return null;
            Assert.That(player.WallKickPhase, Is.EqualTo(WallKickPhase.Aiming), "張り付いている間は押しただけで狙える");

            player.SetMoveInput(Vector2.right);
            yield return null;
            player.ReleaseDash();
            player.SetMoveInput(Vector2.zero);
            yield return null;
            yield return new WaitForFixedUpdate();

            Assert.That(player.WallKickChain, Is.EqualTo(2));
            var expected = _config.WallKick.BaseSpeed * (1f + _config.WallKick.ChainSpeedBonus);
            Assert.That(player.GetComponent<Rigidbody>().linearVelocity.x, Is.EqualTo(expected).Within(0.1f));
        }

        [UnityTest]
        public IEnumerator Chain_NotKickingInTime_LetsGoOfTheWall()
        {
            var player = TestWorld.CreatePlayer(_config, NearRightWall, Vector3.left, Team.A);
            yield return null;
            yield return KickAcross(player, Vector2.left);
            yield return WaitForPhase(player, WallKickPhase.Clinging, 3f);

            yield return new WaitForSeconds(_config.WallKick.ChainWindow + 0.1f);

            Assert.That(player.WallKickPhase, Is.EqualTo(WallKickPhase.None));
            Assert.That(player.WallKickChain, Is.EqualTo(0));
            Assert.That(player.PlanarVelocity.magnitude, Is.LessThan(Tolerance), "離れたあとは止まっている");
        }

        [UnityTest]
        public IEnumerator Flying_CanSwingTheMallet()
        {
            var player = TestWorld.CreatePlayer(_config, NearRightWall, Vector3.left, Team.A);
            yield return null;
            yield return KickAcross(player, Vector2.left);
            Assert.That(player.WallKickPhase, Is.EqualTo(WallKickPhase.Flying));

            player.RequestSwing();

            Assert.That(player.IsSwinging, Is.True);
        }

        [UnityTest]
        public IEnumerator Flying_CannotPickUpTheBall()
        {
            var player = TestWorld.CreatePlayer(_config, NearRightWall, Vector3.left, Team.A);
            var ball = TestWorld.CreateBall(_config, new Vector3(-5f, 1f, 3f));
            yield return null;
            yield return KickAcross(player, Vector2.left);

            Assert.That(player.CanHoldBall, Is.False);
            Assert.That(ball.TryAttach(player), Is.False);
        }

        // ---- できないとき・やめるとき ----

        [UnityTest]
        public IEnumerator HoldingTheBall_LongPressIsJustADash()
        {
            var player = TestWorld.CreatePlayer(_config, NearRightWall, Vector3.left, Team.A);
            var ball = TestWorld.CreateBall(_config, NearRightWall + Vector3.left * 1.2f);
            yield return null;
            Assert.That(ball.TryAttach(player), Is.True);

            player.PressDash();
            yield return new WaitForSeconds(_config.WallKick.HoldTime + 0.1f);
            player.ReleaseDash();
            yield return null;

            Assert.That(player.WallKickPhase, Is.EqualTo(WallKickPhase.None));
            Assert.That(_events, Does.Contain(PlayerEvent.Dashed));
            Assert.That(_events, Has.No.Member(PlayerEvent.WallKicked));
        }

        [UnityTest]
        public IEnumerator HitWhileAiming_EndsTheWallKick()
        {
            var player = TestWorld.CreatePlayer(_config, NearRightWall, Vector3.left, Team.A);
            yield return null;
            yield return HoldUntilAiming(player);

            player.ReceiveHit(Vector3.left);
            yield return null;

            Assert.That(player.PettanState, Is.Not.EqualTo(PettanState.Normal), "狙っている間も叩かれる");
            Assert.That(player.WallKickPhase, Is.EqualTo(WallKickPhase.None));
            Assert.That(player.WallKickChain, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Kickoff_EndsTheWallKick()
        {
            var player = TestWorld.CreatePlayer(_config, NearRightWall, Vector3.left, Team.A);
            yield return null;
            yield return KickAcross(player, Vector2.left);

            player.ResetForKickoff(Vector3.up, Quaternion.identity);

            Assert.That(player.WallKickPhase, Is.EqualTo(WallKickPhase.None));
            Assert.That(player.WallKickChain, Is.EqualTo(0));
        }

        // ---- 表示 ----

        [UnityTest]
        public IEnumerator Arrow_ShowsWhileAimingAndPointsTheAim()
        {
            var player = TestWorld.CreatePlayer(_config, NearRightWall, Vector3.left, Team.A);
            var presenter = new GameObject("WorldGauges").AddComponent<WorldGaugePresenter>();
            yield return null;
            var view = presenter.WallKickOf(player);
            Assert.That(view, Is.Not.Null);
            Assert.That(view.ArrowVisible, Is.False);

            yield return HoldUntilAiming(player);
            player.SetMoveInput(new Vector2(-1f, -1f).normalized);
            yield return null;
            yield return null;

            Assert.That(view.ArrowVisible, Is.True);
            Assert.That(Vector2.Dot(view.ArrowDirection, new Vector2(-1f, -1f).normalized), Is.GreaterThan(0.99f));

            player.ReleaseDash();
            yield return null;
            yield return null;
            Assert.That(view.ArrowVisible, Is.False, "飛んだら消える");
        }

        [UnityTest]
        public IEnumerator ChainLabel_ShowsFromTheSecondKick()
        {
            var player = TestWorld.CreatePlayer(_config, NearRightWall, Vector3.left, Team.A);
            var presenter = new GameObject("WorldGauges").AddComponent<WorldGaugePresenter>();
            yield return null;
            var view = presenter.WallKickOf(player);

            yield return KickAcross(player, Vector2.left);
            yield return null;
            Assert.That(view.ChainVisible, Is.False, "1 回目は出さない");

            yield return WaitForPhase(player, WallKickPhase.Clinging, 3f);
            player.PressDash();
            yield return null;
            player.ReleaseDash();
            yield return null;
            yield return null;

            Assert.That(view.ChainVisible, Is.True);
            Assert.That(view.ChainText, Is.EqualTo("2 CHAIN!"));
        }

        // ---- 組み立て ----

        private IEnumerator HoldUntilAiming(PlayerController player)
        {
            player.PressDash();
            var limit = Time.time + _config.WallKick.HoldTime + 1f;
            while (player.WallKickPhase != WallKickPhase.Aiming && Time.time < limit)
            {
                yield return null;
            }

            Assert.That(player.WallKickPhase, Is.EqualTo(WallKickPhase.Aiming), "長押しで狙いに入らなかった");
            yield return new WaitForFixedUpdate();
        }

        private IEnumerator KickAcross(PlayerController player, Vector2 aim)
        {
            yield return HoldUntilAiming(player);
            player.SetMoveInput(aim);
            yield return null;
            player.ReleaseDash();
            player.SetMoveInput(Vector2.zero);
            yield return null;
            Assert.That(player.WallKickPhase, Is.EqualTo(WallKickPhase.Flying));
        }

        private static IEnumerator WaitForPhase(PlayerController player, WallKickPhase phase, float seconds)
        {
            var limit = Time.time + seconds;
            while (player.WallKickPhase != phase && Time.time < limit)
            {
                yield return null;
            }

            Assert.That(player.WallKickPhase, Is.EqualTo(phase));
            yield return new WaitForFixedUpdate();
        }
    }
}
