using System;
using System.Collections.Generic;
using MochiHockey.Core;

namespace MochiHockey.Runtime
{
    public enum MenuScreen
    {
        /// <summary>タイトル（どれかの入力で対戦設定へ）。</summary>
        Title,

        /// <summary>対戦設定（参加・チーム・開始）。</summary>
        Setup,
    }

    /// <summary>参加枠 1 つ分の表示内容。番号は色だけでなく文字でも示す。</summary>
    public readonly struct SlotView
    {
        public SlotView(string number, bool joined, string device, Team team, string teamText)
        {
            Number = number;
            Joined = joined;
            Device = device;
            Team = team;
            TeamText = teamText;
        }

        public string Number { get; }
        public bool Joined { get; }
        public string Device { get; }
        public Team Team { get; }
        public string TeamText { get; }
    }

    /// <summary>
    /// Menu シーンの画面の状態と表示内容（REQ-041, REQ-043, REQ-051）。画面の部品には依存しない。
    /// </summary>
    public sealed class MenuViewModel
    {
        public MenuScreen Screen { get; private set; } = MenuScreen.Title;

        public event Action Changed;

        public void ProceedFromTitle() => SetScreen(MenuScreen.Setup);

        public void BackToTitle() => SetScreen(MenuScreen.Title);

        /// <summary>1P〜4P の 4 枠分の表示内容。</summary>
        public IReadOnlyList<SlotView> BuildSlots(JoinRoster roster)
        {
            var slots = new SlotView[JoinRoster.MaxPlayers];
            for (var i = 0; i < slots.Length; i++)
            {
                var number = $"{i + 1}P";
                if (i < roster.Players.Count)
                {
                    var player = roster.Players[i];
                    slots[i] = new SlotView(number, true, DeviceText(player.ControlScheme), player.Team,
                        player.Team == Team.A ? UiText.TeamA : UiText.TeamB);
                }
                else
                {
                    slots[i] = new SlotView(number, false, UiText.SlotEmpty, Team.A, string.Empty);
                }
            }

            return slots;
        }

        public string StartHint(JoinRoster roster)
        {
            if (roster.CanStart)
            {
                return UiText.PressToStart;
            }

            var count = roster.Players.Count;
            return count == 2 || count == 4 ? UiText.TeamsMustBeEven : UiText.NeedTwoOrFour;
        }

        private static string DeviceText(string scheme)
        {
            switch (scheme)
            {
                case PlayerInputReader.KeyboardP1Scheme:
                    return UiText.DeviceKeyboardP1;
                case PlayerInputReader.KeyboardP2Scheme:
                    return UiText.DeviceKeyboardP2;
                default:
                    return UiText.DeviceGamepad;
            }
        }

        private void SetScreen(MenuScreen screen)
        {
            if (Screen == screen)
            {
                return;
            }

            Screen = screen;
            Changed?.Invoke();
        }
    }
}
