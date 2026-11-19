using UnityEngine;
using UnityEngine.InputSystem;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// Input Actions を読んで PlayerController に渡す。
    /// 1 台のキーボードを 2 人で使えるよう、プレイヤーごとに Input Actions を複製し、
    /// コントロールスキーム（KeyboardP1 / KeyboardP2 / Gamepad）のバインドだけを有効にする。
    /// PlayerController より先に Update する。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        public const string KeyboardP1Scheme = "KeyboardP1";
        public const string KeyboardP2Scheme = "KeyboardP2";
        public const string GamepadScheme = "Gamepad";

        /// <summary>デバイスを決めない（キーボードの割り当てなら今のキーボード、ゲームパッドならどれでも）。</summary>
        public const int AnyDevice = -1;

        [SerializeField] private InputActionAsset actions;

        [Tooltip("KeyboardP1 / KeyboardP2 / Gamepad")]
        [SerializeField] private string controlScheme = KeyboardP1Scheme;

        private int _deviceId = AnyDevice;
        private PlayerController _controller;

        public InputActionAsset Actions
        {
            get => actions;
            set => actions = value;
        }

        public string ControlScheme => controlScheme;

        /// <summary>読むデバイスの番号（決めていなければ AnyDevice）。</summary>
        public int DeviceId => _deviceId;

        /// <summary>デバイスにつながって入力を読めているか。</summary>
        public bool HasDevice => _instance != null;
        private InputActionAsset _instance;
        private InputAction _move;
        private InputAction _dash;
        private InputAction _shoot;
        private InputAction _swing;
        private InputAction _pause;
        private MatchManager _match;

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
        }

        private void Start()
        {
            // Match シーンでなければ（テストの組み立てなど）ないこともある
            _match = FindFirstObjectByType<MatchManager>();
        }

        private void OnEnable()
        {
            if (actions == null)
            {
                Debug.LogError($"[MochiHockey] {name}: InputActionAsset is not assigned.", this);
                enabled = false;
                return;
            }

            var device = ResolveDevice();
            if (_deviceId != AnyDevice && device == null)
            {
                // 対戦設定のあとでゲームパッドが抜かれたなど。このプレイヤーは入力なしで残す
                Debug.LogWarning($"[MochiHockey] {name}: input device {_deviceId} is not connected.", this);
                return;
            }

            _instance = Instantiate(actions);
            _instance.bindingMask = InputBinding.MaskByGroup(controlScheme);
            if (device != null)
            {
                _instance.devices = new[] { device };
            }

            var map = _instance.FindActionMap("Player", throwIfNotFound: true);
            _move = map.FindAction("Move", throwIfNotFound: true);
            _dash = map.FindAction("Dash", throwIfNotFound: true);
            _shoot = map.FindAction("Shoot", throwIfNotFound: true);
            _swing = map.FindAction("Swing", throwIfNotFound: true);
            _pause = map.FindAction("Pause", throwIfNotFound: true);
            _instance.Enable();
        }

        private void OnDisable()
        {
            if (_instance == null)
            {
                return;
            }

            _instance.Disable();
            Destroy(_instance);
            _instance = null;
            _controller.SetMoveInput(Vector2.zero);
        }

        /// <summary>
        /// 操作の割り当て（KeyboardP1 / KeyboardP2 / Gamepad）と、読むデバイスを決める。
        /// 有効なときに呼ばれたら、すぐに入れ替える。
        /// </summary>
        public void Configure(string scheme, int deviceId)
        {
            var wasActive = isActiveAndEnabled;
            if (wasActive)
            {
                OnDisable();
            }

            controlScheme = scheme;
            _deviceId = deviceId;

            if (wasActive)
            {
                OnEnable();
            }
        }

        /// <summary>デバイスが抜けた。入力を止める（Configure で別のデバイスを渡されるまで動かない）。</summary>
        public void DropDevice()
        {
            OnDisable();
        }

        private InputDevice ResolveDevice()
        {
            if (_deviceId != AnyDevice)
            {
                return InputSystem.GetDeviceById(_deviceId);
            }

            return controlScheme != GamepadScheme ? Keyboard.current : null;
        }

        private void Update()
        {
            if (_instance == null)
            {
                return;
            }

            if (_pause.WasPressedThisFrame() && _match != null)
            {
                _match.TogglePause();
            }

            if (_match != null && _match.IsPaused)
            {
                // ポーズ中はほかの操作を受け付けない（ポーズ前に押していた移動も止める）
                _controller.SetMoveInput(Vector2.zero);
                return;
            }

            _controller.SetMoveInput(Vector2.ClampMagnitude(_move.ReadValue<Vector2>(), 1f));
            // 壁の近くでは長押しで壁キックになるので、押した・離したを両方伝える（REQ-028）
            if (_dash.WasPressedThisFrame())
            {
                _controller.PressDash();
            }

            if (_dash.WasReleasedThisFrame())
            {
                _controller.ReleaseDash();
            }

            if (_shoot.WasPressedThisFrame())
            {
                _controller.BeginShoot();
            }

            if (_shoot.WasReleasedThisFrame())
            {
                _controller.ReleaseShoot();
            }

            if (_swing.WasPressedThisFrame())
            {
                _controller.RequestSwing();
            }
        }
    }
}
