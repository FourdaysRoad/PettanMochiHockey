using System;
using MochiHockey.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UIElements;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// Menu シーンの画面（UI Toolkit）。MenuViewModel と PlayerJoinManager の内容を UXML に表示する（REQ-041〜043, 051, 052）。
    /// - タイトル：どれかの入力で音声を有効にして、対戦設定へ進む
    /// - 対戦設定：参加枠（番号・デバイス・チーム）、開始の案内、ゲームパッドの案内を表示する
    /// タイトルを抜けたのと同じ入力で参加まで済まないよう、参加の受け付けは次のフレームから始める。
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MenuPresenter : MonoBehaviour
    {
        private const string JoinedClass = "slot--joined";
        private const string TeamAClass = "slot--team-a";
        private const string TeamBClass = "slot--team-b";

        [SerializeField] private PlayerJoinManager joinManager;

        private VisualElement _titleScreen;
        private VisualElement _setupScreen;
        private Label _startHint;
        private readonly VisualElement[] _slots = new VisualElement[JoinRoster.MaxPlayers];
        private IDisposable _anyButton;

        public MenuViewModel ViewModel { get; } = new MenuViewModel();

        public PlayerJoinManager JoinManager
        {
            get => joinManager;
            set => joinManager = value;
        }

        private void OnEnable()
        {
            if (joinManager == null)
            {
                joinManager = FindFirstObjectByType<PlayerJoinManager>();
            }

            var root = GetComponent<UIDocument>().rootVisualElement;
            _titleScreen = root.Q("title-screen");
            _setupScreen = root.Q("setup-screen");
            _startHint = root.Q<Label>("start-hint");
            for (var i = 0; i < _slots.Length; i++)
            {
                _slots[i] = root.Q($"slot-{i}");
            }

            root.Q<Label>("game-title").text = UiText.GameTitle;
            root.Q<Label>("press-start").text = UiText.PressAnyButton;
            root.Q<Label>("setup-title").text = UiText.SetupTitle;
            root.Q<Label>("join-help").text = UiText.JoinHelp;
            root.Q<Label>("gamepad-hint").text = UiText.GamepadHint;

            ViewModel.Changed += Refresh;
            if (joinManager != null)
            {
                joinManager.RosterChanged += Refresh;
                joinManager.AcceptingInput = ViewModel.Screen == MenuScreen.Setup;
            }

            if (ViewModel.Screen == MenuScreen.Title)
            {
                _anyButton = InputSystem.onAnyButtonPress.CallOnce(_ => ProceedFromTitle());
            }

            Refresh();
        }

        private void OnDisable()
        {
            _anyButton?.Dispose();
            _anyButton = null;
            ViewModel.Changed -= Refresh;
            if (joinManager != null)
            {
                joinManager.RosterChanged -= Refresh;
            }
        }

        /// <summary>タイトルから対戦設定へ進む（どれかの入力で呼ばれる）。</summary>
        public void ProceedFromTitle()
        {
            if (ViewModel.Screen != MenuScreen.Title)
            {
                return;
            }

            _anyButton?.Dispose();
            _anyButton = null;
            AudioUnlocker.Unlock();
            ViewModel.ProceedFromTitle();
            if (joinManager != null)
            {
                joinManager.AcceptInputFromNextFrame();
            }
        }

        private void Refresh()
        {
            var onTitle = ViewModel.Screen == MenuScreen.Title;
            _titleScreen.style.display = onTitle ? DisplayStyle.Flex : DisplayStyle.None;
            _setupScreen.style.display = onTitle ? DisplayStyle.None : DisplayStyle.Flex;
            if (onTitle || joinManager == null)
            {
                return;
            }

            var roster = joinManager.Roster;
            var slots = ViewModel.BuildSlots(roster);
            for (var i = 0; i < _slots.Length; i++)
            {
                var element = _slots[i];
                var view = slots[i];
                element.Q<Label>(className: "slot-number").text = view.Number;
                element.Q<Label>(className: "slot-device").text = view.Device;
                element.Q<Label>(className: "slot-team").text = view.TeamText;
                element.EnableInClassList(JoinedClass, view.Joined);
                element.EnableInClassList(TeamAClass, view.Joined && view.Team == Team.A);
                element.EnableInClassList(TeamBClass, view.Joined && view.Team == Team.B);
            }

            _startHint.text = ViewModel.StartHint(roster);
        }
    }
}
