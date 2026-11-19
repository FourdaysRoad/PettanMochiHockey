using System;
using System.Numerics;

namespace MochiHockey.Core
{
    /// <summary>壁キックの状態（REQ-028, REQ-029）。</summary>
    public enum WallKickPhase
    {
        /// <summary>何もしていない。</summary>
        None,

        /// <summary>壁の近くでダッシュを押した。長押しか（壁キック）、短く離すか（ふつうのダッシュ）を待っている。</summary>
        Holding,

        /// <summary>壁に張り付いて、矢印で向きを狙っている。</summary>
        Aiming,

        /// <summary>狙った向きへまっすぐ飛んでいる。</summary>
        Flying,

        /// <summary>飛んだ先の壁に張り付いている。受付時間内にダッシュを押せば、次の壁キックを狙える（連鎖）。</summary>
        Clinging,
    }

    /// <summary>ダッシュボタンを離した・時間が進んだ結果、呼び出し側がすること。</summary>
    public enum WallKickResult
    {
        /// <summary>何もしない。</summary>
        None,

        /// <summary>短く押しただけだったので、ふつうのダッシュをする。</summary>
        Dash,

        /// <summary>壁キックで飛び出した（Velocity の向き・速さで動かす）。</summary>
        Launched,
    }

    /// <summary>
    /// 壁キック（REQ-028）と連鎖（REQ-029）。位置は呼び出し側（Runtime）が物理で動かし、毎フレーム Tick で渡す。
    /// - 壁の近くでダッシュを押すと Holding。HoldTime まで押し続けたら Aiming（壁に張り付いて止まる）、その前に離したらふつうのダッシュ
    /// - Aiming の間は SetAim で向きを決める（壁から離れる向きに限る）。離すか AimMaxTime が過ぎたら Flying
    /// - Flying で壁に向かって着いたら Clinging。ChainWindow の間にダッシュを押せば、すぐ Aiming（連鎖が続く）
    /// - 蹴るたびに Chain が増え、速さは BaseSpeed × (1 + ChainSpeedBonus × (Chain − 1))、MaxSpeed まで
    /// - 受付時間切れ・飛びすぎ・Cancel（叩かれたときなど）で None に戻り、連鎖は 0 になる
    /// </summary>
    public sealed class WallKick
    {
        // これより小さい入力は「スティックを倒していない」とみなす
        private const float InputDeadZone = 0.2f;

        private readonly WallKickSettings _settings;
        private readonly RinkBounds _rink;
        private float _timer;

        public WallKick(WallKickSettings settings, RinkBounds rink)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _rink = rink;
        }

        public WallKickPhase Phase { get; private set; } = WallKickPhase.None;

        /// <summary>続けて蹴った回数（飛んでいる・張り付いている間）。None に戻ると 0。</summary>
        public int Chain { get; private set; }

        /// <summary>張り付いている壁の内向きの法線（Holding・Aiming・Clinging）。</summary>
        public Vector2 WallNormal { get; private set; }

        /// <summary>狙っている向き（長さ 1）。</summary>
        public Vector2 AimDirection { get; private set; }

        /// <summary>飛んでいる速度（m/s）。Flying 以外では 0。</summary>
        public Vector2 Velocity { get; private set; }

        /// <summary>今の（最後に蹴った）速さ（m/s）。</summary>
        public float Speed { get; private set; }

        /// <summary>壁に張り付いて止まっているか（Aiming・Clinging）。</summary>
        public bool IsHoldingPosition => Phase == WallKickPhase.Aiming || Phase == WallKickPhase.Clinging;

        /// <summary>移動を壁キックが決めているか（Aiming・Flying・Clinging）。このときはふつうの移動・ダッシュをしない。</summary>
        public bool ControlsMovement => IsHoldingPosition || Phase == WallKickPhase.Flying;

        /// <summary>いちばん近い壁が Range 以内にあれば、その壁の内向きの法線を返す。</summary>
        public bool TryGetNearWall(Vector2 position, out Vector2 normal)
        {
            normal = default;
            if (!_rink.IsValid)
            {
                return false;
            }

            return FindWall(position, Vector2.Zero, false, out normal);
        }

        /// <summary>
        /// ダッシュボタンが押された。壁キックとして受け取ったら true（呼び出し側はふつうのダッシュをしない）。
        /// </summary>
        /// <param name="canKick">壁キックしてよいか（餅を持っていない・操作できるとき）。</param>
        public bool PressDash(Vector2 position, bool canKick)
        {
            switch (Phase)
            {
                case WallKickPhase.Clinging:
                    if (!canKick)
                    {
                        return true;
                    }

                    BeginAiming();
                    return true;
                case WallKickPhase.None:
                    if (!canKick || !TryGetNearWall(position, out var normal))
                    {
                        return false;
                    }

                    Phase = WallKickPhase.Holding;
                    WallNormal = normal;
                    AimDirection = normal;
                    _timer = 0f;
                    return true;
                default:
                    // 長押しを待っている・狙っている・飛んでいる間は、押しても何も起きない
                    return true;
            }
        }

        /// <summary>ダッシュボタンが離された。</summary>
        public WallKickResult ReleaseDash()
        {
            switch (Phase)
            {
                case WallKickPhase.Holding:
                    Phase = WallKickPhase.None;
                    return WallKickResult.Dash;
                case WallKickPhase.Aiming:
                    Launch();
                    return WallKickResult.Launched;
                default:
                    return WallKickResult.None;
            }
        }

        /// <summary>狙う向き（移動の入力）。壁の中へ向けたら、壁から離れる向きに直す。</summary>
        public void SetAim(Vector2 input)
        {
            if (Phase != WallKickPhase.Aiming || input.Length() < InputDeadZone)
            {
                return;
            }

            AimDirection = Clamp(Vector2.Normalize(input));
        }

        /// <param name="position">プレイヤーの今の位置（床面）。</param>
        public WallKickResult Tick(float deltaTime, Vector2 position)
        {
            if (deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Delta time must not be negative.");
            }

            _timer += deltaTime;
            switch (Phase)
            {
                case WallKickPhase.Holding:
                    if (_timer >= _settings.HoldTime)
                    {
                        BeginAiming();
                    }

                    return WallKickResult.None;
                case WallKickPhase.Aiming:
                    if (_timer >= _settings.AimMaxTime)
                    {
                        Launch();
                        return WallKickResult.Launched;
                    }

                    return WallKickResult.None;
                case WallKickPhase.Flying:
                    if (TryGetArrivedWall(position, out var wall))
                    {
                        Phase = WallKickPhase.Clinging;
                        WallNormal = wall;
                        AimDirection = wall;
                        Velocity = Vector2.Zero;
                        _timer = 0f;
                    }
                    else if (_timer >= _settings.MaxFlightTime)
                    {
                        Cancel();
                    }

                    return WallKickResult.None;
                case WallKickPhase.Clinging:
                    if (_timer >= _settings.ChainWindow)
                    {
                        Cancel();
                    }

                    return WallKickResult.None;
                default:
                    return WallKickResult.None;
            }
        }

        /// <summary>やめる（叩かれた・キックオフなど）。連鎖も 0 に戻す。</summary>
        public void Cancel()
        {
            Phase = WallKickPhase.None;
            Chain = 0;
            Velocity = Vector2.Zero;
            _timer = 0f;
        }

        private void BeginAiming()
        {
            Phase = WallKickPhase.Aiming;
            AimDirection = WallNormal;
            Velocity = Vector2.Zero;
            _timer = 0f;
        }

        private void Launch()
        {
            Chain++;
            Speed = MathF.Min(_settings.BaseSpeed * (1f + _settings.ChainSpeedBonus * (Chain - 1)), _settings.MaxSpeed);
            Velocity = AimDirection * Speed;
            Phase = WallKickPhase.Flying;
            _timer = 0f;
        }

        /// <summary>壁に向かって飛んでいて、その壁の Range 以内に入ったか。</summary>
        private bool TryGetArrivedWall(Vector2 position, out Vector2 normal)
        {
            normal = default;
            if (!_rink.IsValid || Velocity == Vector2.Zero)
            {
                return false;
            }

            return FindWall(position, Vector2.Normalize(Velocity), true, out normal);
        }

        /// <summary>
        /// Range 以内でいちばん近い壁を探す。onlyApproaching なら、direction がその壁へ向かっている
        /// （法線の逆との内積が MinAwayDot 以上の）壁だけを見る。
        /// </summary>
        private bool FindWall(Vector2 position, Vector2 direction, bool onlyApproaching, out Vector2 normal)
        {
            normal = default;
            var found = false;
            var best = _settings.Range;
            for (var i = 0; i < 4; i++)
            {
                float distance;
                Vector2 wallNormal;
                switch (i)
                {
                    case 0:
                        distance = position.X - _rink.Min.X;
                        wallNormal = new Vector2(1f, 0f);
                        break;
                    case 1:
                        distance = _rink.Max.X - position.X;
                        wallNormal = new Vector2(-1f, 0f);
                        break;
                    case 2:
                        distance = position.Y - _rink.Min.Y;
                        wallNormal = new Vector2(0f, 1f);
                        break;
                    default:
                        distance = _rink.Max.Y - position.Y;
                        wallNormal = new Vector2(0f, -1f);
                        break;
                }

                if (distance > best || (onlyApproaching && Vector2.Dot(direction, -wallNormal) < _settings.MinAwayDot))
                {
                    continue;
                }

                best = distance;
                normal = wallNormal;
                found = true;
            }

            return found;
        }

        /// <summary>壁から離れる向き（法線との内積が MinAwayDot 以上）に直す。横の向きは残す。</summary>
        private Vector2 Clamp(Vector2 direction)
        {
            var away = Vector2.Dot(direction, WallNormal);
            if (away >= _settings.MinAwayDot)
            {
                return direction;
            }

            var along = direction - WallNormal * away;
            if (along.Length() < 1e-4f)
            {
                return WallNormal;
            }

            var side = MathF.Sqrt(MathF.Max(0f, 1f - _settings.MinAwayDot * _settings.MinAwayDot));
            return Vector2.Normalize(Vector2.Normalize(along) * side + WallNormal * _settings.MinAwayDot);
        }
    }
}
