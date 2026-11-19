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
        private float _pettanSettingsHitStop;
        private SkateMotion _motion;
        private Stamina _stamina;
        private ShotCharge _shotCharge;
        private PettanStateMachine _pettan;

        private System.Numerics.Vector2 _moveInput;
        private bool _dashRequested;

        private WallKick _wallKick;
        private RinkBounds _rink;
        private float _bodyRadius;
        private bool _dashPressed;
        private bool _dashReleased;

        private float _swingRemaining;
        private bool _swingHitSomeone;
        private float _whiffRemaining;

        private bool _hasPendingHit;
        private Vector3 _pendingHitDirection;
        private PlayerController _pendingAttacker;
        private float _hitStopRemaining;
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

        /// <summary>画面に出すプレイヤー番号（1P〜4P の 1〜4）。決まっていなければ 0。MatchPlayerSpawner が設定する。</summary>
        public int Number { get; set; }

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

        /// <summary>
        /// MatchManager が操作を止めているか（キックオフのカウントダウン中・試合終了後）。
        /// 止まっている間は入力を受け付けない（滑っている分は摩擦で止まる）。
        /// </summary>
        public bool Locked { get; set; }

        /// <summary>移動・ボタン操作を受け付けるか（止められている間、被弾中、空振り硬直中は受け付けない）。</summary>
        public bool CanControl => _pettan != null && _pettan.CanControl && _whiffRemaining <= 0f && !Locked;

        /// <summary>杵で叩かれるか（Normal のときだけ）。</summary>
        public bool CanBeHit => _pettan != null && _pettan.CanBeHit;

        /// <summary>餅を拾えるか（吹っ飛び中・貼り付き中・壁キック中は拾えない）。</summary>
        public bool CanHoldBall => _pettan != null && _pettan.CanControl && (_wallKick == null || !_wallKick.ControlsMovement);

        public bool IsSwinging => _swingRemaining > 0f;

        /// <summary>杵を振り始めた（見た目のアニメーション用）。</summary>
        public event System.Action Swung;

        /// <summary>ダッシュした（氷しぶきの演出用）。</summary>
        public event System.Action Dashed;

        /// <summary>壁キックで飛び出した（演出用。連鎖の数は WallKickChain）。</summary>
        public event System.Action WallKicked;

        /// <summary>壁キックの状態（REQ-028）。</summary>
        public WallKickPhase WallKickPhase => _wallKick?.Phase ?? WallKickPhase.None;

        /// <summary>続けて壁キックした回数（REQ-029）。</summary>
        public int WallKickChain => _wallKick?.Chain ?? 0;

        /// <summary>壁キックで狙っている向き（床面。x, z）。</summary>
        public Vector2 WallKickAim => _wallKick == null ? Vector2.zero : ToUnity(_wallKick.AimDirection);

        /// <summary>どれかのプレイヤーが杵などで叩かれて HitStop に入った（Pettan!!! の演出用）。</summary>
        public static event System.Action<PlayerController> AnyHitTaken;

        /// <summary>どれかのプレイヤーに何か起きた（効果音用）。</summary>
        public static event System.Action<PlayerController, PlayerEvent> AnyPlayerEvent;

        private static readonly List<PlayerController> s_active = new List<PlayerController>();

        /// <summary>
        /// 今いる（有効な）プレイヤー全員。カメラなどが毎フレーム見るので、シーンを探さずにここを使う（TASK-0031）。
        /// </summary>
        public static IReadOnlyList<PlayerController> Active => s_active;

        /// <summary>移動の入力（操作できないときは 0）。</summary>
        public Vector2 MoveInput => CanControl ? new Vector2(_moveInput.X, _moveInput.Y) : Vector2.zero;

        /// <summary>
        /// 杵を当てた側のヒットストップ中か（叩かれた側と同じ時間だけ止まって、手応えを出す）。
        /// 止まっている間は動き・スイング・チャージの時間が進まない。
        /// </summary>
        public bool IsInHitStop => _hitStopRemaining > 0f;

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
            _pettanSettingsHitStop = settings.Pettan.HitStopDuration;
            _motion = new SkateMotion(_settings);
            _stamina = new Stamina(_settings);
            _shotCharge = new ShotCharge(_settings);
            _pettan = new PettanStateMachine(settings.Pettan, settings.Rink);
            _rink = settings.Rink;
            _wallKick = new WallKick(settings.WallKick, settings.Rink);
            var capsule = GetComponent<CapsuleCollider>();
            _bodyRadius = capsule != null
                ? capsule.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z)
                : 0.5f;
            _pettan.StateChanged += OnPettanStateChanged;
            _pettan.Bounced += () => AnyPlayerEvent?.Invoke(this, PlayerEvent.LaunchBounced);
            _pettan.Recovered += () => AnyPlayerEvent?.Invoke(this, PlayerEvent.Recovered);
        }

        private void OnEnable()
        {
            if (!s_active.Contains(this))
            {
                s_active.Add(this);
            }
        }

        private void OnDisable()
        {
            s_active.Remove(this);
        }

        /// <summary>スティック・移動キーの入力（長さ 1 まで）。</summary>
        public void SetMoveInput(Vector2 input)
        {
            _moveInput = new System.Numerics.Vector2(input.x, input.y);
        }

        /// <summary>ダッシュボタンが押された。次の Update でスタミナが足りればダッシュする（壁キックは見ない）。</summary>
        public void RequestDash()
        {
            _dashRequested = true;
        }

        /// <summary>
        /// ダッシュボタンを押した（PlayerInputReader から）。壁の近くなら長押しで壁キック（REQ-028）、
        /// それ以外はすぐダッシュする。
        /// </summary>
        public void PressDash()
        {
            _dashPressed = true;
        }

        /// <summary>ダッシュボタンを離した。壁キックの狙いなら飛ぶ、短く押しただけならダッシュする。</summary>
        public void ReleaseDash()
        {
            _dashReleased = true;
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
            Swung?.Invoke();
            AnyPlayerEvent?.Invoke(this, PlayerEvent.Swung);
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

            ReceiveHit(attacker.transform.forward);
            _pendingAttacker = attacker;
        }

        /// <summary>吹き飛ばす方向を指定して被弾させる（Lv.3 シュートの巻き込みなど）。反映は LateUpdate。</summary>
        public void ReceiveHit(Vector3 launchDirection)
        {
            if (!CanBeHit || _hasPendingHit)
            {
                return;
            }

            launchDirection.y = 0f;
            _pendingHitDirection = launchDirection.sqrMagnitude > 0f ? launchDirection.normalized : -transform.forward;
            _hasPendingHit = true;
        }

        /// <summary>
        /// キックオフのために、被弾・スイング・チャージ・スタミナを最初の状態に戻し、指定の位置と向きに置く（REQ-005）。
        /// 持っている餅は MatchManager が別に戻す。
        /// </summary>
        public void ResetForKickoff(Vector3 position, Quaternion rotation)
        {
            _hasPendingHit = false;
            _pettan.Reset();
            _motion.Stop();
            _shotCharge.Cancel();
            _stamina = new Stamina(_settings);
            _swingRemaining = 0f;
            _whiffRemaining = 0f;
            _hitStopRemaining = 0f;
            _pendingAttacker = null;
            _dashRequested = false;
            _dashPressed = false;
            _dashReleased = false;
            _wallKick.Cancel();
            _moveInput = System.Numerics.Vector2.Zero;

            _body.isKinematic = false;
            _body.position = position;
            _body.rotation = rotation;
            transform.SetPositionAndRotation(position, rotation);
            _body.linearVelocity = Vector3.zero;
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;

            _pettan.Tick(deltaTime);
            if (IsInHitStop && !IsPettanMoving())
            {
                _hitStopRemaining -= deltaTime;
                return;
            }

            _shotCharge.Tick(deltaTime);
            _stamina.Tick(deltaTime);
            UpdateSwing(deltaTime);

            if (IsPettanMoving())
            {
                _dashRequested = false;
                _dashPressed = false;
                _dashReleased = false;
                ApplyPettanScale();
                return;
            }

            UpdateWallKick(deltaTime);
            if (_wallKick.ControlsMovement)
            {
                _dashRequested = false;
                UpdateBlink();
                return;
            }

            var input = CanControl ? _moveInput : System.Numerics.Vector2.Zero;
            if (_dashRequested)
            {
                _dashRequested = false;
                if (CanControl && _stamina.TryConsumeDash())
                {
                    _motion.AddImpulse(DashDirection() * _settings.DashImpulse);
                    Dashed?.Invoke();
                    AnyPlayerEvent?.Invoke(this, PlayerEvent.Dashed);
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

            if (IsInHitStop)
            {
                _body.linearVelocity = new Vector3(0f, _body.linearVelocity.y, 0f);
                return;
            }

            if (_wallKick.ControlsMovement)
            {
                // 狙っている・張り付いている間は止まり、飛んでいる間は壁キックの速度でまっすぐ進む
                var kick = _wallKick.Velocity;
                _body.linearVelocity = new Vector3(kick.X, _body.linearVelocity.y, kick.Y);
                var facing = _wallKick.Phase == WallKickPhase.Flying ? kick : _wallKick.AimDirection;
                if (facing.LengthSquared() > 0f)
                {
                    _body.rotation = Quaternion.LookRotation(new Vector3(facing.X, 0f, facing.Y), Vector3.up);
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
            var attacker = _pendingAttacker;
            _pendingAttacker = null;
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
            _wallKick.Cancel();
            _swingRemaining = 0f;
            _whiffRemaining = 0f;
            _hitStopRemaining = 0f;

            _pettanBaseHeight = _body.position.y;
            _body.linearVelocity = Vector3.zero;
            _body.isKinematic = true;

            var position = _body.position;
            _pettan.Begin(
                new System.Numerics.Vector2(position.x, position.z),
                new System.Numerics.Vector2(_pendingHitDirection.x, _pendingHitDirection.z));

            if (attacker != null && attacker != this)
            {
                attacker.BeginHitStop(_pettanSettingsHitStop);
            }

            AnyHitTaken?.Invoke(this);
            AnyPlayerEvent?.Invoke(this, PlayerEvent.Hit);
        }

        /// <summary>杵を当てた側を、叩かれた側と同じ時間だけ止める。</summary>
        private void BeginHitStop(float duration)
        {
            if (PettanState == PettanState.Normal || PettanState == PettanState.Invincible)
            {
                _hitStopRemaining = Mathf.Max(_hitStopRemaining, duration);
            }
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
                    AnyPlayerEvent?.Invoke(this, PlayerEvent.Stuck);
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
                    // ふつうは Invincible から戻るが、Reset でどの状態からでも来るので全部元に戻す
                    SetRenderersVisible(true);
                    SetCollidersEnabled(true);
                    SetCollisionWithOtherPlayers(true);
                    _body.isKinematic = false;
                    transform.localScale = _baseScale;
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

        /// <summary>壁キック（REQ-028, REQ-029）の入力と時間を進める。</summary>
        private void UpdateWallKick(float deltaTime)
        {
            var position = new System.Numerics.Vector2(_body.position.x, _body.position.z);
            var before = _wallKick.Phase;

            if (_dashPressed)
            {
                _dashPressed = false;
                var canKick = CanControl && HeldBall == null;
                if (!_wallKick.PressDash(position, canKick))
                {
                    _dashRequested = true;
                }
            }

            // 長押しを待っている間に餅を拾ったら、壁キックはやめてふつうのダッシュにする
            if (_wallKick.Phase == WallKickPhase.Holding && HeldBall != null)
            {
                _wallKick.Cancel();
                _dashRequested = true;
            }

            if (_wallKick.Phase == WallKickPhase.Aiming && CanControl)
            {
                _wallKick.SetAim(_moveInput);
            }

            var result = WallKickResult.None;
            if (_dashReleased)
            {
                _dashReleased = false;
                result = _wallKick.ReleaseDash();
            }

            if (result == WallKickResult.None)
            {
                result = _wallKick.Tick(deltaTime, position);
            }

            switch (result)
            {
                case WallKickResult.Dash:
                    _dashRequested = true;
                    break;
                case WallKickResult.Launched:
                    _motion.SetVelocity(_wallKick.Velocity);
                    WallKicked?.Invoke();
                    AnyPlayerEvent?.Invoke(this, PlayerEvent.WallKicked);
                    break;
            }

            var now = _wallKick.Phase;
            if (now != before && _wallKick.IsHoldingPosition)
            {
                _motion.Stop();
                SnapToWall(_wallKick.WallNormal);
            }
            else if (before == WallKickPhase.Clinging && now == WallKickPhase.None)
            {
                _motion.Stop();
            }
        }

        /// <summary>壁に体をぴったり付ける（狙い始めたとき・飛んだ先の壁に着いたとき）。</summary>
        private void SnapToWall(System.Numerics.Vector2 normal)
        {
            var position = _body.position;
            if (normal.X > 0.5f)
            {
                position.x = _rink.Min.X + _bodyRadius;
            }
            else if (normal.X < -0.5f)
            {
                position.x = _rink.Max.X - _bodyRadius;
            }
            else if (normal.Y > 0.5f)
            {
                position.z = _rink.Min.Y + _bodyRadius;
            }
            else
            {
                position.z = _rink.Max.Y - _bodyRadius;
            }

            _body.position = position;
            transform.position = position;
            _body.linearVelocity = new Vector3(0f, _body.linearVelocity.y, 0f);
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
