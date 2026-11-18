using System;
using MochiHockey.Core;
using NUnit.Framework;

namespace MochiHockey.Tests.EditMode
{
    /// <summary>TASK-0021：参加の受け付け（番号・デバイス・チーム）と、試合を始められる人数・チーム。</summary>
    public class JoinRosterTests
    {
        private const int Keyboard = 1;
        private const int PadA = 10;
        private const int PadB = 11;
        private const string P1 = "KeyboardP1";
        private const string P2 = "KeyboardP2";
        private const string Pad = "Gamepad";

        private JoinRoster _roster;

        [SetUp]
        public void SetUp()
        {
            _roster = new JoinRoster();
        }

        // ---- 参加（REQ-041）----

        [Test]
        public void TryJoin_GivesNumbersInJoinOrder()
        {
            Assert.That(_roster.TryJoin(PadA, Pad), Is.EqualTo(0));
            Assert.That(_roster.TryJoin(Keyboard, P1), Is.EqualTo(1));
            Assert.That(_roster.TryJoin(PadB, Pad), Is.EqualTo(2));

            Assert.That(_roster.Players.Count, Is.EqualTo(3));
            Assert.That(_roster.Players[1].DeviceId, Is.EqualTo(Keyboard));
            Assert.That(_roster.Players[1].ControlScheme, Is.EqualTo(P1));
            Assert.That(_roster.Players[1].Slot, Is.EqualTo(1));
        }

        [Test]
        public void TryJoin_OneKeyboardCanJoinTwiceWithDifferentSchemes()
        {
            Assert.That(_roster.TryJoin(Keyboard, P1), Is.EqualTo(0));
            Assert.That(_roster.TryJoin(Keyboard, P2), Is.EqualTo(1));
        }

        [Test]
        public void TryJoin_SameDeviceAndSchemeTwice_IsRejected()
        {
            _roster.TryJoin(PadA, Pad);

            Assert.That(_roster.TryJoin(PadA, Pad), Is.EqualTo(-1));
            Assert.That(_roster.Players.Count, Is.EqualTo(1));
        }

        [Test]
        public void TryJoin_FifthPlayer_IsRejected()
        {
            _roster.TryJoin(Keyboard, P1);
            _roster.TryJoin(Keyboard, P2);
            _roster.TryJoin(PadA, Pad);
            _roster.TryJoin(PadB, Pad);

            Assert.That(_roster.TryJoin(99, Pad), Is.EqualTo(-1));
            Assert.That(_roster.Players.Count, Is.EqualTo(JoinRoster.MaxPlayers));
        }

        [Test]
        public void TryJoin_NullScheme_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _roster.TryJoin(PadA, null));
        }

        // ---- チーム ----

        [Test]
        public void TryJoin_AlternatesTeamsStartingWithA()
        {
            _roster.TryJoin(Keyboard, P1);
            _roster.TryJoin(Keyboard, P2);
            _roster.TryJoin(PadA, Pad);
            _roster.TryJoin(PadB, Pad);

            Assert.That(_roster.Players[0].Team, Is.EqualTo(Team.A));
            Assert.That(_roster.Players[1].Team, Is.EqualTo(Team.B));
            Assert.That(_roster.Players[2].Team, Is.EqualTo(Team.A));
            Assert.That(_roster.Players[3].Team, Is.EqualTo(Team.B));
        }

        [Test]
        public void SetTeam_ChangesTeamOfThatPlayer()
        {
            _roster.TryJoin(Keyboard, P1);
            _roster.TryJoin(Keyboard, P2);

            _roster.SetTeam(1, Team.A);

            Assert.That(_roster.Players[1].Team, Is.EqualTo(Team.A));
        }

        [Test]
        public void SetTeam_UnknownSlot_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _roster.SetTeam(0, Team.A));
        }

        // ---- 抜ける ----

        [Test]
        public void Leave_RemovesPlayerAndRenumbersLaterOnesKeepingTheirTeams()
        {
            _roster.TryJoin(Keyboard, P1); // 0: A
            _roster.TryJoin(PadA, Pad);    // 1: B
            _roster.TryJoin(PadB, Pad);    // 2: A
            _roster.SetTeam(2, Team.B);

            Assert.That(_roster.Leave(PadA, Pad), Is.True);

            Assert.That(_roster.Players.Count, Is.EqualTo(2));
            Assert.That(_roster.Players[1].DeviceId, Is.EqualTo(PadB));
            Assert.That(_roster.Players[1].Slot, Is.EqualTo(1));
            Assert.That(_roster.Players[1].Team, Is.EqualTo(Team.B));
        }

        [Test]
        public void Leave_NotJoined_ReturnsFalse()
        {
            Assert.That(_roster.Leave(PadA, Pad), Is.False);
        }

        [Test]
        public void Clear_RemovesEveryone()
        {
            _roster.TryJoin(Keyboard, P1);
            _roster.TryJoin(Keyboard, P2);

            _roster.Clear();

            Assert.That(_roster.Players, Is.Empty);
        }

        // ---- 始められるか（1vs1 / 2vs2）----

        [Test]
        public void CanStart_TwoPlayersOnOppositeTeams_IsTrue()
        {
            _roster.TryJoin(Keyboard, P1);
            _roster.TryJoin(Keyboard, P2);

            Assert.That(_roster.CanStart, Is.True);
        }

        [Test]
        public void CanStart_FourPlayersTwoPerTeam_IsTrue()
        {
            _roster.TryJoin(Keyboard, P1);
            _roster.TryJoin(Keyboard, P2);
            _roster.TryJoin(PadA, Pad);
            _roster.TryJoin(PadB, Pad);

            Assert.That(_roster.CanStart, Is.True);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(3)]
        public void CanStart_WrongNumberOfPlayers_IsFalse(int count)
        {
            var devices = new[] { 1, 2, 3 };
            for (var i = 0; i < count; i++)
            {
                _roster.TryJoin(devices[i], Pad);
            }

            Assert.That(_roster.CanStart, Is.False);
        }

        [Test]
        public void CanStart_UnevenTeams_IsFalse()
        {
            _roster.TryJoin(Keyboard, P1);
            _roster.TryJoin(Keyboard, P2);
            _roster.SetTeam(1, Team.A);

            Assert.That(_roster.CanStart, Is.False);
        }

        [Test]
        public void TeamAssignment_CanStart_ChecksCountAndBalance()
        {
            Assert.That(TeamAssignment.CanStart(new[] { Team.A, Team.B }), Is.True);
            Assert.That(TeamAssignment.CanStart(new[] { Team.B, Team.A, Team.A, Team.B }), Is.True);
            Assert.That(TeamAssignment.CanStart(new[] { Team.A, Team.A, Team.A, Team.B }), Is.False);
            Assert.That(TeamAssignment.CanStart(new[] { Team.A, Team.B, Team.A }), Is.False);
            Assert.That(TeamAssignment.CanStart(new Team[0]), Is.False);
        }

        [TestCase(0, Team.A)]
        [TestCase(1, Team.B)]
        [TestCase(2, Team.A)]
        [TestCase(3, Team.B)]
        public void TeamAssignment_DefaultTeam_Alternates(int slot, Team expected)
        {
            Assert.That(TeamAssignment.DefaultTeam(slot), Is.EqualTo(expected));
        }
    }
}
