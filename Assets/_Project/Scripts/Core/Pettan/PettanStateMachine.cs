using System;
using System.Collections.Generic;
using System.Numerics;

namespace MochiHockey.Core
{
    /// <summary>
    /// 被弾後の状態と軌道（REQ-031〜035）。Runtime はこの値を kinematic で反映する。
    /// 時刻はすべて Begin からの経過秒で、PettanSettings の値を区切りに使う：
    /// HitStop → Launched（HitStopDuration）→ 壁到達で Stuck → Recover で Invincible（RecoverTime）→ Normal。
    /// 吹っ飛び：攻撃者の正面方向へ、高さを変えずに水平に等速で直進する。壁に当たると反射して跳ね返り、
    /// LaunchWallBounces 回跳ね返ったあと、次に当たった壁に貼り付く。
    /// LaunchMaxDuration までに貼り付けなければ、その場で復帰する。
    /// 張り付き：SlideDownStartTime から SlideDownSpeed で壁沿いに下がる。
    /// </summary>
    public sealed class PettanStateMachine
    {
        // 1/60 秒を積み重ねた誤差で区切りの時刻を越えられないことがないようにする許容差
        private const float TimeTolerance = 1e-5f;

        private readonly PettanSettings _settings;
        private readonly RinkBounds _rink;

        // 吹っ飛びの道のり。壁で跳ね返るたびに次の区間になる
        private readonly List<LaunchLeg> _legs = new List<LaunchLeg>();
        private int _nextBounceLeg;
        private bool _willHitWall;
        private Vector2 _wallPoint;
        private Vector2 _wallNormal;
        private float _wallArrivalTime;
        private float _recoverTime;

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
        /// 通常の立ち位置からの高さ（Y）。吹っ飛び中は 0（水平に飛ぶ）。
        /// 張り付き中のずり落ちで 0 より下がる（Runtime が貼り付けた位置に足して使う）。
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

        /// <summary>吹っ飛び中に壁で跳ね返った瞬間（SE・VFX 用）。</summary>
        public event Action Bounced;

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
            PlanLaunch(position, Vector2.Normalize(launchDirection));

            ChangeState(PettanState.HitStop);
        }

        /// <summary>どの状態からでも、すぐに Normal に戻す（キックオフのやり直しなど）。Normal なら何もしない。</summary>
        public void Reset()
        {
            if (State == PettanState.Normal)
            {
                return;
            }

            Elapsed = 0f;
            Height = 0f;
            WallNormal = Vector2.Zero;
            _legs.Clear();
            _willHitWall = false;
            ChangeState(PettanState.Normal);
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
                        Height = 0f;
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

        /// <summary>
        /// 被弾した時点で、壁で跳ね返る道のりを先に決めておく。
        /// 跳ね返るたびに区間を足し、最後に当たる壁を貼り付く位置にする。
        /// </summary>
        private void PlanLaunch(Vector2 origin, Vector2 direction)
        {
            _legs.Clear();
            _legs.Add(new LaunchLeg(origin, direction, 0f));
            _nextBounceLeg = 1;
            _willHitWall = false;
            _wallArrivalTime = float.PositiveInfinity;

            if (_settings.LaunchSpeed <= 0f)
            {
                return;
            }

            var distance = 0f;
            for (var bounce = 0; ; bounce++)
            {
                var leg = _legs[_legs.Count - 1];
                if (!_rink.TryGetWallHit(leg.Start, leg.Direction, out var hit, out var normal))
                {
                    return;
                }

                distance += Vector2.Distance(leg.Start, hit);
                if (bounce >= _settings.LaunchWallBounces)
                {
                    _willHitWall = true;
                    _wallPoint = hit;
                    _wallNormal = normal;
                    _wallArrivalTime = _settings.HitStopDuration + distance / _settings.LaunchSpeed;
                    return;
                }

                _legs.Add(new LaunchLeg(hit, Vector2.Reflect(leg.Direction, normal), distance));
            }
        }

        private void UpdatePose()
        {
            switch (State)
            {
                case PettanState.Launched:
                    var traveled = _settings.LaunchSpeed * (Elapsed - _settings.HitStopDuration);
                    RaiseBouncesUpTo(traveled);
                    var leg = _legs[_nextBounceLeg - 1];
                    Position = leg.Start + leg.Direction * (traveled - leg.StartDistance);
                    Height = 0f;
                    break;
                case PettanState.Stuck:
                    var slideTime = MathF.Max(0f, Elapsed - _settings.SlideDownStartTime);
                    Height = -_settings.SlideDownSpeed * slideTime;
                    break;
            }
        }

        /// <summary>進んだ距離までにある跳ね返りを、順に知らせる。</summary>
        private void RaiseBouncesUpTo(float traveled)
        {
            while (_nextBounceLeg < _legs.Count && _legs[_nextBounceLeg].StartDistance <= traveled + TimeTolerance)
            {
                _nextBounceLeg++;
                Bounced?.Invoke();
            }
        }

        private void ChangeState(PettanState next)
        {
            State = next;
            StateChanged?.Invoke(next);
        }

        /// <summary>吹っ飛びの道のりの 1 区間（壁から壁まで）。</summary>
        private readonly struct LaunchLeg
        {
            public LaunchLeg(Vector2 start, Vector2 direction, float startDistance)
            {
                Start = start;
                Direction = direction;
                StartDistance = startDistance;
            }

            public Vector2 Start { get; }
            public Vector2 Direction { get; }

            /// <summary>吹っ飛び始めから、この区間の始まりまでの距離。</summary>
            public float StartDistance { get; }
        }
    }
}
