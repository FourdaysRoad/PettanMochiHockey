using System;
using System.Collections.Generic;

namespace MochiHockey.Core
{
    /// <summary>参加したプレイヤー 1 人分（番号・デバイス・操作の割り当て・チーム）。</summary>
    public readonly struct JoinedPlayer
    {
        public JoinedPlayer(int slot, int deviceId, string controlScheme, Team team)
        {
            Slot = slot;
            DeviceId = deviceId;
            ControlScheme = controlScheme;
            Team = team;
        }

        /// <summary>参加した順の番号（0 = 1P 〜 3 = 4P）。</summary>
        public int Slot { get; }

        /// <summary>入力デバイスの番号（Input System の deviceId）。キーボードは 2 人で同じ番号を使う。</summary>
        public int DeviceId { get; }

        /// <summary>操作の割り当て（KeyboardP1 / KeyboardP2 / Gamepad）。</summary>
        public string ControlScheme { get; }

        public Team Team { get; }

        public JoinedPlayer WithSlot(int slot) => new JoinedPlayer(slot, DeviceId, ControlScheme, Team);

        public JoinedPlayer WithTeam(Team team) => new JoinedPlayer(Slot, DeviceId, ControlScheme, team);
    }

    /// <summary>
    /// 対戦設定での参加受付（REQ-041, REQ-042）。ボタンを押したデバイスから順に 1P〜4P として参加させる。
    /// 同じデバイスでも操作の割り当てが違えば別の人（キーボード 1 台で 2 人）として扱う。
    /// </summary>
    public sealed class JoinRoster
    {
        public const int MaxPlayers = 4;

        private readonly List<JoinedPlayer> _players = new List<JoinedPlayer>();

        public IReadOnlyList<JoinedPlayer> Players => _players;

        /// <summary>今の人数とチームで試合を始められるか（1vs1 か 2vs2）。</summary>
        public bool CanStart
        {
            get
            {
                var teams = new List<Team>(_players.Count);
                foreach (var player in _players)
                {
                    teams.Add(player.Team);
                }

                return TeamAssignment.CanStart(teams);
            }
        }

        /// <summary>参加させる。番号（0 始まり）を返す。満員か、同じデバイスと割り当てですでに参加していれば -1。</summary>
        public int TryJoin(int deviceId, string controlScheme)
        {
            if (controlScheme == null)
            {
                throw new ArgumentNullException(nameof(controlScheme));
            }

            if (_players.Count >= MaxPlayers || IndexOf(deviceId, controlScheme) >= 0)
            {
                return -1;
            }

            var slot = _players.Count;
            _players.Add(new JoinedPlayer(slot, deviceId, controlScheme, TeamAssignment.DefaultTeam(slot)));
            return slot;
        }

        /// <summary>抜ける。後ろの人の番号を詰める（チームはそのまま）。参加していなければ false。</summary>
        public bool Leave(int deviceId, string controlScheme)
        {
            var index = IndexOf(deviceId, controlScheme);
            if (index < 0)
            {
                return false;
            }

            _players.RemoveAt(index);
            for (var i = index; i < _players.Count; i++)
            {
                _players[i] = _players[i].WithSlot(i);
            }

            return true;
        }

        public void SetTeam(int slot, Team team)
        {
            if (slot < 0 || slot >= _players.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(slot), slot, "No player has joined in this slot.");
            }

            _players[slot] = _players[slot].WithTeam(team);
        }

        public void Clear()
        {
            _players.Clear();
        }

        private int IndexOf(int deviceId, string controlScheme)
        {
            for (var i = 0; i < _players.Count; i++)
            {
                if (_players[i].DeviceId == deviceId && _players[i].ControlScheme == controlScheme)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
