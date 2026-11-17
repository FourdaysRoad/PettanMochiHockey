namespace MochiHockey.Core
{
    /// <summary>
    /// 杵が当たるかの判定（REQ-006, REQ-035, REQ-036）。
    /// 2 人が同時に当て合った場合に両方を被弾させるため、呼び出し側はスイング開始時点の
    /// targetCanBeHit を使って全員分を判定してから、被弾（PettanStateMachine.Begin）を反映すること。
    /// </summary>
    public static class HitRules
    {
        /// <summary>自分・味方・被弾できない状態の相手には当たらない。</summary>
        public static bool CanHit(int attackerId, Team attackerTeam, int targetId, Team targetTeam, bool targetCanBeHit)
        {
            if (attackerId == targetId)
            {
                return false;
            }

            if (attackerTeam == targetTeam)
            {
                return false;
            }

            return targetCanBeHit;
        }
    }
}
