using MochiHockey.Core;
using UnityEngine;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// 餅（球）。Core の Mochi の状態を、見た目・コライダー・物理に反映する（REQ-012〜015, 022〜024, 032）。
    /// - 誰も持っていないとき：通常の Rigidbody として転がる
    /// - 正面から触れたプレイヤーに吸着し、持たれている間は kinematic で杵の先（体の正面）に追従する
    /// - Shoot でチャージ段階とレベルに応じた速さで打ち出し、Drop で軽く跳ねて落ちる
    /// - レベルが上がると大きさ（とコライダー）が変わり、持ち手の速度倍率も変わる
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class MochiBall : MonoBehaviour
    {
        [SerializeField] private GameBalanceConfig config;

        private Rigidbody _body;
        private Collider _collider;
        private GameBalanceConfig.MochiBallConfig _ballConfig;
        private MochiGrowth _growth;
        private Mochi _mochi;
        private Vector3 _baseScale;
        private float _heldHeight;

        public GameBalanceConfig Config
        {
            get => config;
            set => config = value;
        }

        /// <summary>持っているプレイヤー。誰も持っていなければ null。</summary>
        public PlayerController Holder { get; private set; }

        public bool IsHeld => Holder != null;

        public MochiLevel Level => _mochi?.Level ?? MochiLevel.Lv1;

        public int RiceCount => _mochi?.RiceCount ?? 0;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();
            _baseScale = transform.localScale;

            if (config == null)
            {
                Fail("GameBalanceConfig is not assigned");
                return;
            }

            var settings = config.ToSettings();
            _ballConfig = config.MochiBall;
            if (_ballConfig.ShotSpeedByStage == null
                || _ballConfig.ShotSpeedByStage.Length != settings.Player.ChargeStages + 1)
            {
                Fail($"GameBalanceConfig.MochiBall.ShotSpeedByStage must have {settings.Player.ChargeStages + 1} elements");
                return;
            }

            if (_ballConfig.ShotSpeedMultiplierByLevel == null
                || _ballConfig.ShotSpeedMultiplierByLevel.Length != BalanceSettings.MochiLevelCount)
            {
                Fail($"GameBalanceConfig.MochiBall.ShotSpeedMultiplierByLevel must have {BalanceSettings.MochiLevelCount} elements");
                return;
            }

            _body.linearDamping = _ballConfig.LinearDamping;
            _growth = new MochiGrowth(settings.Mochi);
            _mochi = new Mochi(_growth, settings.Mochi);
            _mochi.LevelChanged += OnLevelChanged;
            ApplyScale();
        }

        private void Update()
        {
            _mochi.Tick(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (IsHeld)
            {
                _body.MovePosition(DribblePosition());
            }
        }

        private void OnCollisionEnter(Collision collision) => TryAttachFromContact(collision);

        private void OnCollisionStay(Collision collision) => TryAttachFromContact(collision);

        /// <summary>
        /// 吸着を試みる。ほかの人が持っているとき、手放した直後の本人（REQ-024）、
        /// すでに別の餅を持っているプレイヤーは失敗する。向きの判定はしない。
        /// </summary>
        public bool TryAttach(PlayerController player)
        {
            if (!enabled || player == null)
            {
                return false;
            }

            if (IsHeld)
            {
                return Holder == player;
            }

            if (player.HeldBall != null || !_mochi.TryAttach(IdOf(player)))
            {
                return false;
            }

            Holder = player;
            player.HeldBall = this;
            player.SpeedMultiplier = _growth.GetSpeedMultiplier(Level);

            _heldHeight = _body.position.y;
            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
            _body.isKinematic = true;
            SetCollisionWithHolder(false);
            return true;
        }

        /// <summary>持ち手が前方へ打ち出す（REQ-023）。持ち手以外が呼んだら何もしない。</summary>
        /// <param name="stage">ShotCharge.Release() の段階（0 〜 ChargeStages）。</param>
        public void Shoot(PlayerController shooter, int stage)
        {
            if (!IsHeld || Holder != shooter)
            {
                return;
            }

            var direction = Flatten(shooter.transform.forward);
            var stages = _ballConfig.ShotSpeedByStage;
            var speed = stages[Mathf.Clamp(stage, 0, stages.Length - 1)]
                        * _ballConfig.ShotSpeedMultiplierByLevel[(int)Level - 1];

            DetachFromHolder();
            _mochi.Release();
            _body.isKinematic = false;
            _body.linearVelocity = direction * speed;
        }

        /// <summary>被弾で落とす（REQ-032）。大きさは保ったまま軽く跳ねる。再吸着禁止は付かない。</summary>
        public void Drop()
        {
            if (!IsHeld)
            {
                return;
            }

            DetachFromHolder();
            _mochi.Drop();
            _body.isKinematic = false;
            _body.linearVelocity = Vector3.up * _ballConfig.DropPopSpeed;
        }

        /// <summary>ご飯粒を吸収する。持たれているときだけ成功する（REQ-012）。</summary>
        public bool TryAbsorbRice() => enabled && _mochi.TryAbsorbRice();

        private void TryAttachFromContact(Collision collision)
        {
            if (!enabled || IsHeld)
            {
                return;
            }

            var player = collision.collider.GetComponentInParent<PlayerController>();
            if (player != null && player.enabled && IsInFrontOf(player))
            {
                TryAttach(player);
            }
        }

        private bool IsInFrontOf(PlayerController player)
        {
            var toBall = Flatten(transform.position - player.transform.position);
            var forward = Flatten(player.transform.forward);
            return Vector3.Angle(forward, toBall) <= _ballConfig.AttachAngle / 2f;
        }

        private void DetachFromHolder()
        {
            SetCollisionWithHolder(true);
            Holder.HeldBall = null;
            Holder.SpeedMultiplier = 1f;
            Holder = null;
        }

        private void SetCollisionWithHolder(bool enable)
        {
            foreach (var holderCollider in Holder.GetComponentsInChildren<Collider>())
            {
                Physics.IgnoreCollision(_collider, holderCollider, !enable);
            }
        }

        private Vector3 DribblePosition()
        {
            var holderPosition = Holder.transform.position;
            var forward = Flatten(Holder.transform.forward);
            var distance = HolderRadius() + BallRadius() + _ballConfig.DribbleGap;
            var position = holderPosition + forward * distance;
            position.y = _heldHeight;
            return position;
        }

        private float HolderRadius()
        {
            var capsule = Holder.GetComponent<CapsuleCollider>();
            if (capsule == null)
            {
                return 0f;
            }

            var scale = Holder.transform.lossyScale;
            return capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        }

        private float BallRadius()
        {
            var sphere = _collider as SphereCollider;
            if (sphere == null)
            {
                return _collider.bounds.extents.x;
            }

            return sphere.radius * Mathf.Abs(transform.lossyScale.x);
        }

        private void OnLevelChanged(MochiLevel level)
        {
            ApplyScale();
            if (IsHeld)
            {
                Holder.SpeedMultiplier = _growth.GetSpeedMultiplier(level);

                // 大きくなった分だけすぐ前へずらす（次の FixedUpdate まで待つと持ち手の体にめり込む）
                var position = DribblePosition();
                _body.position = position;
                transform.position = position;
            }
        }

        private void ApplyScale()
        {
            transform.localScale = _baseScale * _growth.GetScale(Level);
        }

        private void Fail(string reason)
        {
            Debug.LogError($"[MochiHockey] {name}: {reason}. MochiBall is disabled.", this);
            enabled = false;
        }

        private static int IdOf(PlayerController player) => player.Id;

        private static Vector3 Flatten(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 0f ? v.normalized : Vector3.forward;
        }
    }
}
