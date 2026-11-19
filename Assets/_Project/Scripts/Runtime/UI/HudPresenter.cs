using MochiHockey.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// Match シーンの画面（UI Toolkit）：HUD（スコア・残り時間・READY・GOAL!）、ポーズメニュー、リザルト（REQ-053, REQ-051）。
    /// 表示内容は HudViewModel が決める。ボタンはマウスでも、キーボード・ゲームパッドでも選べる。
    /// - ポーズ中：再開は Esc / Start（PlayerInputReader のポーズ切り替え）、タイトルへは Backspace / Select
    /// - リザルト：もう一度は Enter / A / Start、タイトルへは Esc / B（出てから ResultInputDelay 秒たってから受け付ける）
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class HudPresenter : MonoBehaviour
    {
        [SerializeField] private MatchManager match;

        private Label _scoreA;
        private Label _scoreB;
        private Label _timer;
        private Label _kickoff;
        private VisualElement _goalPanel;
        private Label _goalDetail;
        private VisualElement _pauseOverlay;
        private Label _pauseTitle;
        private Label _pauseHelp;
        private VisualElement _resultOverlay;
        private Label _resultTitle;
        private Label _resultScore;

        private HudViewModel _viewModel;
        private float _resultShownAt = -1f;

        public MatchManager Match
        {
            get => match;
            set => match = value;
        }

        public HudViewModel ViewModel => _viewModel;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _scoreA = root.Q<Label>("score-a");
            _scoreB = root.Q<Label>("score-b");
            _timer = root.Q<Label>("timer");
            _kickoff = root.Q<Label>("kickoff");
            _goalPanel = root.Q("goal-panel");
            _goalDetail = root.Q<Label>("goal-detail");
            _pauseOverlay = root.Q("pause-overlay");
            _resultOverlay = root.Q("result-overlay");
            _resultTitle = root.Q<Label>("result-title");
            _resultScore = root.Q<Label>("result-score");

            root.Q<Label>("team-a-name").text = UiText.TeamNameA;
            root.Q<Label>("team-b-name").text = UiText.TeamNameB;
            root.Q<Label>("goal-text").text = UiText.Goal;
            _pauseTitle = root.Q<Label>("pause-title");
            _pauseHelp = root.Q<Label>("pause-help");
            _pauseTitle.text = UiText.Paused;
            _pauseHelp.text = UiText.PauseHelp;
            root.Q<Label>("result-help").text = UiText.ResultHelp;

            var resume = root.Q<Button>("resume-button");
            resume.text = UiText.Resume;
            resume.clicked += Resume;
            var pauseTitle = root.Q<Button>("pause-title-button");
            pauseTitle.text = UiText.ToTitle;
            pauseTitle.clicked += GoToTitle;
            var again = root.Q<Button>("again-button");
            again.text = UiText.PlayAgain;
            again.clicked += PlayAgain;
            var resultTitle = root.Q<Button>("result-title-button");
            resultTitle.text = UiText.ToTitle;
            resultTitle.clicked += GoToTitle;

            _pauseOverlay.style.display = DisplayStyle.None;
            _resultOverlay.style.display = DisplayStyle.None;
            _goalPanel.style.display = DisplayStyle.None;
        }

        private void Start()
        {
            if (match == null)
            {
                match = FindFirstObjectByType<MatchManager>();
            }

            if (match == null || match.Rules == null)
            {
                Debug.LogError($"[MochiHockey] {name}: MatchManager is not found. HudPresenter is disabled.", this);
                enabled = false;
                return;
            }

            _viewModel = new HudViewModel(match.Rules, match.Config.Match.GoDisplayTime);
            Refresh();
        }

        private void Update()
        {
            if (_viewModel == null)
            {
                return;
            }

            Refresh();
            HandleMenuInput();
        }

        public void Resume() => match.SetPaused(false);

        public void GoToTitle() => match.GoToTitle();

        public void PlayAgain() => match.PlayAgain();

        /// <summary>数字が変わったら、大きく出して元の大きさに縮める（3・2・1・GO!）。</summary>
        private void SetKickoffText(string text)
        {
            if (_kickoff.text == text)
            {
                return;
            }

            _kickoff.text = text;
            _kickoff.AddToClassList("kickoff--pop");
            _kickoff.schedule.Execute(() => _kickoff.RemoveFromClassList("kickoff--pop")).StartingIn(30);
        }

        private void Refresh()
        {
            _scoreA.text = _viewModel.ScoreAText;
            _scoreB.text = _viewModel.ScoreBText;
            _timer.text = _viewModel.TimerText;
            Show(_kickoff, _viewModel.ShowKickoff);
            if (_viewModel.ShowKickoff)
            {
                SetKickoffText(_viewModel.KickoffText);
            }

            Show(_goalPanel, _viewModel.ShowGoal);
            if (_viewModel.ShowGoal)
            {
                _goalDetail.text = _viewModel.GoalDetailText;
            }

            var showResult = _viewModel.ShowResult;
            Show(_resultOverlay, showResult);
            if (showResult)
            {
                if (_resultShownAt < 0f)
                {
                    _resultShownAt = Time.unscaledTime;
                }

                _resultTitle.text = _viewModel.ResultTitle;
                _resultScore.text = _viewModel.ResultScore;
            }
            else
            {
                _resultShownAt = -1f;
            }

            Show(_pauseOverlay, match.IsPaused && !showResult);
            var disconnected = match.DisconnectedPlayerCount > 0;
            _pauseTitle.text = disconnected ? UiText.ControllerDisconnected : UiText.Paused;
            _pauseHelp.text = disconnected ? UiText.ReconnectHelp : UiText.PauseHelp;
        }

        private void HandleMenuInput()
        {
            var keyboard = Keyboard.current;
            if (_viewModel.ShowResult)
            {
                if (Time.unscaledTime - _resultShownAt < match.Config.Match.ResultInputDelay)
                {
                    return;
                }

                if (Pressed(keyboard?.enterKey) || Pressed(keyboard?.numpadEnterKey) || AnyPad(p => p.buttonSouth.wasPressedThisFrame || p.startButton.wasPressedThisFrame))
                {
                    PlayAgain();
                }
                else if (Pressed(keyboard?.escapeKey) || AnyPad(p => p.buttonEast.wasPressedThisFrame))
                {
                    GoToTitle();
                }
            }
            else if (match.IsPaused)
            {
                if (Pressed(keyboard?.backspaceKey) || AnyPad(p => p.selectButton.wasPressedThisFrame))
                {
                    GoToTitle();
                }
            }
        }

        private static bool Pressed(UnityEngine.InputSystem.Controls.ButtonControl key) => key != null && key.wasPressedThisFrame;

        private static bool AnyPad(System.Func<Gamepad, bool> pressed)
        {
            foreach (var pad in Gamepad.all)
            {
                if (pressed(pad))
                {
                    return true;
                }
            }

            return false;
        }

        private static void Show(VisualElement element, bool visible)
        {
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
