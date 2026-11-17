using MochiHockey.Core;
using UnityEngine;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// プレイヤー 1 人分の操作（REQ-021, REQ-023, REQ-026）。
    /// Update で Core の SkateMotion / Stamina / ShotCharge を進め、FixedUpdate で Rigidbody に速度と向きを反映する。
    /// 入力は PlayerInputReader（またはテスト）が SetMoveInput / RequestDash / BeginShoot / ReleaseShoot で渡す。
    /// Core の XZ 平面は Vector2(x, y) = ワールドの (x, z)。
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerController : MonoBehaviour
    {
        private static int s_nextId;

        [SerializeField] private GameBalanceConfig config;

        private Rigidbody _body;
        private PlayerSettings _settings;
        private SkateMotion _motion;
        private Stamina _stamina;
        private ShotCharge _shotCharge;

        private System.Numerics.Vector2 _moveInput;
        private bool _dashRequested;

        public GameBalanceConfig Config
        {
            get => config;
            set => config = value;
        }

        /// <summary>Core に渡すプレイヤーの識別番号（このプレイ中に作られた順の通し番号）。</summary>
        public int Id { get; private set; }

        /// <summary>床面の速度（x = ワールド x、y = ワールド z）。</summary>
        public Vector2 PlanarVelocity => _motion == null ? Vector2.zero : ToUnity(_motion.Velocity);

        /// <summary>スタミナゲージ用（0〜1）。</summary>
        public float StaminaNormalized => _stamina?.Normalized ?? 0f;

        /// <summary>チャージゲージ用（0〜1）。</summary>
        public float ShotChargeNormalized => _shotCharge?.Normalized ?? 0f;

        /// <summary>持っている餅による速度倍率（餅を持っていなければ 1）。MochiBall が設定する。</summary>
        public float SpeedMultiplier { get; set; } = 1f;

        /// <summary>持っている餅。MochiBall が吸着・手放しのときに設定する。</summary>
        public MochiBall HeldBall { get; internal set; }

        private void Awake()
        {
            Id = ++s_nextId;
            _body = GetComponent<Rigidbody>();
            _body.constraints = RigidbodyConstraints.FreezeRotation;

            if (config == null)
            {
                Debug.LogError($"[MochiHockey] {name}: GameBalanceConfig is not assigned. PlayerController is disabled.", this);
                enabled = false;
                return;
            }

            _settings = config.ToSettings().Player;
            _motion = new SkateMotion(_settings);
            _stamina = new Stamina(_settings);
            _shotCharge = new ShotCharge(_settings);
        }

        /// <summary>スティック・移動キーの入力（長さ 1 まで）。</summary>
        public void SetMoveInput(Vector2 input)
        {
            _moveInput = new System.Numerics.Vector2(input.x, input.y);
        }

        /// <summary>ダッシュボタンが押された。次の Update でスタミナが足りればダッシュする。</summary>
        public void RequestDash()
        {
            _dashRequested = true;
        }

        /// <summary>パス／シュートボタンを押した。長押しでチャージする。</summary>
        public void BeginShoot()
        {
            _shotCharge.Begin();
        }

        /// <summary>パス／シュートボタンを離した。餅を持っていれば、チャージ段階に応じて打ち出す。</summary>
        public void ReleaseShoot()
        {
            var stage = _shotCharge.Release();
            if (HeldBall != null)
            {
                HeldBall.Shoot(this, stage);
            }
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;

            _shotCharge.Tick(deltaTime);

            _stamina.Tick(deltaTime);
            if (_dashRequested)
            {
                _dashRequested = false;
                if (_stamina.TryConsumeDash())
                {
                    _motion.AddImpulse(DashDirection() * _settings.DashImpulse);
                }
            }

            _motion.Tick(deltaTime, _moveInput, SpeedMultiplier);
        }

        private void FixedUpdate()
        {
            var velocity = _motion.Velocity;
            _body.linearVelocity = new Vector3(velocity.X, _body.linearVelocity.y, velocity.Y);

            if (_moveInput.LengthSquared() > 0f)
            {
                _body.rotation = Quaternion.LookRotation(new Vector3(_moveInput.X, 0f, _moveInput.Y), Vector3.up);
            }
        }

        /// <summary>入力の向き → いまの進行方向 → 体の正面、の順に決める。</summary>
        private System.Numerics.Vector2 DashDirection()
        {
            if (_moveInput.LengthSquared() > 0f)
            {
                return System.Numerics.Vector2.Normalize(_moveInput);
            }

            if (_motion.Velocity.LengthSquared() > 0f)
            {
                return System.Numerics.Vector2.Normalize(_motion.Velocity);
            }

            var forward = transform.forward;
            return System.Numerics.Vector2.Normalize(new System.Numerics.Vector2(forward.x, forward.z));
        }

        private static Vector2 ToUnity(System.Numerics.Vector2 v) => new Vector2(v.X, v.Y);
    }
}
