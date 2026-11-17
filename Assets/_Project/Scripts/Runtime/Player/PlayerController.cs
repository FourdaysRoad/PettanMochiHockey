using System.Collections.Generic;
using MochiHockey.Core;
using UnityEngine;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// プレイヤー 1 人分の操作と被弾（REQ-021, REQ-023, REQ-025, REQ-026, REQ-031〜036）。
    /// - Update で Core の SkateMotion / Stamina / ShotCharge / PettanStateMachine を進め、杵の当たりを集める
    /// - LateUpdate で、このフレームに受けた杵を反映する（同時に当て合ったら両方被弾するよう、判定の後でまとめて反映）
    /// - FixedUpdate で、通常時は Rigidbody に速度と向きを、被弾中は kinematic で Core の位置・高さ・向きを反映する
    /// 入力は PlayerInputReader（またはテスト）が SetMoveInput / RequestDash / BeginShoot / ReleaseShoot / RequestSwing で渡す。
    /// Core の XZ 平面は Vector2(x, y) = ワールドの (x, z)。
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerController : MonoBehaviour
    {
        private static int s_nextId;

        [SerializeField] private GameBalanceConfig config;
        [SerializeField] private Team team;

        private Rigidbody _body;
        private MalletHitbox _mallet;
        private Collider[] _colliders;
        private Renderer[] _renderers;
        private Vector3 _baseScale;

        private PlayerSettings _settings;
        private float _blinkInterval;
        private SkateMotion _motion;
        private Stamina _stamina;
        private ShotCharge _shotCharge;
        private PettanStateMachine _pettan;

        private System.Numerics.Vector2 _moveInput;
        private bool _dashRequested;

        private float _swingRemaining;
        private bool _swingHitSomeone;
        private float _whiffRemaining;

        private bool _hasPendingHit;
        private Vector3 _pendingHitDirection;
        private float _pettanBaseHeight;

        public GameBalanceConfig Config
        {
            get => config;
            set => config = value;
        }

        public Team Team
        {
            get => team;
            set => team = value;
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

        public PettanState PettanState => _pettan?.State ?? PettanState.Normal;

        /// <summary>移動・ボタン操作を受け付けるか（被弾中と空振り硬直中は受け付けない）。</summary>
        public bool CanControl => _pettan != null && _pettan.CanControl && _whiffRemaining <= 0f;

        /// <summary>杵で叩かれるか（Normal のときだけ）。</summary>
        public bool CanBeHit => _pettan != null && _pettan.CanBeHit;

        /// <summary>餅を拾えるか（吹っ飛び中・貼り付き中は拾えない）。</summary>
        public bool CanHoldBall => _pettan != null && _pettan.CanControl;

        public bool IsSwinging => _swingRemaining > 0f;

        private void Awake()
        {
            Id = ++s_nextId;
            _body = GetComponent<Rigidbody>();
            _body.constraints = RigidbodyConstraints.FreezeRotation;
            _mallet = GetComponent<MalletHitbox>();
            _colliders = GetComponentsInChildren<Collider>();
            _renderers = GetComponentsInChildren<Renderer>();
            _baseScale = transform.localScale;

            if (config == null)
            {
                Debug.LogError($"[MochiHockey] {name}: GameBalanceConfig is not assigned. PlayerController is disabled.", this);
                enabled = false;
                return;
            }

            var settings = config.ToSettings();
            _settings = settings.Player;
            _blinkInterval = config.Pettan.BlinkInterval;
            _motion = new SkateMotion(_settings);
            _stamina = new Stamina(_settings);
            _shotCharge = new ShotCharge(_settings);
            _pettan = new PettanStateMachine(settings.Pettan, settings.Rink);
            _pettan.StateChanged += OnPettanStateChanged;
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
            if (CanControl)
            {
                _shotCharge.Begin();
            }
        }

        /// <summary>パス／シュートボタンを離した。餅を持っていれば、チャージ段階に応じて打ち出す。</summary>
        public void ReleaseShoot()
        {
            var stage = _shotCharge.Release();
            if (HeldBall != null && CanControl)
            {
                HeldBall.Shoot(this, stage);
            }
        }

        /// <summary>杵スイングボタンを押した（REQ-025）。操作できないとき・スイング中は何もしない。</summary>
        public void RequestSwing()
        {
            if (!CanControl || IsSwinging)
            {
                return;
            }

            _swingRemaining = _settings.SwingActiveTime;
            _swingHitSomeone = false;
            _mallet?.BeginSwing();
        }

        /// <summary>
        /// 杵が当たった。すぐには反映せず、このフレームの LateUpdate で被弾させる（REQ-036）。
        /// 叩かれない状態なら何もしない。
        /// </summary>
        public void ReceiveHit(PlayerController attacker)
        {
            if (!CanBeHit || _hasPendingHit)
            {
                return;
            }

            var direction = attacker.transform.forward;
            direction.y = 0f;
            _pendingHitDirection = direction.sqrMagnitude > 0f ? direction.normalized : -transform.forward;
            _hasPendingHit = true;
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;

            _pettan.Tick(deltaTime);
            _shotCharge.Tick(deltaTime);
            _stamina.Tick(deltaTime);
            UpdateSwing(deltaTime);

            if (IsPettanMoving())
            {
                _dashRequested = false;
                ApplyPettanScale();
                return;
            }

            var input = CanControl ? _moveInput : System.Numerics.Vector2.Zero;
            if (_dashRequested)
            {
                _dashRequested = false;
                if (CanControl && _stamina.TryConsumeDash())
                {
                    _motion.AddImpulse(DashDirection() * _settings.DashImpulse);
                }
            }

            _motion.Tick(deltaTime, input, SpeedMultiplier);
            UpdateBlink();
        }

        private void LateUpdate()
        {
            if (_hasPendingHit)
            {
                ApplyHit();
            }
        }

        private void FixedUpdate()
        {
            if (IsPettanMoving())
            {
                var position = _pettan.Position;
                _body.MovePosition(new Vector3(position.X, _pettanBaseHeight + _pettan.Height, position.Y));
                if (_pettan.State == PettanState.Stuck)
                {
                    var normal = _pettan.WallNormal;
                    _body.MoveRotation(Quaternion.LookRotation(new Vector3(normal.X, 0f, normal.Y), Vector3.up));
                }

                return;
            }

            var velocity = _motion.Velocity;
            _body.linearVelocity = new Vector3(velocity.X, _body.linearVelocity.y, velocity.Y);

            if (CanControl && _moveInput.LengthSquared() > 0f)
            {
                _body.rotation = Quaternion.LookRotation(new Vector3(_moveInput.X, 0f, _moveInput.Y), Vector3.up);
            }
        }

        private void UpdateSwing(float deltaTime)
        {
            if (IsSwinging)
            {
                if (_mallet != null)
                {
                    foreach (var target in _mallet.CollectHits())
                    {
                        target.ReceiveHit(this);
                        _swingHitSomeone = true;
                    }
                }

                _swingRemaining -= deltaTime;
                if (_swingRemaining <= 0f)
                {
                    _swingRemaining = 0f;
                    if (!_swingHitSomeone)
                    {
                        _whiffRemaining = _settings.WhiffRecoveryTime;
                    }
                }
            }
            else if (_whiffRemaining > 0f)
            {
                _whiffRemaining -= deltaTime;
            }
        }

        private void ApplyHit()
        {
            _hasPendingHit = false;
            if (!CanBeHit)
            {
                return;
            }

            if (HeldBall != null)
            {
                HeldBall.Drop();
            }

            _shotCharge.Cancel();
            _motion.Stop();
            _swingRemaining = 0f;
            _whiffRemaining = 0f;

            _pettanBaseHeight = _body.position.y;
            _body.linearVelocity = Vector3.zero;
            _body.isKinematic = true;

            var position = _body.position;
            _pettan.Begin(
                new System.Numerics.Vector2(position.x, position.z),
                new System.Numerics.Vector2(_pendingHitDirection.x, _pendingHitDirection.z));
        }

        private void OnPettanStateChanged(PettanState state)
        {
            switch (state)
            {
                case PettanState.Launched:
                    SetCollisionWithOtherPlayers(false);
                    break;
                case PettanState.Stuck:
                    SetCollidersEnabled(false);
                    break;
                case PettanState.Invincible:
                    SetCollidersEnabled(true);
                    SetCollisionWithOtherPlayers(true);
                    var position = _pettan.Position;
                    _body.position = new Vector3(position.X, _pettanBaseHeight, position.Y);
                    transform.position = _body.position;
                    _body.isKinematic = false;
                    transform.localScale = _baseScale;
                    break;
                case PettanState.Normal:
                    SetRenderersVisible(true);
                    break;
            }
        }

        /// <summary>HitStop・Launched・Stuck の間は Core の位置で動かす。</summary>
        private bool IsPettanMoving()
        {
            var state = PettanState;
            return state == PettanState.HitStop || state == PettanState.Launched || state == PettanState.Stuck;
        }

        private void ApplyPettanScale()
        {
            var scale = _pettan.Scale;
            transform.localScale = Vector3.Scale(_baseScale, new Vector3(scale.X, scale.Y, scale.Z));
        }

        private void UpdateBlink()
        {
            if (PettanState != PettanState.Invincible)
            {
                return;
            }

            var phase = Mathf.FloorToInt(_pettan.Elapsed / _blinkInterval);
            SetRenderersVisible(phase % 2 == 0);
        }

        private void SetRenderersVisible(bool visible)
        {
            foreach (var r in _renderers)
            {
                r.enabled = visible;
            }
        }

        private void SetCollidersEnabled(bool enable)
        {
            foreach (var c in _colliders)
            {
                c.enabled = enable;
            }
        }

        /// <summary>吹っ飛び中は他のプレイヤーをすり抜ける（REQ-035）。</summary>
        private void SetCollisionWithOtherPlayers(bool collide)
        {
            var others = new List<Collider>();
            foreach (var other in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            {
                if (other != this)
                {
                    others.AddRange(other.GetComponentsInChildren<Collider>());
                }
            }

            foreach (var mine in _colliders)
            {
                foreach (var theirs in others)
                {
                    Physics.IgnoreCollision(mine, theirs, !collide);
                }
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
