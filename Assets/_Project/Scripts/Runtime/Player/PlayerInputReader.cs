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

        [SerializeField] private InputActionAsset actions;

        [Tooltip("KeyboardP1 / KeyboardP2 / Gamepad")]
        [SerializeField] private string controlScheme = KeyboardP1Scheme;

        private PlayerController _controller;
        private InputActionAsset _instance;
        private InputAction _move;
        private InputAction _dash;
        private InputAction _shoot;
        private InputAction _swing;

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
        }

        private void OnEnable()
        {
            if (actions == null)
            {
                Debug.LogError($"[MochiHockey] {name}: InputActionAsset is not assigned.", this);
                enabled = false;
                return;
            }

            _instance = Instantiate(actions);
            _instance.bindingMask = InputBinding.MaskByGroup(controlScheme);
            if (controlScheme != GamepadScheme && Keyboard.current != null)
            {
                _instance.devices = new InputDevice[] { Keyboard.current };
            }

            var map = _instance.FindActionMap("Player", throwIfNotFound: true);
            _move = map.FindAction("Move", throwIfNotFound: true);
            _dash = map.FindAction("Dash", throwIfNotFound: true);
            _shoot = map.FindAction("Shoot", throwIfNotFound: true);
            _swing = map.FindAction("Swing", throwIfNotFound: true);
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

        private void Update()
        {
            _controller.SetMoveInput(Vector2.ClampMagnitude(_move.ReadValue<Vector2>(), 1f));
            if (_dash.WasPressedThisFrame())
            {
                _controller.RequestDash();
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
