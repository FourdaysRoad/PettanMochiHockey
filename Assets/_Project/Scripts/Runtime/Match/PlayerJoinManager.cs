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
    /// - 開始：Enter かゲームパッドの Start（1vs1 か 2vs2 のときだけ）
    /// 画面の表示は Menu の UI（TASK-0022）が RosterChanged を見て行う。
    /// </summary>
    public sealed class PlayerJoinManager : MonoBehaviour
    {
        [SerializeField] private string matchSceneName = "Match";

        public JoinRoster Roster { get; } = new JoinRoster();

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
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                PollKeyboard(keyboard, keyboard.jKey, keyboard.kKey, PlayerInputReader.KeyboardP1Scheme);
                PollKeyboard(keyboard, keyboard.numpad1Key, keyboard.numpad2Key, PlayerInputReader.KeyboardP2Scheme);
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
