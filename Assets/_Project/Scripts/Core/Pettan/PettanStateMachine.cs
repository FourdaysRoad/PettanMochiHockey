using System;
using System.Numerics;

namespace MochiHockey.Core
{
    /// <summary>
    /// 被弾後の状態と軌道（REQ-031〜035）。Runtime はこの値を kinematic で反映する。
    /// 時刻はすべて Begin からの経過秒で、PettanSettings の値を区切りに使う：
    /// HitStop → Launched（HitStopDuration）→ 壁到達で Stuck → Recover で Invincible（RecoverTime）→ Normal。
    /// 吹っ飛び：床面は攻撃者の正面方向へ等速で直進する。高さは標準の吹っ飛び時間
    /// （HitStopDuration〜LaunchNominalDuration）を前半・後半に分けた 2 つの放物線で、真ん中で床に 1 回バウンドする。
    /// その後は床を滑って壁へ向かい、LaunchMaxDuration までに届かなければその場で復帰する。
    /// 張り付き：届いた瞬間の高さのまま止まり、SlideDownStartTime から SlideDownSpeed で下がる。
    /// </summary>
    public sealed class PettanStateMachine
    {
        // 1/60 秒を積み重ねた誤差で区切りの時刻を越えられないことがないようにする許容差
        private const float TimeTolerance = 1e-5f;

        private readonly PettanSettings _settings;
        private readonly RinkBounds _rink;

        private Vector2 _launchOrigin;
        private Vector2 _launchDirection;
        private bool _willHitWall;
        private Vector2 _wallPoint;
        private Vector2 _wallNormal;
        private float _wallArrivalTime;
        private float _recoverTime;
        private float _stuckHeight;

        public PettanStateMachine(PettanSettings settings, RinkBounds rink)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (!rink.IsValid)
            {
                throw new ArgumentException("Rink must have a positive size.", nameof(rink));
            }

            _rink = rink;
        }

        public PettanState State { get; private set; } = PettanState.Normal;

        /// <summary>Begin からの経過秒。Normal のときは 0。</summary>
        public float Elapsed { get; private set; }

        /// <summary>床面 XZ の位置。</summary>
        public Vector2 Position { get; private set; }

        /// <summary>
        /// 通常の立ち位置からの高さ（Y）。吹っ飛び中は 0 以上。
        /// 張り付き中のずり落ちでは 0 より下がることがある（Runtime が壁の貼り付け位置に足して使う）。
        /// </summary>
        public float Height { get; private set; }

        /// <summary>貼り付いている壁の内向きの法線。Stuck 中のみ意味がある。</summary>
        public Vector2 WallNormal { get; private set; }

        /// <summary>張り付き中のずり落ち（SlideDownStartTime 以降）。</summary>
        public bool IsSlidingDown => State == PettanState.Stuck && Elapsed >= _settings.SlideDownStartTime - TimeTolerance;

        public Vector3 Scale => State == PettanState.Stuck ? _settings.StuckScale : Vector3.One;

        public bool CanControl => State == PettanState.Normal || State == PettanState.Invincible;

        /// <summary>杵で叩かれるか。HitStop 中も叩かれない。</summary>
        public bool CanBeHit => State == PettanState.Normal;

        /// <summary>他のプレイヤーと衝突するか。吹っ飛び中はすり抜ける。</summary>
        public bool CollidesWithPlayers => State != PettanState.Launched && State != PettanState.Stuck;

        /// <summary>コライダーを有効にするか。貼り付き中は無効。</summary>
        public bool ColliderEnabled => State != PettanState.Stuck;

        public event Action<PettanState> StateChanged;

        /// <summary>復帰した瞬間（「ポンッ！」SE 用）。</summary>
        public event Action Recovered;

        /// <summary>被弾を始める。Normal 以外のときは無視する。</summary>
        /// <param name="position">被弾した位置（床面 XZ）。</param>
        /// <param name="launchDirection">攻撃者の正面方向。長さは問わない。</param>
        public void Begin(Vector2 position, Vector2 launchDirection)
        {
            if (launchDirection == Vector2.Zero)
            {
                throw new ArgumentException("Launch direction must not be zero.", nameof(launchDirection));
            }

            if (State != PettanState.Normal)
            {
                return;
            }

            Elapsed = 0f;
            Position = position;
            Height = 0f;
            WallNormal = Vector2.Zero;
            _launchOrigin = position;
            _launchDirection = Vector2.Normalize(launchDirection);

            _willHitWall = _settings.LaunchSpeed > 0f
                && _rink.TryGetWallHit(position, _launchDirection, out _wallPoint, out _wallNormal);
            _wallArrivalTime = _willHitWall
                ? _settings.HitStopDuration + Vector2.Distance(position, _wallPoint) / _settings.LaunchSpeed
                : float.PositiveInfinity;

            ChangeState(PettanState.HitStop);
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Delta time must not be negative.");
            }

            var remaining = deltaTime;
            while (State != PettanState.Normal)
            {
                // 区切りをすでに過ぎていれば（壁に届くのが復帰時刻より遅かった場合など）すぐに次へ進む
                var stateEnd = MathF.Max(Elapsed, CurrentStateEndTime());
                if (Elapsed + remaining < stateEnd - TimeTolerance)
                {
                    Elapsed += remaining;
                    UpdatePose();
                    return;
                }

                remaining = MathF.Max(0f, remaining - (stateEnd - Elapsed));
                Elapsed = stateEnd;
                UpdatePose();
                Advance();
            }
        }

        private float CurrentStateEndTime()
        {
            switch (State)
            {
                case PettanState.HitStop:
                    return _settings.HitStopDuration;
                case PettanState.Launched:
                    return MathF.Min(_wallArrivalTime, _settings.LaunchMaxDuration);
                case PettanState.Stuck:
                    return _settings.RecoverTime;
                case PettanState.Invincible:
                    return _recoverTime + (_settings.InvincibleEndTime - _settings.RecoverTime);
                default:
                    return float.PositiveInfinity;
            }
        }

        private void Advance()
        {
            switch (State)
            {
                case PettanState.HitStop:
                    ChangeState(PettanState.Launched);
                    break;
                case PettanState.Launched:
                    if (_willHitWall && Elapsed >= _wallArrivalTime - TimeTolerance)
                    {
                        Position = _wallPoint;
                        WallNormal = _wallNormal;
                        _stuckHeight = Height;
                        ChangeState(PettanState.Stuck);
                    }
                    else
                    {
                        // 上限までに壁に届かなかった：その場で復帰する
                        Recover();
                    }

                    break;
                case PettanState.Stuck:
                    Recover();
                    break;
                case PettanState.Invincible:
                    Elapsed = 0f;
                    ChangeState(PettanState.Normal);
                    break;
            }
        }

        private void Recover()
        {
            _recoverTime = Elapsed;
            Height = 0f;
            ChangeState(PettanState.Invincible);
            Recovered?.Invoke();
        }

        private void UpdatePose()
        {
            switch (State)
            {
                case PettanState.Launched:
                    var flightTime = Elapsed - _settings.HitStopDuration;
                    Position = _launchOrigin + _launchDirection * (_settings.LaunchSpeed * flightTime);
                    Height = BounceHeight(flightTime);
                    break;
                case PettanState.Stuck:
                    var slideTime = MathF.Max(0f, Elapsed - _settings.SlideDownStartTime);
                    Height = _stuckHeight - _settings.SlideDownSpeed * slideTime;
                    break;
            }
        }

        /// <summary>前半・後半 2 つの放物線（高さ LaunchBounceHeight）。標準の吹っ飛び時間を過ぎたら 0。</summary>
        private float BounceHeight(float flightTime)
        {
            var nominalFlight = _settings.LaunchNominalDuration - _settings.HitStopDuration;
            if (nominalFlight <= 0f || flightTime <= 0f || flightTime >= nominalFlight)
            {
                return 0f;
            }

            var arc = nominalFlight / 2f;
            var t = flightTime < arc ? flightTime : flightTime - arc;
            var q = t / arc;
            return 4f * _settings.LaunchBounceHeight * q * (1f - q);
        }

        private void ChangeState(PettanState next)
        {
            State = next;
            StateChanged?.Invoke(next);
        }
    }
}
