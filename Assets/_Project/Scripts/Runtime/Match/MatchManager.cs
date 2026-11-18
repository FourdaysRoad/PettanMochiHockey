using System;
using System.Collections.Generic;
using MochiHockey.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// Match シーンの司令塔（REQ-001〜005）。Core の MatchRules を進め、その結果をシーンに反映する。
    /// - キックオフ：シーンに置いた最初の位置へプレイヤーと餅を戻し、カウントダウン中はプレイヤーを止める
    /// - ゴール：GoalPost の知らせを MatchRules に渡し、得点が入ったら餅を消す。再出現の知らせで中央に Lv.1 を出す
    ///   （プレイヤーの位置は戻さない）
    /// - ポーズ：Time.timeScale = 0 にする（Core に渡す経過時間も 0 になる）
    /// - 試合終了：プレイヤーを止める。PlayAgain で同じ参加者でやり直し、GoToTitle で Menu へ戻る
    /// - ゲームパッドが抜けたら自動でポーズし、次につながったゲームパッドをそのプレイヤーに割り当て直す
    ///   （ブラウザでは挿し直したパッドが別のデバイスになることがあるため）。再開はポーズの解除で行う
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class MatchManager : MonoBehaviour
    {
        [SerializeField] private GameBalanceConfig config;
        [SerializeField] private MochiBall ball;
        [SerializeField] private string titleSceneName = "Menu";

        private readonly List<(PlayerController player, Vector3 position, Quaternion rotation)> _starts =
            new List<(PlayerController, Vector3, Quaternion)>();

        private readonly List<(PlayerInputReader reader, int lostDeviceId)> _waitingForPad =
            new List<(PlayerInputReader, int)>();

        private GoalPost[] _goals = Array.Empty<GoalPost>();
        private Vector3 _ballStart;
        private float _timeScaleBeforePause = 1f;

        public GameBalanceConfig Config
        {
            get => config;
            set => config = value;
        }

        public MochiBall Ball
        {
            get => ball;
            set => ball = value;
        }

        /// <summary>試合のルール（スコア・残り時間・フェーズ）。HUD はここを見る。</summary>
        public MatchRules Rules { get; private set; }

        public bool IsPaused { get; private set; }

        /// <summary>ゲームパッドが抜けて、挿し直しを待っているプレイヤーの数。</summary>
        public int DisconnectedPlayerCount => _waitingForPad.Count;

        /// <summary>ポーズの切り替え（true = 止めた）。</summary>
        public event Action<bool> PauseChanged;

        private void Awake()
        {
            if (config == null || ball == null)
            {
                Debug.LogError($"[MochiHockey] {name}: GameBalanceConfig or MochiBall is not assigned. MatchManager is disabled.", this);
                enabled = false;
                return;
            }

            var settings = config.ToSettings();
            Rules = new MatchRules(settings.Match, new MochiGrowth(settings.Mochi));
            Rules.PhaseChanged += OnPhaseChanged;
            Rules.Scored += OnScored;
            Rules.MochiRespawnRequested += OnMochiRespawnRequested;
        }

        private void Start()
        {
            // プレイヤーと餅の最初の位置を、キックオフの位置として覚えておく
            foreach (var player in FindObjectsByType<PlayerController>(FindObjectsSortMode.InstanceID))
            {
                _starts.Add((player, player.transform.position, player.transform.rotation));
            }

            _ballStart = ball.transform.position;

            _goals = FindObjectsByType<GoalPost>(FindObjectsSortMode.None);
            foreach (var goal in _goals)
            {
                goal.BallEntered += OnBallEntered;
            }

            InputSystem.onDeviceChange += OnDeviceChange;
            Rules.StartKickoff();
        }

        private void OnDestroy()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;

            foreach (var goal in _goals)
            {
                if (goal != null)
                {
                    goal.BallEntered -= OnBallEntered;
                }
            }

            if (IsPaused)
            {
                Time.timeScale = _timeScaleBeforePause;
            }
        }

        private void Update()
        {
            // ポーズ中は timeScale = 0 なので deltaTime も 0
            Rules.Tick(Time.deltaTime);
        }

        /// <summary>最初からやり直す（キックオフ）。</summary>
        public void Restart()
        {
            SetPaused(false);
            Rules.StartKickoff();
        }

        public void TogglePause() => SetPaused(!IsPaused);

        /// <summary>試合を止める・再開する。試合終了後は止めない。</summary>
        public void SetPaused(bool paused)
        {
            if (paused == IsPaused || (paused && Rules.Phase == MatchPhase.Finished))
            {
                return;
            }

            IsPaused = paused;
            if (paused)
            {
                _timeScaleBeforePause = Time.timeScale;
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = _timeScaleBeforePause;
            }

            PauseChanged?.Invoke(paused);
        }

        /// <summary>同じ参加者でもう一度（Match シーンを読み込み直す）。</summary>
        public void PlayAgain()
        {
            SetPaused(false);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        /// <summary>タイトル（Menu シーン）へ戻る。参加者は選び直すので捨てる。</summary>
        public void GoToTitle()
        {
            SetPaused(false);
            MatchSession.Clear();
            SceneManager.LoadScene(titleSceneName);
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (!(device is Gamepad))
            {
                return;
            }

            switch (change)
            {
                case InputDeviceChange.Removed:
                case InputDeviceChange.Disconnected:
                    OnPadLost(device.deviceId);
                    break;
                case InputDeviceChange.Added:
                case InputDeviceChange.Reconnected:
                    OnPadFound(device.deviceId);
                    break;
            }
        }

        private void OnPadLost(int deviceId)
        {
            var lost = false;
            foreach (var (player, _, _) in _starts)
            {
                var reader = player != null ? player.GetComponent<PlayerInputReader>() : null;
                if (reader != null && reader.ControlScheme == PlayerInputReader.GamepadScheme
                    && reader.DeviceId == deviceId && reader.HasDevice)
                {
                    reader.DropDevice();
                    _waitingForPad.Add((reader, deviceId));
                    lost = true;
                }
            }

            if (lost)
            {
                SetPaused(true);
            }
        }

        private void OnPadFound(int deviceId)
        {
            if (_waitingForPad.Count == 0 || IsUsedByAnyone(deviceId))
            {
                return;
            }

            // 同じパッドが戻ってきたらその人に、別のパッドなら抜けた順に割り当てる
            var index = _waitingForPad.FindIndex(w => w.lostDeviceId == deviceId);
            if (index < 0)
            {
                index = 0;
            }

            var (reader, lostDeviceId) = _waitingForPad[index];
            _waitingForPad.RemoveAt(index);
            if (reader == null)
            {
                return;
            }

            reader.Configure(PlayerInputReader.GamepadScheme, deviceId);
            if (MatchSession.Current != null)
            {
                MatchSession.Current.ReplaceDevice(lostDeviceId, deviceId, PlayerInputReader.GamepadScheme);
            }
        }

        private bool IsUsedByAnyone(int deviceId)
        {
            foreach (var (player, _, _) in _starts)
            {
                var reader = player != null ? player.GetComponent<PlayerInputReader>() : null;
                if (reader != null && reader.HasDevice && reader.DeviceId == deviceId)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnPhaseChanged(MatchPhase phase)
        {
            switch (phase)
            {
                case MatchPhase.Kickoff:
                    PlaceForKickoff();
                    SetPlayersLocked(true);
                    break;
                case MatchPhase.Playing:
                case MatchPhase.SuddenDeath:
                    SetPlayersLocked(false);
                    break;
                case MatchPhase.Finished:
                    SetPlayersLocked(true);
                    break;
            }
        }

        private void OnBallEntered(GoalPost goal, MochiBall enteredBall)
        {
            if (enteredBall == ball && !IsPaused)
            {
                Rules.RegisterGoal(goal.DefendingTeam, ball.Level);
            }
        }

        private void OnScored(Team team, int points)
        {
            ball.Hide();
        }

        private void OnMochiRespawnRequested()
        {
            ball.ResetTo(_ballStart);
        }

        private void PlaceForKickoff()
        {
            foreach (var (player, position, rotation) in _starts)
            {
                if (player != null)
                {
                    player.ResetForKickoff(position, rotation);
                }
            }

            ball.ResetTo(_ballStart);
        }

        private void SetPlayersLocked(bool locked)
        {
            foreach (var (player, _, _) in _starts)
            {
                if (player != null)
                {
                    player.Locked = locked;
                }
            }
        }
    }
}
