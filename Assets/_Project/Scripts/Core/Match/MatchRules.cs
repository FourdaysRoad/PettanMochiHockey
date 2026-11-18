using System;

namespace MochiHockey.Core
{
    /// <summary>
    /// 試合のルール（REQ-001〜005）。スコア・残り時間・フェーズを持ち、Tick で時間を進める。
    /// - StartKickoff でスコアと時間を戻し、KickoffCountdown の後に Playing になる（カウントダウン中は試合時間は進まない）
    /// - ゴールの得点は、守る側の相手チームに、餅のレベルに応じて入る（オウンゴールも同じ）
    /// - 時間切れで点差があれば Finished、同点なら SuddenDeath
    /// </summary>
    public sealed class MatchRules
    {
        // 1/60 秒を積み重ねた誤差で区切りを越えられないことがないようにする許容差
        private const float TimeTolerance = 1e-4f;

        private readonly MatchSettings _settings;
        private readonly MochiGrowth _growth;
        private readonly int[] _scores = new int[2];

        private bool _started;
        private float _kickoffRemaining;

        public MatchRules(MatchSettings settings, MochiGrowth growth)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _growth = growth ?? throw new ArgumentNullException(nameof(growth));
            RemainingTime = _settings.MatchDuration;
        }

        public MatchPhase Phase { get; private set; } = MatchPhase.Kickoff;

        /// <summary>試合の残り時間（秒）。0 以上。サドンデス中は 0。</summary>
        public float RemainingTime { get; private set; }

        /// <summary>勝ったチーム。Finished のときだけ値がある。</summary>
        public Team? Winner { get; private set; }

        public event Action<MatchPhase> PhaseChanged;

        /// <summary>得点が入った（得点したチーム、加算した点）。</summary>
        public event Action<Team, int> Scored;

        public int GetScore(Team team) => _scores[(int)team];

        /// <summary>キックオフから始める。スコア・残り時間・勝者を最初に戻す。</summary>
        public void StartKickoff()
        {
            _started = true;
            _scores[0] = 0;
            _scores[1] = 0;
            Winner = null;
            RemainingTime = _settings.MatchDuration;
            _kickoffRemaining = _settings.KickoffCountdown;
            ChangePhase(MatchPhase.Kickoff);
        }

        /// <summary>ゴールに入った。得点は守る側の相手に入る（REQ-003）。試合中でなければ何もしない。</summary>
        /// <param name="defendingTeam">ゴールを守っているチーム。</param>
        public void RegisterGoal(Team defendingTeam, MochiLevel level)
        {
            if (Phase != MatchPhase.Playing)
            {
                return;
            }

            var scoringTeam = defendingTeam == Team.A ? Team.B : Team.A;
            var points = _growth.GetPoints(level);
            _scores[(int)scoringTeam] += points;
            Scored?.Invoke(scoringTeam, points);
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Delta time must not be negative.");
            }

            if (!_started)
            {
                return;
            }

            var remaining = deltaTime;
            while (remaining > 0f)
            {
                switch (Phase)
                {
                    case MatchPhase.Kickoff:
                        if (remaining < _kickoffRemaining - TimeTolerance)
                        {
                            _kickoffRemaining -= remaining;
                            return;
                        }

                        remaining -= _kickoffRemaining;
                        _kickoffRemaining = 0f;
                        ChangePhase(MatchPhase.Playing);
                        break;

                    case MatchPhase.Playing:
                        if (remaining < RemainingTime - TimeTolerance)
                        {
                            RemainingTime -= remaining;
                            return;
                        }

                        remaining -= RemainingTime;
                        RemainingTime = 0f;
                        OnTimeUp();
                        break;

                    default:
                        return;
                }
            }
        }

        private void OnTimeUp()
        {
            var a = GetScore(Team.A);
            var b = GetScore(Team.B);
            if (a == b)
            {
                ChangePhase(MatchPhase.SuddenDeath);
                return;
            }

            Winner = a > b ? Team.A : Team.B;
            ChangePhase(MatchPhase.Finished);
        }

        private void ChangePhase(MatchPhase next)
        {
            Phase = next;
            PhaseChanged?.Invoke(next);
        }
    }
}
