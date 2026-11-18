using System.Collections;
using MochiHockey.Core;
using MochiHockey.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace MochiHockey.Tests.PlayMode
{
    /// <summary>TASK-0023：HUD（スコア・時間・READY・GOAL!）、ポーズメニュー、リザルト。</summary>
    public class HudPresenterTests
    {
        private GameBalanceConfig _config;
        private MochiBall _ball;
        private MatchManager _manager;
        private HudPresenter _hud;
        private VisualElement _root;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            _config.Match.KickoffCountdown = 0.2f;
            _config.Match.ResultInputDelay = 0f;
            _config.MochiBall.LinearDamping = 0f;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            MatchSession.Clear();
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go.name is "Player" or "MochiBall" or "Goal" or "MatchManager" or "Hud")
                {
                    Object.Destroy(go);
                }
            }

            Object.Destroy(_config);
            if (SceneManager.GetActiveScene().name == "Menu")
            {
                var empty = SceneManager.CreateScene("EmptyAfterHudPresenterTests");
                SceneManager.SetActiveScene(empty);
                yield return SceneManager.UnloadSceneAsync("Menu");
            }
        }

        [UnityTest]
        public IEnumerator Kickoff_ShowsReadyThenTimerCountsDown()
        {
            yield return CreateMatchWithHud();

            Assert.That(IsShown("kickoff"), Is.True);
            Assert.That(Text("timer"), Is.EqualTo("3:00"));
            Assert.That(Text("score-a"), Is.EqualTo("0"));

            yield return new WaitForSeconds(_config.Match.KickoffCountdown + 1.1f);
            Assert.That(IsShown("kickoff"), Is.False);
            Assert.That(Text("timer"), Is.EqualTo("2:58"));
        }

        [UnityTest]
        public IEnumerator Goal_UpdatesScoreAndShowsGoalUntilRespawn()
        {
            yield return CreateMatchWithHud();
            yield return WaitForPlaying();

            ShootInto(new Vector3(10f, 0f, 0f)); // B のゴールへ → A に 1 点
            yield return new WaitForSeconds(0.4f);

            Assert.That(Text("score-a"), Is.EqualTo("1"));
            Assert.That(IsShown("goal-panel"), Is.True);
            Assert.That(Text("goal-text"), Is.EqualTo(UiText.Goal));
            Assert.That(Text("goal-detail"), Is.EqualTo("+1  " + UiText.TeamNameA));

            yield return new WaitForSeconds(_config.Match.MochiRespawnDelay);
            Assert.That(IsShown("goal-panel"), Is.False);
        }

        [UnityTest]
        public IEnumerator Pause_ShowsMenuAndResumeHidesIt()
        {
            yield return CreateMatchWithHud();
            yield return WaitForPlaying();

            _manager.SetPaused(true);
            yield return null;
            Assert.That(IsShown("pause-overlay"), Is.True);
            Assert.That(Text("pause-title"), Is.EqualTo(UiText.Paused));

            _hud.Resume();
            yield return null;
            Assert.That(_manager.IsPaused, Is.False);
            Assert.That(IsShown("pause-overlay"), Is.False);
        }

        [UnityTest]
        public IEnumerator TimeUp_ShowsResultWithWinnerAndScore()
        {
            _config.Match.MatchDuration = 0.6f;
            yield return CreateMatchWithHud();
            yield return WaitForPlaying();

            ShootInto(new Vector3(-10f, 0f, 0f)); // A のゴールへ → B に 1 点
            yield return new WaitForSeconds(1f);

            Assert.That(IsShown("result-overlay"), Is.True);
            Assert.That(Text("result-title"), Is.EqualTo(UiText.TeamNameB + " " + UiText.Wins));
            Assert.That(Text("result-score"), Is.EqualTo("0 - 1"));
            Assert.That(IsShown("pause-overlay"), Is.False);
        }

        [UnityTest]
        public IEnumerator SuddenDeath_ShowsSuddenDeathInsteadOfTime()
        {
            _config.Match.MatchDuration = 0.3f;
            yield return CreateMatchWithHud();
            yield return WaitForPlaying();
            yield return new WaitForSeconds(0.5f);

            Assert.That(Text("timer"), Is.EqualTo(UiText.SuddenDeath));
        }

        [UnityTest]
        public IEnumerator GoToTitleFromPause_LoadsMenuUnpausedWithoutSession()
        {
            MatchSession.Create(new[]
            {
                new JoinedPlayer(0, 1, PlayerInputReader.KeyboardP1Scheme, Team.A),
                new JoinedPlayer(1, 1, PlayerInputReader.KeyboardP2Scheme, Team.B),
            });
            yield return CreateMatchWithHud();
            yield return WaitForPlaying();
            _manager.SetPaused(true);

            _hud.GoToTitle();
            yield return null;
            yield return null;

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Menu"));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(MatchSession.Current, Is.Null);
        }

        private IEnumerator CreateMatchWithHud()
        {
            TestWorld.CreatePlayer(_config, new Vector3(-4f, 1f, 0f), Vector3.right, Team.A);
            TestWorld.CreatePlayer(_config, new Vector3(4f, 1f, 0f), Vector3.left, Team.B);
            _ball = TestWorld.CreateBall(_config, new Vector3(0f, 0.5f, 0f));
            TestWorld.CreateGoal(new Vector3(-10f, 1f, 0f), Team.A);
            TestWorld.CreateGoal(new Vector3(10f, 1f, 0f), Team.B);
            _manager = TestWorld.CreateMatchManager(_config, _ball);

            var go = new GameObject("Hud");
            go.SetActive(false);
            var document = go.AddComponent<UIDocument>();
#if UNITY_EDITOR
            document.panelSettings = UnityEditor.AssetDatabase.LoadAssetAtPath<PanelSettings>(
                "Assets/_Project/UI/MochiPanelSettings.asset");
            document.visualTreeAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/_Project/UI/Match/MatchHud.uxml");
#endif
            Assert.That(document.visualTreeAsset, Is.Not.Null, "MatchHud.uxml がない");
            _hud = go.AddComponent<HudPresenter>();
            _hud.Match = _manager;
            go.SetActive(true);
            _root = document.rootVisualElement;
            yield return null;
            yield return null;
        }

        private IEnumerator WaitForPlaying()
        {
            yield return new WaitForSeconds(_config.Match.KickoffCountdown + 0.1f);
            Assert.That(_manager.Rules.Phase, Is.EqualTo(MatchPhase.Playing));
        }

        private void ShootInto(Vector3 goalPosition)
        {
            var body = _ball.GetComponent<Rigidbody>();
            var start = goalPosition * 0.7f + new Vector3(0f, 0.5f, 0f);
            body.position = start;
            _ball.transform.position = start;
            body.linearVelocity = goalPosition.normalized * 10f;
        }

        private string Text(string name) => _root.Q<Label>(name).text;

        private bool IsShown(string name) => _root.Q(name).style.display != DisplayStyle.None;
    }
}
