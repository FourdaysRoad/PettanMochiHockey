using MochiHockey.Core;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// 画面に出す文字列。今は英語（日本語フォントがまだないため）。
    /// 日本語にするときは、ここをまとめて差し替える（または Localization パッケージに移す）。
    /// </summary>
    public static class UiText
    {
        public const string GameTitle = "Pettan! MOCHI HOCKEY";
        public const string PressAnyButton = "Press any key, click, or press a gamepad button";

        public const string SetupTitle = "WHO'S PLAYING?";
        public const string JoinHelp =
            "Join:  1P = J   /   2P = Numpad 1   /   Gamepad = A\n" +
            "Switch team:  move left / right        Leave:  K / Numpad 2 / B        Fullscreen:  F";
        public const string GamepadHint =
            "Gamepad not showing up? Press any button on it once so the browser can detect it.";

        public const string SlotEmpty = "Press to join";
        public const string DeviceKeyboardP1 = "Keyboard  (WASD + J K L)";
        public const string DeviceKeyboardP2 = "Keyboard  (Arrows + Numpad 1 2 3)";
        public const string DeviceGamepad = "Gamepad";

        public const string TeamA = "TEAM A  (left)";
        public const string TeamB = "TEAM B  (right)";

        public const string NeedTwoOrFour = "2 players (1 vs 1) or 4 players (2 vs 2) needed";
        public const string TeamsMustBeEven = "Teams must be even - move left / right to switch";
        public const string PressToStart = "Press ENTER or START to play!";

        // ---- 試合中（HUD・ポーズ・リザルト）----

        public const string TeamNameA = "TEAM A";
        public const string TeamNameB = "TEAM B";
        public const string Ready = "READY...";
        public const string SuddenDeath = "SUDDEN DEATH";
        public const string Goal = "GOAL!";
        public const string Wins = "WINS!";

        public const string Paused = "PAUSED";
        public const string Resume = "Resume";
        public const string ToTitle = "Back to title";
        public const string PauseHelp = "Resume: Esc / Start      Title: Backspace / Select";

        public const string PlayAgain = "Play again";
        public const string ResultHelp = "Play again: Enter / A      Title: Esc / B";

        // ---- ワールド空間のゲージ ----

        public static string LevelLabel(MochiLevel level) => $"Lv.{(int)level}";

        public static string MultiplierLabel(int points) => $"x{points}";

        /// <summary>プレイヤー番号（1〜4）の表示。番号が決まっていなければ "?"。</summary>
        public static string PlayerLabel(int number) => number >= 1 ? $"{number}P" : "?";
    }
}
