using System;
using MochiHockey.Core;
using UnityEngine;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// 効果音と BGM を鳴らす（TASK-0029）。音はすべてコードで合成する（SoundSynth / BgmSynth）。
    /// - プレイヤー・餅のイベント（PlayerController.AnyPlayerEvent / MochiBall.AnyBallEvent）に効果音を対応付ける
    /// - 試合のルール（MatchManager.Rules）の得点・開始・終了に、ゴールの音と笛を鳴らす
    /// - BGM は、最初の入力（AudioUnlocker）のあとに鳴らし始める（REQ-052）。それまでは効果音も鳴らさない
    /// Menu と Match の両方のシーンに 1 つずつ置く。音量・テンポは GameBalanceConfig の Audio。
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        [SerializeField] private GameBalanceConfig config;

        [Tooltip("BGM を鳴らすか（試合の BGM。タイトルでも同じ曲を使う）")]
        [SerializeField] private bool playBgm = true;

        private GameBalanceConfig.AudioConfig _audio;
        private AudioSource _bgm;
        private AudioSource[] _voices;
        private int _nextVoice;
        private MatchRules _rules;
        private MatchPhase _phase;
        private int _countdownDigit = -1;

        public GameBalanceConfig Config
        {
            get => config;
            set => config = value;
        }

        public bool PlayBgm
        {
            get => playBgm;
            set => playBgm = value;
        }

        /// <summary>BGM を鳴らし始めたか。</summary>
        public bool BgmStarted { get; private set; }

        public AudioSource BgmSource => _bgm;

        /// <summary>最後に鳴らした効果音（テスト用）。</summary>
        public SoundId? LastSound { get; private set; }

        /// <summary>これまでに鳴らした効果音の数（テスト用）。</summary>
        public int PlayedCount { get; private set; }

        /// <summary>効果音を鳴らした（テスト用）。</summary>
        public event Action<SoundId> Played;

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError($"[MochiHockey] {name}: GameBalanceConfig is not assigned. AudioManager is disabled.", this);
                enabled = false;
                return;
            }

            _audio = config.Audio;
            _bgm = CreateSource("BGM");
            _bgm.loop = true;
            _bgm.volume = _audio.BgmVolume;

            _voices = new AudioSource[Mathf.Max(1, _audio.Voices)];
            for (var i = 0; i < _voices.Length; i++)
            {
                _voices[i] = CreateSource($"SE{i}");
            }
        }

        private void OnEnable()
        {
            if (_audio == null)
            {
                return;
            }

            PlayerController.AnyPlayerEvent += OnPlayerEvent;
            MochiBall.AnyBallEvent += OnBallEvent;
        }

        private void OnDisable()
        {
            PlayerController.AnyPlayerEvent -= OnPlayerEvent;
            MochiBall.AnyBallEvent -= OnBallEvent;
            BindRules(null);
        }

        private void Start()
        {
            var match = FindFirstObjectByType<MatchManager>();
            if (match != null)
            {
                BindRules(match.Rules);
            }
        }

        private void Update()
        {
            UpdateCountdown();

            if (playBgm && !BgmStarted && AudioUnlocker.Unlocked)
            {
                _bgm.clip = SoundBank.Bgm(_audio.SampleRate, _audio.BgmTempo);
                _bgm.Play();
                BgmStarted = true;
            }
        }

        /// <summary>キックオフの 3・2・1 で「ピッ」と鳴らす（GO! は笛）。</summary>
        public void UpdateCountdown()
        {
            if (_rules == null || _rules.Phase != MatchPhase.Kickoff)
            {
                _countdownDigit = -1;
                return;
            }

            var digit = Mathf.Max(1, Mathf.CeilToInt(_rules.KickoffRemaining - 1e-4f));
            if (digit != _countdownDigit)
            {
                _countdownDigit = digit;
                Play(SoundId.Count);
            }
        }

        /// <summary>試合のルールに、ゴールの音と笛をつなぐ（MatchManager が作り直したときにも呼べる）。</summary>
        public void BindRules(MatchRules rules)
        {
            if (_rules != null)
            {
                _rules.Scored -= OnScored;
                _rules.PhaseChanged -= OnPhaseChanged;
            }

            _rules = rules;
            if (_rules != null)
            {
                _phase = _rules.Phase;
                _rules.Scored += OnScored;
                _rules.PhaseChanged += OnPhaseChanged;
            }
        }

        /// <summary>効果音を鳴らす。最初の入力の前は鳴らさない（REQ-052）。</summary>
        public void Play(SoundId id, float pitch = 1f, float volume = 1f)
        {
            if (_voices == null || !AudioUnlocker.Unlocked)
            {
                return;
            }

            var voice = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _voices.Length;
            voice.Stop();
            voice.clip = SoundBank.Sound(id, _audio.SampleRate);
            voice.pitch = pitch;
            voice.volume = Mathf.Clamp01(_audio.SeVolume * volume);
            voice.Play();

            LastSound = id;
            PlayedCount++;
            Played?.Invoke(id);
        }

        private void OnPlayerEvent(PlayerController player, PlayerEvent e)
        {
            switch (e)
            {
                case PlayerEvent.Swung:
                    Play(SoundId.Swing, Jitter(), 0.6f);
                    break;
                case PlayerEvent.Dashed:
                    Play(SoundId.Dash, Jitter(), 0.7f);
                    break;
                case PlayerEvent.Hit:
                    Play(SoundId.Pettan, Jitter());
                    break;
                case PlayerEvent.LaunchBounced:
                    Play(SoundId.LaunchBounce, Jitter());
                    break;
                case PlayerEvent.Stuck:
                    Play(SoundId.Stuck);
                    break;
                case PlayerEvent.Recovered:
                    Play(SoundId.Pon);
                    break;
            }
        }

        private void OnBallEvent(MochiBall ball, BallEvent e)
        {
            switch (e)
            {
                case BallEvent.Shot:
                    // チャージするほど低く、大きく
                    var stage = Mathf.Max(0, ball.LastShotStage);
                    Play(SoundId.Shot, 1f - 0.1f * stage, 0.8f + 0.1f * stage);
                    break;
                case BallEvent.RiceAbsorbed:
                    Play(SoundId.RiceGet, Jitter(), 0.6f);
                    break;
                case BallEvent.LevelUp:
                    Play(SoundId.LevelUp);
                    break;
                case BallEvent.Bounced:
                    Play(SoundId.BallBounce, Jitter(), 0.7f);
                    break;
            }
        }

        private void OnScored(Team team, int points)
        {
            Play(SoundId.Goal);
        }

        private void OnPhaseChanged(MatchPhase next)
        {
            var previous = _phase;
            _phase = next;
            if ((previous == MatchPhase.Kickoff && next == MatchPhase.Playing) || next == MatchPhase.SuddenDeath)
            {
                Play(SoundId.Whistle);
            }
            else if (next == MatchPhase.Finished)
            {
                Play(SoundId.FinalWhistle);
            }
        }

        private float Jitter() => 1f + UnityEngine.Random.Range(-_audio.PitchJitter, _audio.PitchJitter);

        private AudioSource CreateSource(string sourceName)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f; // 2D（画面全体で同じ大きさ）
            return source;
        }
    }
}
