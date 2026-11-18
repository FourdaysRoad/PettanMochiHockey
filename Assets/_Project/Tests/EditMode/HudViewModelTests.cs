using MochiHockey.Core;
using MochiHockey.Runtime;
using NUnit.Framework;

namespace MochiHockey.Tests.EditMode
{
    /// <summary>TASK-0023：時間の整形と、試合中の HUD・リザルトの表示内容。</summary>
    public class HudViewModelTests
    {
        private MatchSettings _settings;
        private MatchRules _rules;
        private HudViewModel _hud;

        [SetUp]
        public void SetUp()
        {
            // 試合 180 秒、キックオフ 3 秒、再出現 1 秒
            _settings = TestSettings.CreateMatch();
            _rules = new MatchRules(_settings, new MochiGrowth(TestSettings.CreateMochi()));
            _hud = new HudViewModel(_rules);
            _rules.StartKickoff();
        }

        // ---- 時間の整形 ----

        [TestCase(180f, "3:00")]
        [TestCase(179.4f, "2:59")]
        [TestCase(65f, "1:05")]
        [TestCase(9.9f, "0:09")]
        [TestCase(0f, "0:00")]
        [TestCase(-3f, "0:00")]
        public void TimeFormat_ShowsMinutesAndSeconds(float seconds, string expected)
        {
            Assert.That(TimeFormat.MinutesSeconds(seconds), Is.EqualTo(expected));
        }

        // ---- スコア・時間 ----

        [Test]
        public void Kickoff_ShowsReadyAndFullTime()
        {
            Assert.That(_hud.ShowKickoff, Is.True);
            Assert.That(_hud.TimerText, Is.EqualTo("3:00"));
            Assert.That(_hud.ScoreAText, Is.EqualTo("0"));
            Assert.That(_hud.ScoreBText, Is.EqualTo("0"));
        }

        [Test]
        public void Playing_HidesKickoffAndCountsDown()
        {
            _rules.Tick(3f);
            _rules.Tick(0.6f);

            Assert.That(_hud.ShowKickoff, Is.False);
            Assert.That(_hud.TimerText, Is.EqualTo("2:59"));
        }

        [Test]
        public void SuddenDeath_ShowsSuddenDeathInsteadOfTime()
        {
            _rules.Tick(3f);
            _rules.Tick(180f);

            Assert.That(_hud.TimerText, Is.EqualTo(UiText.SuddenDeath));
        }

        // ---- GOAL! ----

        [Test]
        public void Goal_ShowsGoalWithPointsAndTeamUntilRespawn()
        {
            _rules.Tick(3f);

            _rules.RegisterGoal(Team.A, MochiLevel.Lv3); // B に 3 点

            Assert.That(_hud.ShowGoal, Is.True);
            Assert.That(_hud.GoalText, Is.EqualTo(UiText.Goal));
            Assert.That(_hud.GoalDetailText, Is.EqualTo("+3  " + UiText.TeamNameB));
            Assert.That(_hud.ScoreBText, Is.EqualTo("3"));

            _rules.Tick(1f);
            Assert.That(_hud.ShowGoal, Is.False);
        }

        [Test]
        public void NoGoalYet_HidesGoal()
        {
            _rules.Tick(3f);

            Assert.That(_hud.ShowGoal, Is.False);
        }

        // ---- リザルト（REQ-001）----

        [Test]
        public void Finished_ShowsWinnerAndFinalScore()
        {
            _rules.Tick(3f);
            _rules.RegisterGoal(Team.B, MochiLevel.Lv2); // A 2
            _rules.Tick(1f);
            _rules.RegisterGoal(Team.A, MochiLevel.Lv1); // B 1
            _rules.Tick(200f);

            Assert.That(_hud.ShowResult, Is.True);
            Assert.That(_hud.ResultTitle, Is.EqualTo(UiText.TeamNameA + " " + UiText.Wins));
            Assert.That(_hud.ResultScore, Is.EqualTo("2 - 1"));
            Assert.That(_hud.ShowGoal, Is.False);
        }

        [Test]
        public void SuddenDeathGoal_ShowsResultForScorer()
        {
            _rules.Tick(3f);
            _rules.Tick(180f);

            _rules.RegisterGoal(Team.A, MochiLevel.Lv1);

            Assert.That(_hud.ShowResult, Is.True);
            Assert.That(_hud.ResultTitle, Is.EqualTo(UiText.TeamNameB + " " + UiText.Wins));
            Assert.That(_hud.ResultScore, Is.EqualTo("0 - 1"));
        }

        [Test]
        public void NotFinished_HidesResult()
        {
            Assert.That(_hud.ShowResult, Is.False);
        }

        [Test]
        public void Restart_ClearsLastGoal()
        {
            _rules.Tick(3f);
            _rules.RegisterGoal(Team.A, MochiLevel.Lv1);

            _rules.StartKickoff();

            Assert.That(_hud.ShowGoal, Is.False);
            Assert.That(_hud.ScoreBText, Is.EqualTo("0"));
        }
    }
}
