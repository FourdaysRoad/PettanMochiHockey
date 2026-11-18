using System;
using System.Collections.Generic;

namespace MochiHockey.Core
{
    /// <summary>チーム分けの決まり（REQ-006, REQ-041）。1vs1（2 人）か 2vs2（4 人）だけ試合を始められる。</summary>
    public static class TeamAssignment
    {
        /// <summary>参加した順番（0 = 1P）から決める最初のチーム。1P から A, B, A, B と交互にする。</summary>
        public static Team DefaultTeam(int slot)
        {
            if (slot < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(slot), slot, "Slot must not be negative.");
            }

            return slot % 2 == 0 ? Team.A : Team.B;
        }

        /// <summary>2 人か 4 人で、両チームが同じ人数なら試合を始められる。</summary>
        public static bool CanStart(IReadOnlyCollection<Team> teams)
        {
            if (teams == null || (teams.Count != 2 && teams.Count != 4))
            {
                return false;
            }

            var teamA = 0;
            foreach (var team in teams)
            {
                if (team == Team.A)
                {
                    teamA++;
                }
            }

            return teamA * 2 == teams.Count;
        }
    }
}
