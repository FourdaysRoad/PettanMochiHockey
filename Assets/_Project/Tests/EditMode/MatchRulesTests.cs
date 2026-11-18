using System;
using System.Collections.Generic;
using MochiHockey.Core;
using NUnit.Framework;

namespace MochiHockey.Tests.EditMode
{
    /// <summary>TASK-0018：キックオフ・得点・タイマー・時間切れの勝敗。</summary>
    public class MatchRulesTests
    {
        private const float Tolerance = 1e-3f;

        // 試合 180 秒、キックオフ 3 秒。得点は Lv.1 / 2 / 3 で 1 / 2 / 3 点
        private MatchSettings _settings;
        private MatchRules _rules;
        private List<MatchPhase> _phases;
        private List<(Team team, int points)> _scored;

        [SetUp]
        public void SetUp()
        {
            _settings = TestSettings.CreateMatch();
            _rules = new MatchRules(_settings, new MochiGrowth(TestSettings.CreateMochi()));
            _phases = new List<MatchPhase>();
            _scored = new List<(Team, int)>();
            _rules.PhaseChanged += _phases.Add;
            _rules.Scored += (team, points) => _scored.Add((team, points));
        }

        [Test]
        public void Constructor_NullArguments_Throw()
        {
            var growth = new MochiGrowth(TestSettings.CreateMochi());
            Assert.Throws<ArgumentNullException>(() => new MatchRules(null, growth));
            Assert.Throws<ArgumentNullException>(() => new MatchRules(_settings, null));
        }

        // ---- 開始前・キックオフ（REQ-005）----

        [Test]
        public void NewRules_WaitInKickoffWithFullTimeAndNoScore()
        {
            Assert.That(_rules.Phase, Is.EqualTo(MatchPhase.Kickoff));
            Assert.That(_rules.RemainingTime, Is.EqualTo(180f));
            Assert.That(_rules.GetScore(Team.A), Is.EqualTo(0));
            Assert.That(_rules.GetScore(Team.B), Is.EqualTo(0));
            Assert.That(_rules.Winner, Is.Null);
        }

        [Test]
        public void Tick_BeforeStartKickoff_DoesNothing()
        {
            _rules.Tick(10f);

            Assert.That(_rules.Phase, Is.EqualTo(MatchPhase.Kickoff));
            Assert.That(_rules.RemainingTime, Is.EqualTo(180f));
            Assert.That(_phases, Is.Empty);
        }

        [Test]
        public void StartKickoff_RaisesKickoffAndStartsPlayingAfterCountdown()
        {
            _rules.StartKickoff();
            Assert.That(_phases, Is.EqualTo(new[] { MatchPhase.Kickoff }));

            _rules.Tick(2.9f);
            Assert.That(_rules.Phase, Is.EqualTo(MatchPhase.Kickoff));

            _rules.Tick(0.2f);
            Assert.That(_rules.Phase, Is.EqualTo(MatchPhase.Playing));
            Assert.That(_phases, Is.EqualTo(new[] { MatchPhase.Kickoff, MatchPhase.Playing }));
        }

        [Test]
        public void Kickoff_DoesNotUseMatchTime()
        {
            _rules.StartKickoff();

            _rules.Tick(3f);

            Assert.That(_rules.RemainingTime, Is.EqualTo(180f).Within(Tolerance));
        }

        [Test]
        public void Kickoff_CountdownTimeLeftOverGoesToMatchTime()
        {
            _rules.StartKickoff();

            _rules.Tick(5f); // キックオフ 3 秒 + 試合 2 秒

            Assert.That(_rules.RemainingTime, Is.EqualTo(178f).Within(Tolerance));
        }

        // ---- タイマー（REQ-001）----

        [Test]
        public void Playing_TickCountsDownRemainingTime()
        {
            StartPlaying();

            _rules.Tick(10f);
            _rules.Tick(0.5f);

            Assert.That(_rules.RemainingTime, Is.EqualTo(169.5f).Within(Tolerance));
        }

        [Test]
        public void RemainingTime_NeverGoesBelowZero()
        {
            StartPlaying();
            _rules.RegisterGoal(Team.A, MochiLevel.Lv1);

            _rules.Tick(500f);

            Assert.That(_rules.RemainingTime, Is.EqualTo(0f));
        }

        [Test]
        public void Tick_NegativeDeltaTime_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _rules.Tick(-0.1f));
        }

        // ---- 得点（REQ-003）----

        [TestCase(MochiLevel.Lv1, 1)]
        [TestCase(MochiLevel.Lv2, 2)]
        [TestCase(MochiLevel.Lv3, 3)]
        public void RegisterGoal_GivesLevelPointsToTheOtherTeam(MochiLevel level, int points)
        {
            StartPlaying();

            _rules.RegisterGoal(Team.A, level);

            Assert.That(_rules.GetScore(Team.B), Is.EqualTo(points));
            Assert.That(_rules.GetScore(Team.A), Is.EqualTo(0));
            Assert.That(_scored, Is.EqualTo(new[] { (Team.B, points) }));
        }

        [Test]
        public void RegisterGoal_IntoTeamBGoal_ScoresForTeamA()
        {
            StartPlaying();

            _rules.RegisterGoal(Team.B, MochiLevel.Lv1);

            Assert.That(_rules.GetScore(Team.A), Is.EqualTo(1));
        }

        [Test]
        public void RegisterGoal_OwnGoal_StillScoresForTheOtherTeam()
        {
            // 誰が入れたかは関係なく、守る側の相手チームの得点になる
            StartPlaying();

            _rules.RegisterGoal(Team.A, MochiLevel.Lv2);
            _rules.RegisterGoal(Team.A, MochiLevel.Lv1);

            Assert.That(_rules.GetScore(Team.B), Is.EqualTo(3));
        }

        [Test]
        public void RegisterGoal_DuringKickoff_IsIgnored()
        {
            _rules.StartKickoff();

            _rules.RegisterGoal(Team.A, MochiLevel.Lv3);

            Assert.That(_rules.GetScore(Team.B), Is.EqualTo(0));
            Assert.That(_scored, Is.Empty);
        }

        // ---- 時間切れ（REQ-001）----

        [Test]
        public void TimeUp_WithLead_FinishesWithLeaderAsWinner()
        {
            StartPlaying();
            _rules.RegisterGoal(Team.A, MochiLevel.Lv2); // B 2 点
            _rules.RegisterGoal(Team.B, MochiLevel.Lv1); // A 1 点

            _rules.Tick(179.9f);
            Assert.That(_rules.Phase, Is.EqualTo(MatchPhase.Playing));
            Assert.That(_rules.Winner, Is.Null);

            _rules.Tick(0.2f);
            Assert.That(_rules.Phase, Is.EqualTo(MatchPhase.Finished));
            Assert.That(_rules.Winner, Is.EqualTo(Team.B));
            Assert.That(_phases[_phases.Count - 1], Is.EqualTo(MatchPhase.Finished));
        }

        [Test]
        public void TimeUp_InOneLargeStepFromKickoff_FinishesInOrder()
        {
            _rules.StartKickoff();
            _rules.Tick(3f);
            _rules.RegisterGoal(Team.B, MochiLevel.Lv3);

            _rules.Tick(1000f);

            Assert.That(_rules.Phase, Is.EqualTo(MatchPhase.Finished));
            Assert.That(_rules.Winner, Is.EqualTo(Team.A));
            Assert.That(_phases, Is.EqualTo(new[] { MatchPhase.Kickoff, MatchPhase.Playing, MatchPhase.Finished }));
        }

        [Test]
        public void TimeUp_Tied_GoesToSuddenDeathWithoutWinner()
        {
            StartPlaying();

            _rules.Tick(180f);

            Assert.That(_rules.Phase, Is.EqualTo(MatchPhase.SuddenDeath));
            Assert.That(_rules.Winner, Is.Null);
            Assert.That(_rules.RemainingTime, Is.EqualTo(0f));
        }

        [Test]
        public void Finished_IgnoresGoalsAndTicks()
        {
            StartPlaying();
            _rules.RegisterGoal(Team.A, MochiLevel.Lv1);
            _rules.Tick(180f);

            _rules.RegisterGoal(Team.B, MochiLevel.Lv3);
            _rules.Tick(10f);

            Assert.That(_rules.Phase, Is.EqualTo(MatchPhase.Finished));
            Assert.That(_rules.GetScore(Team.A), Is.EqualTo(0));
            Assert.That(_rules.Winner, Is.EqualTo(Team.B));
        }

        [Test]
        public void StartKickoff_AgainAfterFinish_ResetsScoreTimeAndWinner()
        {
            StartPlaying();
            _rules.RegisterGoal(Team.A, MochiLevel.Lv1);
            _rules.Tick(180f);

            _rules.StartKickoff();

            Assert.That(_rules.Phase, Is.EqualTo(MatchPhase.Kickoff));
            Assert.That(_rules.GetScore(Team.B), Is.EqualTo(0));
            Assert.That(_rules.RemainingTime, Is.EqualTo(180f));
            Assert.That(_rules.Winner, Is.Null);
        }

        [Test]
        public void UsesDurationsFromSettings()
        {
            _settings.MatchDuration = 60f;
            _settings.KickoffCountdown = 1f;
            var rules = new MatchRules(_settings, new MochiGrowth(TestSettings.CreateMochi()));

            rules.StartKickoff();
            rules.Tick(1f);
            Assert.That(rules.Phase, Is.EqualTo(MatchPhase.Playing));
            Assert.That(rules.RemainingTime, Is.EqualTo(60f).Within(Tolerance));
        }

        private void StartPlaying()
        {
            _rules.StartKickoff();
            _rules.Tick(_settings.KickoffCountdown);
            Assert.That(_rules.Phase, Is.EqualTo(MatchPhase.Playing));
        }
    }
}
