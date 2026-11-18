using System.Collections.Generic;
using MochiHockey.Core;
using NUnit.Framework;

namespace MochiHockey.Tests.EditMode
{
    /// <summary>TASK-0019：ゴール後の再出現待ち・サドンデス・フェーズ遷移。</summary>
    public class MatchRulesFlowTests
    {
        private const float Tolerance = 1e-3f;

        // 試合 180 秒、キックオフ 3 秒、再出現 1.0 秒
        private MatchSettings _settings;
        private MatchRules _rules;
        private List<MatchPhase> _phases;
        private int _respawnRequests;

        [SetUp]
        public void SetUp()
        {
            _settings = TestSettings.CreateMatch();
            _rules = new MatchRules(_settings, new MochiGrowth(TestSettings.CreateMochi()));
            _phases = new List<MatchPhase>();
            _respawnRequests = 0;
            _rules.PhaseChanged += _phases.Add;
            _rules.MochiRespawnRequested += () => _respawnRequests++;
        }

        // ---- ゴール後の再出現（REQ-004）----

        [Test]
        public void Goal_WaitsRespawnDelayThenRequestsMochiAndResumesPlaying()
        {
            StartPlaying();

            _rules.RegisterGoal(Team.A, MochiLevel.Lv2);
            Assert.That(_rules.Phase, Is.EqualTo(MatchPhase.MochiRespawning));

            _rules.Tick(0.9f);
            Assert.That(_respawnRequests, Is.EqualTo(0));
            Assert.That(_rules.Phase, Is.EqualTo(MatchPhase.MochiRespawning));

            _rules.Tick(0.15f);
            Assert.That(_respawnRequests, Is.EqualTo(1));
            Assert.That(_rules.Phase, Is.EqualTo(MatchPhase.Playing));
        }

        [Test]
        public void RespawnWait_KeepsMatchClockRunning()
        {
            StartPlaying();

            _rules.RegisterGoal(Team.A, MochiLevel.Lv1);
            _rules.Tick(1f);

            Assert.That(_rules.RemainingTime, Is.EqualTo(179f).Within(Tolerance));
        }

        [Test]
        public void RespawnWait_IgnoresAnotherGoal()
        {
            StartPlaying();
            _rules.RegisterGoal(Team.A, MochiLevel.Lv1);

            _rules.RegisterGoal(Team.A, MochiLevel.Lv3);

            Assert.That(_rules.GetScore(Team.B), Is.EqualTo(1));
        }

        [Test]
        public void ManySmallSteps_RequestRespawnOnce()
        {
            StartPlaying();
            _rules.RegisterGoal(Team.B, MochiLevel.Lv1);

            for (var i = 0; i < 120; i++)
            {
                _rules.Tick(1f / 60f);
            }

            Assert.That(_respawnRequests, Is.EqualTo(1));
            Assert.That(_rules.Phase, Is.EqualTo(MatchPhase.Playing));
        }

        [Test]
        public void UsesRespawnDelayFromSettings()
        {
            _settings.MochiRespawnDelay = 2f;
            var rules = new MatchRules(_settings, new MochiGrowth(TestSettings.CreateMochi()));
            var requests = 0;
            rules.MochiRespawnRequested += () => requests++;
            rules.StartKickoff();
            rules.Tick(_settings.KickoffCountdown);
            rules.RegisterGoal(Team.A, MochiLevel.Lv1);

            rules.Tick(1.5f);
            Assert.That(requests, Is.EqualTo(0));

            rules.Tick(0.6f);
            Assert.That(requests, Is.EqualTo(1));
        }

        // ---- 再出現待ち中の時間切れ ----

        [Test]
        public void TimeUpDuringRespawnWait_WithLead_FinishesWithoutRespawn()
        {
            StartPlaying();
            _rules.Tick(179.5f);
            _rules.RegisterGoal(Team.A, MochiLevel.Lv1); // B が 1-0 でリード

            _rules.Tick(1f);

            Assert.That(_rules.Phase, Is.EqualTo(MatchPhase.Finished));
            Assert.That(_rules.Winner, Is.EqualTo(Team.B));
            Assert.That(_respawnRequests, Is.EqualTo(0));
        }

        [Test]
        public void TimeUpDuringRespawnWait_Tied_GoesToSuddenDeathAndStillRespawns()
        {
            StartPlaying();
            _rules.RegisterGoal(Team.A, MochiLevel.Lv1); // B 1
            _rules.Tick(1f);                             // 1 回目の再出現
            _rules.Tick(178.5f);                         // 残り 0.5 秒
            _rules.RegisterGoal(Team.B, MochiLevel.Lv1); // A 1 → 1-1

            _rules.Tick(0.6f);
            Assert.That(_rules.Phase, Is.EqualTo(MatchPhase.SuddenDeath));
            Assert.That(_respawnRequests, Is.EqualTo(1));

            // 餅がまだないので、待ち時間が終わったら出し直す（サドンデスのまま）
            _rules.Tick(0.5f);
            Assert.That(_respawnRequests, Is.EqualTo(2));
            Assert.That(_rules.Phase, Is.EqualTo(MatchPhase.SuddenDeath));
        }

        // ---- サドンデス（REQ-002）----

        [Test]
        public void SuddenDeath_NextGoalWinsImmediately()
        {
            StartPlaying();
            _rules.RegisterGoal(Team.A, MochiLevel.Lv1); // B 1
            _rules.Tick(1f);
            _rules.RegisterGoal(Team.B, MochiLevel.Lv1); // A 1
            _rules.Tick(1f);
            _rules.Tick(200f);
            Assert.That(_rules.Phase, Is.EqualTo(MatchPhase.SuddenDeath));

            _rules.RegisterGoal(Team.B, MochiLevel.Lv2); // A 3

            Assert.That(_rules.Phase, Is.EqualTo(MatchPhase.Finished));
            Assert.That(_rules.Winner, Is.EqualTo(Team.A));
            Assert.That(_rules.GetScore(Team.A), Is.EqualTo(3));
            Assert.That(_respawnRequests, Is.EqualTo(2));
        }

        [Test]
        public void SuddenDeath_HasNoTimeLimit()
        {
            StartPlaying();
            _rules.Tick(180f);

            _rules.Tick(10000f);

            Assert.That(_rules.Phase, Is.EqualTo(MatchPhase.SuddenDeath));
            Assert.That(_rules.RemainingTime, Is.EqualTo(0f));
        }

        [Test]
        public void GoalDuringRespawnWaitNearTimeUp_IsIgnored()
        {
            StartPlaying();
            _rules.Tick(179.5f);
            _rules.RegisterGoal(Team.A, MochiLevel.Lv1);
            _rules.RegisterGoal(Team.B, MochiLevel.Lv1);
            // ↑ 2 つ目は再出現待ち中なので無視され、B 1-0 のまま
            Assert.That(_rules.GetScore(Team.A), Is.EqualTo(0));
        }

        // ---- フェーズの通し ----

        [Test]
        public void WholeMatch_RaisesPhasesInOrder()
        {
            _rules.StartKickoff();
            _rules.Tick(3f);
            _rules.RegisterGoal(Team.A, MochiLevel.Lv1); // B 1
            _rules.Tick(1f);
            _rules.RegisterGoal(Team.B, MochiLevel.Lv1); // A 1
            _rules.Tick(1f);
            _rules.Tick(500f);
            _rules.RegisterGoal(Team.A, MochiLevel.Lv3); // B 4

            Assert.That(_phases, Is.EqualTo(new[]
            {
                MatchPhase.Kickoff,
                MatchPhase.Playing,
                MatchPhase.MochiRespawning,
                MatchPhase.Playing,
                MatchPhase.MochiRespawning,
                MatchPhase.Playing,
                MatchPhase.SuddenDeath,
                MatchPhase.Finished,
            }));
            Assert.That(_rules.Winner, Is.EqualTo(Team.B));
        }

        [Test]
        public void StartKickoff_DuringRespawnWait_CancelsPendingRespawn()
        {
            StartPlaying();
            _rules.RegisterGoal(Team.A, MochiLevel.Lv1);

            _rules.StartKickoff();
            _rules.Tick(5f);

            Assert.That(_respawnRequests, Is.EqualTo(0));
            Assert.That(_rules.Phase, Is.EqualTo(MatchPhase.Playing));
        }

        private void StartPlaying()
        {
            _rules.StartKickoff();
            _rules.Tick(_settings.KickoffCountdown);
            Assert.That(_rules.Phase, Is.EqualTo(MatchPhase.Playing));
        }
    }
}
