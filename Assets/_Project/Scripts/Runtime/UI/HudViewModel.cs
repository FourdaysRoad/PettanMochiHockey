using System;
using MochiHockey.Core;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// 試合中の HUD とリザルトの表示内容（REQ-001〜004, REQ-053）。MatchRules から作り、画面の部品には依存しない。
    /// - スコアと残り時間（サドンデス中は SUDDEN DEATH）
    /// - キックオフ中は READY
    /// - ゴール後、餅が出し直されるまで GOAL! と獲得点数
    /// - 試合終了で勝者と最終スコア
    /// </summary>
    public sealed class HudViewModel
    {
        private readonly MatchRules _rules;
        private Team _lastScoringTeam;
        private int _lastPoints;
        private int _timerSeconds = -1;
        private string _timerText;

        public HudViewModel(MatchRules rules)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _rules.Scored += OnScored;
            _rules.PhaseChanged += OnPhaseChanged;
        }

        public string ScoreAText => UiText.Number(_rules.GetScore(Team.A));

        public string ScoreBText => UiText.Number(_rules.GetScore(Team.B));

        /// <summary>残り時間。HUD が毎フレーム読むので、表示する秒が変わったときだけ作り直す（TASK-0031）。</summary>
        public string TimerText
        {
            get
            {
                if (_rules.Phase == MatchPhase.SuddenDeath)
                {
                    return UiText.SuddenDeath;
                }

                var remaining = _rules.RemainingTime;
                var whole = remaining > 0f ? (int)System.MathF.Floor(remaining) : 0;
                if (_timerText == null || whole != _timerSeconds)
                {
                    _timerSeconds = whole;
                    _timerText = TimeFormat.MinutesSeconds(remaining);
                }

                return _timerText;
            }
        }

        public bool ShowKickoff => _rules.Phase == MatchPhase.Kickoff;

        public bool ShowGoal => _rules.Phase == MatchPhase.MochiRespawning && _lastPoints > 0;

        public string GoalText => UiText.Goal;

        public string GoalDetailText => $"+{_lastPoints}  {TeamName(_lastScoringTeam)}";

        public bool ShowResult => _rules.Phase == MatchPhase.Finished;

        public string ResultTitle => _rules.Winner.HasValue ? $"{TeamName(_rules.Winner.Value)} {UiText.Wins}" : string.Empty;

        public string ResultScore => $"{_rules.GetScore(Team.A)} - {_rules.GetScore(Team.B)}";

        private static string TeamName(Team team) => team == Team.A ? UiText.TeamNameA : UiText.TeamNameB;

        private void OnScored(Team team, int points)
        {
            _lastScoringTeam = team;
            _lastPoints = points;
        }

        private void OnPhaseChanged(MatchPhase phase)
        {
            if (phase == MatchPhase.Kickoff)
            {
                _lastPoints = 0;
            }
        }
    }
}
