using System.Collections.Generic;
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
    /// - Lv.3 で打つと、PierceMinSpeed 以上で飛んでいる間は敵をすり抜けながら、触れた敵を吹き飛ばす（REQ-015）
    /// - ゴール後は Hide で消え、ResetTo で Lv.1 として出し直される（REQ-004）
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class MochiBall : MonoBehaviour
    {
        [SerializeField] private GameBalanceConfig config;

        private Rigidbody _body;
        private Collider _collider;
        private GameBalanceConfig.MochiBallConfig _ballConfig;
        private MochiSettings _mochiSettings;
        private MochiGrowth _growth;
        private Mochi _mochi;
        private Vector3 _baseScale;
        private float _heldHeight;

        private PlayerController _piercingShooter;
        private readonly List<Collider> _piercedColliders = new List<Collider>();

        // 巻き込み判定で見つかったコライダーの入れ物（毎回配列を作らないように使い回す。TASK-0031）
        private readonly Collider[] _overlaps = new Collider[32];

        public GameBalanceConfig Config
        {
            get => config;
            set => config = value;
        }

        /// <summary>持っているプレイヤー。誰も持っていなければ null。</summary>
        public PlayerController Holder { get; private set; }

        public bool IsHeld => Holder != null;

        /// <summary>餅に何か起きた（効果音用）。</summary>
        public static event System.Action<MochiBall, BallEvent> AnyBallEvent;

        /// <summary>最後に打ったときのチャージ段階（効果音用）。</summary>
        public int LastShotStage { get; private set; }

        public MochiLevel Level => _mochi?.Level ?? MochiLevel.Lv1;

        public int RiceCount => _mochi?.RiceCount ?? 0;

        /// <summary>次のレベルまでの成長ゲージ（0〜1、Lv.3 は 1）。</summary>
        public float GrowthProgress => _growth == null ? 0f : _growth.GetGrowthProgress(RiceCount);

        /// <summary>今のレベルでゴールしたときの得点（頭上の ×倍率）。</summary>
        public int Points => _growth == null ? 1 : _growth.GetPoints(Level);

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
            _mochiSettings = settings.Mochi;
            _growth = new MochiGrowth(_mochiSettings);
            CreateMochi();
        }

        /// <summary>Lv.3 のシュートが敵を巻き込んでいる間か。</summary>
        public bool IsPiercing => _piercingShooter != null;

        private void Update()
        {
            _mochi.Tick(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (IsHeld)
            {
                _body.MovePosition(DribblePosition());
                return;
            }

            if (IsPiercing)
            {
                UpdatePierce();
            }
        }

        private void OnDisable()
        {
            EndPierce();
        }

        private void OnCollisionEnter(Collision collision)
        {
            TryAttachFromContact(collision);
            CheckBounce(collision);
        }

        /// <summary>持たれていない餅が、プレイヤー以外（壁など）に勢いよく当たったら知らせる。</summary>
        private void CheckBounce(Collision collision)
        {
            if (!enabled || IsHeld || config == null || collision.collider.GetComponentInParent<PlayerController>() != null)
            {
                return;
            }

            if (collision.relativeVelocity.magnitude >= config.Audio.BallBounceMinSpeed)
            {
                AnyBallEvent?.Invoke(this, BallEvent.Bounced);
            }
        }

        private void OnCollisionStay(Collision collision) => TryAttachFromContact(collision);

        /// <summary>
        /// 吸着を試みる。ほかの人が持っているとき、手放した直後の本人（REQ-024）、
        /// すでに別の餅を持っているプレイヤー、被弾中（吹っ飛び・貼り付き）のプレイヤーは失敗する。向きの判定はしない。
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

            if (!player.CanHoldBall || player.HeldBall != null || !_mochi.TryAttach(IdOf(player)))
            {
                return false;
            }

            EndPierce();
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

            if (Level == MochiLevel.Lv3)
            {
                BeginPierce(shooter);
            }

            LastShotStage = stage;
            AnyBallEvent?.Invoke(this, BallEvent.Shot);
        }

        /// <summary>ゴールに入った餅を消す。持たれていれば手放させる。</summary>
        public void Hide()
        {
            if (IsHeld)
            {
                DetachFromHolder();
                _mochi.Drop();
            }

            EndPierce();
            _body.isKinematic = false;
            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
            gameObject.SetActive(false);
        }

        /// <summary>Lv.1（ご飯粒 0）の新しい餅として、指定の位置に止まった状態で出し直す（REQ-004, REQ-005）。</summary>
        public void ResetTo(Vector3 position)
        {
            if (IsHeld)
            {
                DetachFromHolder();
            }

            EndPierce();
            gameObject.SetActive(true);
            CreateMochi();
            _body.isKinematic = false;
            _body.position = position;
            transform.position = position;
            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
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
        public bool TryAbsorbRice()
        {
            if (!enabled)
            {
                return false;
            }

            var before = Level;
            if (!_mochi.TryAbsorbRice())
            {
                return false;
            }

            AnyBallEvent?.Invoke(this, Level != before ? BallEvent.LevelUp : BallEvent.RiceAbsorbed);
            return true;
        }

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

        private void CreateMochi()
        {
            if (_mochi != null)
            {
                _mochi.LevelChanged -= OnLevelChanged;
            }

            _mochi = new Mochi(_growth, _mochiSettings);
            _mochi.LevelChanged += OnLevelChanged;
            ApplyScale();
        }

        /// <summary>打った人の敵とは衝突しないようにして、触れた敵を吹き飛ばしながら進む。</summary>
        private void BeginPierce(PlayerController shooter)
        {
            _piercingShooter = shooter;
            _piercedColliders.Clear();
            foreach (var player in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            {
                if (player.Team == shooter.Team)
                {
                    continue;
                }

                foreach (var playerCollider in player.GetComponentsInChildren<Collider>())
                {
                    Physics.IgnoreCollision(_collider, playerCollider, true);
                    _piercedColliders.Add(playerCollider);
                }
            }
        }

        private void UpdatePierce()
        {
            var velocity = _body.linearVelocity;
            velocity.y = 0f;
            if (velocity.magnitude < _ballConfig.PierceMinSpeed)
            {
                EndPierce();
                return;
            }

            var shooter = _piercingShooter;
            var count = Physics.OverlapSphereNonAlloc(_body.position, BallRadius(), _overlaps, ~0, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                var hit = _overlaps[i];
                var target = hit.GetComponentInParent<PlayerController>();
                if (target != null && target != shooter
                    && HitRules.CanHit(shooter.Id, shooter.Team, target.Id, target.Team, target.CanBeHit))
                {
                    target.ReceiveHit(velocity);
                }
            }
        }

        private void EndPierce()
        {
            if (_piercingShooter == null)
            {
                return;
            }

            foreach (var playerCollider in _piercedColliders)
            {
                if (playerCollider != null)
                {
                    Physics.IgnoreCollision(_collider, playerCollider, false);
                }
            }

            _piercedColliders.Clear();
            _piercingShooter = null;
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
