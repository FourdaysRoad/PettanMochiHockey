using System.Linq;
using MochiHockey.Core;
using MochiHockey.Runtime;
using NUnit.Framework;

namespace MochiHockey.Tests.EditMode
{
    /// <summary>TASK-0022：タイトル → 対戦設定の画面の切り替えと、参加枠の表示内容。</summary>
    public class MenuViewModelTests
    {
        private const int Keyboard = 1;
        private const int Pad = 10;

        private MenuViewModel _menu;
        private JoinRoster _roster;
        private int _changes;

        [SetUp]
        public void SetUp()
        {
            _menu = new MenuViewModel();
            _roster = new JoinRoster();
            _changes = 0;
            _menu.Changed += () => _changes++;
        }

        // ---- 画面の切り替え（REQ-051）----

        [Test]
        public void StartsOnTitle()
        {
            Assert.That(_menu.Screen, Is.EqualTo(MenuScreen.Title));
        }

        [Test]
        public void ProceedFromTitle_GoesToSetupOnce()
        {
            _menu.ProceedFromTitle();
            _menu.ProceedFromTitle();

            Assert.That(_menu.Screen, Is.EqualTo(MenuScreen.Setup));
            Assert.That(_changes, Is.EqualTo(1));
        }

        [Test]
        public void BackToTitle_ReturnsToTitle()
        {
            _menu.ProceedFromTitle();

            _menu.BackToTitle();

            Assert.That(_menu.Screen, Is.EqualTo(MenuScreen.Title));
            Assert.That(_changes, Is.EqualTo(2));
        }

        // ---- 参加枠（REQ-041）----

        [Test]
        public void Slots_AlwaysFourFromOnePToFourP()
        {
            var slots = _menu.BuildSlots(_roster);

            Assert.That(slots.Select(s => s.Number), Is.EqualTo(new[] { "1P", "2P", "3P", "4P" }));
            Assert.That(slots.All(s => !s.Joined), Is.True);
            Assert.That(slots.All(s => s.Device == UiText.SlotEmpty), Is.True);
        }

        [Test]
        public void Slots_ShowDeviceAndTeamOfJoinedPlayers()
        {
            _roster.TryJoin(Pad, PlayerInputReader.GamepadScheme);
            _roster.TryJoin(Keyboard, PlayerInputReader.KeyboardP1Scheme);
            _roster.TryJoin(Keyboard, PlayerInputReader.KeyboardP2Scheme);

            var slots = _menu.BuildSlots(_roster);

            Assert.That(slots[0].Joined, Is.True);
            Assert.That(slots[0].Device, Is.EqualTo(UiText.DeviceGamepad));
            Assert.That(slots[0].Team, Is.EqualTo(Team.A));
            Assert.That(slots[0].TeamText, Is.EqualTo(UiText.TeamA));
            Assert.That(slots[1].Device, Is.EqualTo(UiText.DeviceKeyboardP1));
            Assert.That(slots[1].TeamText, Is.EqualTo(UiText.TeamB));
            Assert.That(slots[2].Device, Is.EqualTo(UiText.DeviceKeyboardP2));
            Assert.That(slots[3].Joined, Is.False);
            Assert.That(slots[3].TeamText, Is.Empty);
        }

        // ---- 開始の案内 ----

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(3)]
        public void StartHint_WrongNumberOfPlayers_AsksForTwoOrFour(int count)
        {
            for (var i = 0; i < count; i++)
            {
                _roster.TryJoin(100 + i, PlayerInputReader.GamepadScheme);
            }

            Assert.That(_menu.StartHint(_roster), Is.EqualTo(UiText.NeedTwoOrFour));
        }

        [Test]
        public void StartHint_UnevenTeams_AsksToBalance()
        {
            _roster.TryJoin(Keyboard, PlayerInputReader.KeyboardP1Scheme);
            _roster.TryJoin(Keyboard, PlayerInputReader.KeyboardP2Scheme);
            _roster.SetTeam(1, Team.A);

            Assert.That(_menu.StartHint(_roster), Is.EqualTo(UiText.TeamsMustBeEven));
        }

        [Test]
        public void StartHint_ReadyToStart_TellsHowToStart()
        {
            _roster.TryJoin(Keyboard, PlayerInputReader.KeyboardP1Scheme);
            _roster.TryJoin(Keyboard, PlayerInputReader.KeyboardP2Scheme);

            Assert.That(_menu.StartHint(_roster), Is.EqualTo(UiText.PressToStart));
        }

        [Test]
        public void Texts_AreNotEmptyAndGamepadHintMentionsPressingAButton()
        {
            foreach (var text in new[]
                     {
                         UiText.GameTitle, UiText.PressAnyButton, UiText.SetupTitle, UiText.JoinHelp,
                         UiText.GamepadHint, UiText.SlotEmpty, UiText.NeedTwoOrFour, UiText.TeamsMustBeEven,
                         UiText.PressToStart,
                     })
            {
                Assert.That(text, Is.Not.Null.And.Not.Empty);
            }

            StringAssert.Contains("button", UiText.GamepadHint);
        }
    }
}
