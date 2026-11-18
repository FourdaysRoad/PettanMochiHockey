using System;
using MochiHockey.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// 対戦設定での参加受付（REQ-041〜043）。ボタンを押したデバイスから順に 1P〜4P として参加させる。
    /// - 参加：キーボード 1P は J、2P はテンキー 1、ゲームパッドは A（South）
    /// - 抜ける：キーボード 1P は K、2P はテンキー 2、ゲームパッドは B（East）
    /// - チーム：参加した人が左（A / ← / 十字キー・スティックの左）でチーム A、右でチーム B
    /// - 開始：Enter かゲームパッドの Start（1vs1 か 2vs2 のときだけ）
    /// 画面の表示は Menu の UI（TASK-0022）が RosterChanged を見て行う。
    /// </summary>
    public sealed class PlayerJoinManager : MonoBehaviour
    {
        [SerializeField] private string matchSceneName = "Match";

        private int _acceptFromFrame;

        public JoinRoster Roster { get; } = new JoinRoster();

        /// <summary>ボタンでの参加・開始を受け付けるか（タイトル画面の間は受け付けない）。</summary>
        public bool AcceptingInput { get; set; } = true;

        /// <summary>参加者・チームが変わった。</summary>
        public event Action RosterChanged;

        public int TryJoin(int deviceId, string controlScheme)
        {
            var slot = Roster.TryJoin(deviceId, controlScheme);
            if (slot >= 0)
            {
                RosterChanged?.Invoke();
            }

            return slot;
        }

        public bool Leave(int deviceId, string controlScheme)
        {
            var left = Roster.Leave(deviceId, controlScheme);
            if (left)
            {
                RosterChanged?.Invoke();
            }

            return left;
        }

        public void SetTeam(int slot, Team team)
        {
            Roster.SetTeam(slot, team);
            RosterChanged?.Invoke();
        }

        /// <summary>次のフレームから受け付ける（タイトルを抜けた入力で参加まで済まないように）。</summary>
        public void AcceptInputFromNextFrame()
        {
            AcceptingInput = true;
            _acceptFromFrame = Time.frameCount + 1;
        }

        /// <summary>そのデバイスと割り当てで参加している人のチームを変える。参加していなければ何もしない。</summary>
        public void SetTeamOf(int deviceId, string controlScheme, Team team)
        {
            foreach (var player in Roster.Players)
            {
                if (player.DeviceId == deviceId && player.ControlScheme == controlScheme)
                {
                    if (player.Team != team)
                    {
                        SetTeam(player.Slot, team);
                    }

                    return;
                }
            }
        }

        /// <summary>1vs1 か 2vs2 なら参加者を MatchSession に保存して Match シーンへ移る。始められなければ false。</summary>
        public bool StartMatch()
        {
            if (!Roster.CanStart)
            {
                return false;
            }

            MatchSession.Create(Roster.Players);
            SceneManager.LoadScene(matchSceneName);
            return true;
        }

        private void Update()
        {
            if (!AcceptingInput || Time.frameCount < _acceptFromFrame)
            {
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                PollKeyboard(keyboard, keyboard.jKey, keyboard.kKey, PlayerInputReader.KeyboardP1Scheme);
                PollKeyboard(keyboard, keyboard.numpad1Key, keyboard.numpad2Key, PlayerInputReader.KeyboardP2Scheme);
                PollTeam(keyboard.deviceId, PlayerInputReader.KeyboardP1Scheme, keyboard.aKey.wasPressedThisFrame, keyboard.dKey.wasPressedThisFrame);
                PollTeam(keyboard.deviceId, PlayerInputReader.KeyboardP2Scheme, keyboard.leftArrowKey.wasPressedThisFrame, keyboard.rightArrowKey.wasPressedThisFrame);
                if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
                {
                    StartMatch();
                }
            }

            foreach (var pad in Gamepad.all)
            {
                if (pad.buttonSouth.wasPressedThisFrame)
                {
                    TryJoin(pad.deviceId, PlayerInputReader.GamepadScheme);
                }
                else if (pad.buttonEast.wasPressedThisFrame)
                {
                    Leave(pad.deviceId, PlayerInputReader.GamepadScheme);
                }
                else if (pad.startButton.wasPressedThisFrame)
                {
                    StartMatch();
                }

                PollTeam(pad.deviceId, PlayerInputReader.GamepadScheme,
                    pad.dpad.left.wasPressedThisFrame || pad.leftStick.left.wasPressedThisFrame,
                    pad.dpad.right.wasPressedThisFrame || pad.leftStick.right.wasPressedThisFrame);
            }
        }

        private void PollTeam(int deviceId, string scheme, bool left, bool right)
        {
            if (left)
            {
                SetTeamOf(deviceId, scheme, Team.A);
            }
            else if (right)
            {
                SetTeamOf(deviceId, scheme, Team.B);
            }
        }

        private void PollKeyboard(Keyboard keyboard, UnityEngine.InputSystem.Controls.KeyControl join,
            UnityEngine.InputSystem.Controls.KeyControl leave, string scheme)
        {
            if (join.wasPressedThisFrame)
            {
                TryJoin(keyboard.deviceId, scheme);
            }
            else if (leave.wasPressedThisFrame)
            {
                Leave(keyboard.deviceId, scheme);
            }
        }
    }
}
