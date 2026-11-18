using System.Collections;
using MochiHockey.Core;
using MochiHockey.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MochiHockey.Tests.PlayMode
{
    /// <summary>TASK-0020：ゴール・キックオフ・再出現・ポーズ・試合終了・Lv.3 シュートの巻き込み。</summary>
    public class MatchManagerTests
    {
        private const float Tolerance = 0.05f;

        // チーム A は左（x = -10 のゴールを守る）、チーム B は右（x = 10 のゴールを守る）
        private static readonly Vector3 Center = new Vector3(0f, 0.5f, 0f);
        private static readonly Vector3 P1Start = new Vector3(-4f, 1f, 0f);
        private static readonly Vector3 P2Start = new Vector3(4f, 1f, 0f);

        private GameBalanceConfig _config;
        private PlayerController _p1;
        private PlayerController _p2;
        private MochiBall _ball;
        private MatchManager _manager;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            _config.Match.KickoffCountdown = 0.2f;
            _config.MochiBall.LinearDamping = 0f;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go.name is "Player" or "MochiBall" or "Goal" or "MatchManager")
                {
                    Object.Destroy(go);
                }
            }

            Object.Destroy(_config);
        }

        // ---- キックオフ（REQ-005）----

        [UnityTest]
        public IEnumerator Kickoff_LocksPlayersUntilCountdownEnds()
        {
            yield return CreateMatch();
            Assert.That(_manager.Rules.Phase, Is.EqualTo(MatchPhase.Kickoff));

            _p1.SetMoveInput(Vector2.up);
            yield return new WaitForSeconds(0.1f);
            Assert.That(_p1.transform.position.z, Is.EqualTo(P1Start.z).Within(Tolerance));

            yield return new WaitForSeconds(0.5f);
            Assert.That(_manager.Rules.Phase, Is.EqualTo(MatchPhase.Playing));
            Assert.That(_p1.transform.position.z, Is.GreaterThan(P1Start.z + 0.3f));
        }

        [UnityTest]
        public IEnumerator Restart_PutsPlayersAndBallBackToStart()
        {
            yield return CreateMatchAndWaitForPlaying();
            _p1.SetMoveInput(Vector2.up);
            yield return new WaitForSeconds(0.5f);
            _p1.SetMoveInput(Vector2.zero);
            Assert.That(_ball.TryAttach(_p2), Is.True);

            _manager.Restart();
            yield return new WaitForFixedUpdate();

            Assert.That(_manager.Rules.Phase, Is.EqualTo(MatchPhase.Kickoff));
            Assert.That(Vector3.Distance(_p1.transform.position, P1Start), Is.LessThan(Tolerance));
            Assert.That(_ball.IsHeld, Is.False);
            Assert.That(Vector3.Distance(_ball.transform.position, Center), Is.LessThan(Tolerance));
        }

        // ---- ゴール（REQ-003, REQ-004）----

        [UnityTest]
        public IEnumerator BallIntoGoal_ScoresHidesBallAndRespawnsLv1AtCenterAfterDelay()
        {
            yield return CreateMatchAndWaitForPlaying();
            Assert.That(_ball.TryAttach(_p1), Is.True);
            for (var i = 0; i < 3; i++) _ball.TryAbsorbRice();
            _ball.Drop();
            Place(_ball, new Vector3(7f, 0.5f, 0f), new Vector3(10f, 0f, 0f)); // B のゴールへ

            yield return new WaitForSeconds(0.4f);
            Assert.That(_manager.Rules.GetScore(Team.A), Is.EqualTo(2));
            Assert.That(_manager.Rules.Phase, Is.EqualTo(MatchPhase.MochiRespawning));
            Assert.That(_ball.gameObject.activeSelf, Is.False);
            var p2Before = _p2.transform.position;

            yield return new WaitForSeconds(_config.Match.MochiRespawnDelay);
            Assert.That(_ball.gameObject.activeSelf, Is.True);
            Assert.That(_ball.Level, Is.EqualTo(MochiLevel.Lv1));
            Assert.That(_ball.RiceCount, Is.EqualTo(0));
            Assert.That(Vector3.Distance(_ball.transform.position, Center), Is.LessThan(Tolerance));
            Assert.That(_manager.Rules.Phase, Is.EqualTo(MatchPhase.Playing));
            Assert.That(Vector3.Distance(_p2.transform.position, p2Before), Is.LessThan(Tolerance));
        }

        [UnityTest]
        public IEnumerator OwnGoal_ScoresForOpponent()
        {
            yield return CreateMatchAndWaitForPlaying();
            Place(_ball, new Vector3(-7f, 0.5f, 0f), new Vector3(-10f, 0f, 0f)); // A のゴールへ

            yield return new WaitForSeconds(0.4f);

            Assert.That(_manager.Rules.GetScore(Team.B), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator HeldBallWalkedIntoGoal_DoesNotScore()
        {
            yield return CreateMatchAndWaitForPlaying();
            Place(_p1, new Vector3(7f, 1f, 0f), Vector3.right);
            Place(_ball, new Vector3(8.5f, 0.5f, 0f), Vector3.zero);
            yield return new WaitForFixedUpdate();
            Assert.That(_ball.TryAttach(_p1), Is.True);

            _p1.SetMoveInput(Vector2.right);
            yield return new WaitForSeconds(0.5f);

            Assert.That(_manager.Rules.GetScore(Team.A), Is.EqualTo(0));
            Assert.That(_ball.IsHeld, Is.True);
        }

        // ---- ポーズ ----

        [UnityTest]
        public IEnumerator Pause_StopsMatchClockAndResumeContinues()
        {
            yield return CreateMatchAndWaitForPlaying();

            _manager.SetPaused(true);
            var frozen = _manager.Rules.RemainingTime;
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(_manager.IsPaused, Is.True);
            Assert.That(_manager.Rules.RemainingTime, Is.EqualTo(frozen));

            _manager.SetPaused(false);
            yield return new WaitForSeconds(0.3f);
            Assert.That(_manager.Rules.RemainingTime, Is.LessThan(frozen));
        }

        [UnityTest]
        public IEnumerator TogglePause_SwitchesBackAndForth()
        {
            yield return CreateMatchAndWaitForPlaying();

            _manager.TogglePause();
            Assert.That(_manager.IsPaused, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f));

            _manager.TogglePause();
            Assert.That(_manager.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        // ---- 試合終了 ----

        [UnityTest]
        public IEnumerator TimeUp_FinishesAndLocksPlayers()
        {
            _config.Match.MatchDuration = 0.5f;
            yield return CreateMatchAndWaitForPlaying();
            Place(_ball, new Vector3(7f, 0.5f, 0f), new Vector3(10f, 0f, 0f));
            yield return new WaitForSeconds(0.8f);

            Assert.That(_manager.Rules.Phase, Is.EqualTo(MatchPhase.Finished));
            Assert.That(_manager.Rules.Winner, Is.EqualTo(Team.A));
            Assert.That(_p1.Locked, Is.True);
            Assert.That(_p2.Locked, Is.True);
        }

        [UnityTest]
        public IEnumerator SuddenDeath_GoalEndsMatch()
        {
            _config.Match.MatchDuration = 0.3f;
            yield return CreateMatchAndWaitForPlaying();
            yield return new WaitForSeconds(0.4f);
            Assert.That(_manager.Rules.Phase, Is.EqualTo(MatchPhase.SuddenDeath));

            Place(_ball, new Vector3(-7f, 0.5f, 0f), new Vector3(-10f, 0f, 0f));
            yield return new WaitForSeconds(0.4f);

            Assert.That(_manager.Rules.Phase, Is.EqualTo(MatchPhase.Finished));
            Assert.That(_manager.Rules.Winner, Is.EqualTo(Team.B));
        }

        // ---- Lv.3 シュートの巻き込み（REQ-015）----

        [UnityTest]
        public IEnumerator Lv3Shot_PiercesEnemyInPathAndLaunchesThem()
        {
            yield return CreateMatchAndWaitForPlaying();
            Place(_p1, new Vector3(-6f, 1f, 3f), Vector3.right);
            Place(_p2, new Vector3(0f, 1f, 3f), Vector3.left);
            Place(_ball, new Vector3(-4.5f, 0.5f, 3f), Vector3.zero);
            yield return new WaitForFixedUpdate();
            Assert.That(_ball.TryAttach(_p1), Is.True);
            for (var i = 0; i < 6; i++) _ball.TryAbsorbRice();
            Assert.That(_ball.Level, Is.EqualTo(MochiLevel.Lv3));
            yield return new WaitForFixedUpdate();

            _p1.BeginShoot();
            _p1.ReleaseShoot();
            yield return new WaitForSeconds(0.5f);

            Assert.That(_p2.PettanState, Is.Not.EqualTo(PettanState.Normal));
            Assert.That(_ball.transform.position.x, Is.GreaterThan(1f), "餅が敵を貫通して進んでいない");
        }

        [UnityTest]
        public IEnumerator Lv1Shot_DoesNotLaunchEnemy()
        {
            yield return CreateMatchAndWaitForPlaying();
            Place(_p1, new Vector3(-6f, 1f, 3f), Vector3.right);
            Place(_p2, new Vector3(0f, 1f, 3f), Vector3.left);
            Place(_ball, new Vector3(-4.5f, 0.5f, 3f), Vector3.zero);
            yield return new WaitForFixedUpdate();
            Assert.That(_ball.TryAttach(_p1), Is.True);
            yield return new WaitForFixedUpdate();

            _p1.BeginShoot();
            _p1.ReleaseShoot();
            yield return new WaitForSeconds(0.5f);

            Assert.That(_p2.PettanState, Is.EqualTo(PettanState.Normal));
        }

        private IEnumerator CreateMatch()
        {
            _p1 = TestWorld.CreatePlayer(_config, P1Start, Vector3.right, Team.A);
            _p2 = TestWorld.CreatePlayer(_config, P2Start, Vector3.left, Team.B);
            _ball = TestWorld.CreateBall(_config, Center);
            TestWorld.CreateGoal(new Vector3(-10f, 1f, 0f), Team.A);
            TestWorld.CreateGoal(new Vector3(10f, 1f, 0f), Team.B);
            _manager = TestWorld.CreateMatchManager(_config, _ball);
            yield return null;
        }

        private IEnumerator CreateMatchAndWaitForPlaying()
        {
            yield return CreateMatch();
            yield return new WaitForSeconds(_config.Match.KickoffCountdown + 0.1f);
            Assert.That(_manager.Rules.Phase, Is.EqualTo(MatchPhase.Playing));
        }

        private static void Place(PlayerController player, Vector3 position, Vector3 facing)
        {
            var body = player.GetComponent<Rigidbody>();
            var rotation = Quaternion.LookRotation(facing, Vector3.up);
            body.position = position;
            body.rotation = rotation;
            player.transform.SetPositionAndRotation(position, rotation);
            body.linearVelocity = Vector3.zero;
        }

        private static void Place(MochiBall ball, Vector3 position, Vector3 velocity)
        {
            var body = ball.GetComponent<Rigidbody>();
            body.position = position;
            ball.transform.position = position;
            body.linearVelocity = velocity;
        }
    }
}
