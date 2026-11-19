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
        private readonly float _goDisplayTime;
        private int _timerSeconds = -1;
        private string _timerText;

        /// <param name="goDisplayTime">キックオフのあと「GO!」を出しておく秒数（0 なら出さない）。</param>
        public HudViewModel(MatchRules rules, float goDisplayTime = 0f)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _goDisplayTime = Math.Max(0f, goDisplayTime);
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

        /// <summary>カウントダウン（3・2・1）か、始まった直後の GO! を出すか（REQ-056）。</summary>
        public bool ShowKickoff => _rules.Phase == MatchPhase.Kickoff || ShowGo;

        /// <summary>カウントダウンの数字（残りの秒を切り上げ、"3" → "2" → "1"）か "GO!"。</summary>
        public string KickoffText => ShowGo
            ? UiText.Go
            : UiText.Number(Math.Max(1, (int)MathF.Ceiling(_rules.KickoffRemaining - 1e-4f)));

        /// <summary>キックオフが終わって、試合が始まった直後か。</summary>
        private bool ShowGo => _goDisplayTime > 0f
                               && _rules.Phase == MatchPhase.Playing
                               && _rules.MatchDuration - _rules.RemainingTime < _goDisplayTime;

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
