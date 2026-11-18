using System;

namespace MochiHockey.Core
{
    /// <summary>
    /// 試合のルール（REQ-001〜005）。スコア・残り時間・フェーズを持ち、Tick で時間を進める。
    /// - StartKickoff でスコアと時間を戻し、KickoffCountdown の後に Playing になる（カウントダウン中は試合時間は進まない）
    /// - ゴールの得点は、守る側の相手チームに、餅のレベルに応じて入る（オウンゴールも同じ）
    /// - ゴール後は MochiRespawning になり、MochiRespawnDelay 後に MochiRespawnRequested を出して Playing に戻る。
    ///   待っている間も試合時間は進み、次のゴールは受け付けない
    /// - 時間切れで点差があれば Finished、同点なら SuddenDeath（時間制限なし）。サドンデスでは次のゴールで Finished
    /// - 同点で時間切れになったのが再出現待ちの間なら、待ち時間が終わったところで餅の再出現を求める
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
        private bool _respawnPending;
        private float _respawnRemaining;

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

        /// <summary>ゴール後、中央に Lv.1 の餅を出し直すとき（プレイヤーの位置は戻さない、REQ-004）。</summary>
        public event Action MochiRespawnRequested;

        public int GetScore(Team team) => _scores[(int)team];

        /// <summary>キックオフから始める。スコア・残り時間・勝者・再出現待ちを最初に戻す。</summary>
        public void StartKickoff()
        {
            _started = true;
            _scores[0] = 0;
            _scores[1] = 0;
            Winner = null;
            RemainingTime = _settings.MatchDuration;
            _kickoffRemaining = _settings.KickoffCountdown;
            _respawnPending = false;
            _respawnRemaining = 0f;
            ChangePhase(MatchPhase.Kickoff);
        }

        /// <summary>
        /// ゴールに入った。得点は守る側の相手に入る（REQ-003）。
        /// Playing・SuddenDeath 以外のとき、餅の再出現を待っているときは何もしない。
        /// </summary>
        /// <param name="defendingTeam">ゴールを守っているチーム。</param>
        public void RegisterGoal(Team defendingTeam, MochiLevel level)
        {
            if (_respawnPending || (Phase != MatchPhase.Playing && Phase != MatchPhase.SuddenDeath))
            {
                return;
            }

            var scoringTeam = defendingTeam == Team.A ? Team.B : Team.A;
            var points = _growth.GetPoints(level);
            _scores[(int)scoringTeam] += points;
            Scored?.Invoke(scoringTeam, points);

            if (Phase == MatchPhase.SuddenDeath)
            {
                Winner = scoringTeam;
                ChangePhase(MatchPhase.Finished);
                return;
            }

            _respawnPending = true;
            _respawnRemaining = _settings.MochiRespawnDelay;
            ChangePhase(MatchPhase.MochiRespawning);
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
            while (true)
            {
                switch (Phase)
                {
                    case MatchPhase.Kickoff:
                        if (remaining < _kickoffRemaining - TimeTolerance)
                        {
                            _kickoffRemaining -= remaining;
                            return;
                        }

                        remaining = MathF.Max(0f, remaining - _kickoffRemaining);
                        _kickoffRemaining = 0f;
                        ChangePhase(MatchPhase.Playing);
                        break;

                    case MatchPhase.Playing:
                        if (remaining < RemainingTime - TimeTolerance)
                        {
                            RemainingTime -= remaining;
                            return;
                        }

                        remaining = MathF.Max(0f, remaining - RemainingTime);
                        RemainingTime = 0f;
                        OnTimeUp();
                        break;

                    case MatchPhase.MochiRespawning:
                        var step = MathF.Min(_respawnRemaining, RemainingTime);
                        if (remaining < step - TimeTolerance)
                        {
                            RemainingTime -= remaining;
                            _respawnRemaining -= remaining;
                            return;
                        }

                        remaining = MathF.Max(0f, remaining - step);
                        RemainingTime = MathF.Max(0f, RemainingTime - step);
                        _respawnRemaining = MathF.Max(0f, _respawnRemaining - step);

                        // 同時なら先に餅を出し直してから時間切れにする
                        if (_respawnRemaining <= TimeTolerance)
                        {
                            Respawn();
                            ChangePhase(MatchPhase.Playing);
                        }
                        else
                        {
                            RemainingTime = 0f;
                            OnTimeUp();
                        }

                        break;

                    case MatchPhase.SuddenDeath:
                        if (!_respawnPending || remaining < _respawnRemaining - TimeTolerance)
                        {
                            if (_respawnPending)
                            {
                                _respawnRemaining -= remaining;
                            }

                            return;
                        }

                        remaining = MathF.Max(0f, remaining - _respawnRemaining);
                        Respawn();
                        break;

                    default:
                        return;
                }
            }
        }

        private void Respawn()
        {
            _respawnPending = false;
            _respawnRemaining = 0f;
            MochiRespawnRequested?.Invoke();
        }

        private void OnTimeUp()
        {
            var a = GetScore(Team.A);
            var b = GetScore(Team.B);
            if (a == b)
            {
                // 再出現待ちの途中なら、待ちはそのまま続ける（サドンデスの餅になる）
                ChangePhase(MatchPhase.SuddenDeath);
                return;
            }

            _respawnPending = false;
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
