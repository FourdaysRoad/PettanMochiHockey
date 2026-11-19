using System;
using System.Numerics;
using MochiHockey.Core;
using NUnit.Framework;

namespace MochiHockey.Tests.EditMode
{
    /// <summary>TASK-0034：壁キック（REQ-028）と連鎖（REQ-029）。</summary>
    public class WallKickTests
    {
        // リンクは x -12〜12、y -7〜7（右の壁は x = 12、内向きの法線は (-1, 0)）
        private static readonly RinkBounds Rink = new RinkBounds(new Vector2(-12f, -7f), new Vector2(12f, 7f));
        private static readonly Vector2 NearRightWall = new Vector2(11.4f, 0f);
        private static readonly Vector2 Center = Vector2.Zero;

        private WallKickSettings _settings;
        private WallKick _kick;

        [SetUp]
        public void SetUp()
        {
            _settings = TestSettings.CreateWallKick();
            _kick = new WallKick(_settings, Rink);
        }

        [Test]
        public void Constructor_NullSettings_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new WallKick(null, Rink));
        }

        // ---- 壁の近くか ----

        [Test]
        public void NearWall_FindsTheClosestWallAndItsInwardNormal()
        {
            Assert.That(_kick.TryGetNearWall(NearRightWall, out var normal), Is.True);
            Assert.That(normal, Is.EqualTo(new Vector2(-1f, 0f)));

            Assert.That(_kick.TryGetNearWall(new Vector2(0f, -6.5f), out normal), Is.True);
            Assert.That(normal, Is.EqualTo(new Vector2(0f, 1f)));
        }

        [Test]
        public void FarFromWalls_IsNotNear()
        {
            Assert.That(_kick.TryGetNearWall(Center, out _), Is.False);
            Assert.That(_kick.TryGetNearWall(new Vector2(12f - _settings.Range - 0.1f, 0f), out _), Is.False);
        }

        // ---- 押す・離す ----

        [Test]
        public void PressAwayFromWall_IsANormalDash()
        {
            Assert.That(_kick.PressDash(Center, canKick: true), Is.False, "壁から遠いときはふつうのダッシュ（すぐ出す）");
            Assert.That(_kick.Phase, Is.EqualTo(WallKickPhase.None));
        }

        [Test]
        public void PressNearWall_WaitsToSeeIfItIsHeld()
        {
            Assert.That(_kick.PressDash(NearRightWall, canKick: true), Is.True);
            Assert.That(_kick.Phase, Is.EqualTo(WallKickPhase.Holding));
        }

        [Test]
        public void PressNearWall_WhenNotAllowed_IsANormalDash()
        {
            // 餅を持っている・被弾中などは壁キックできない
            Assert.That(_kick.PressDash(NearRightWall, canKick: false), Is.False);
            Assert.That(_kick.Phase, Is.EqualTo(WallKickPhase.None));
        }

        [Test]
        public void ShortPressNearWall_BecomesANormalDash()
        {
            _kick.PressDash(NearRightWall, true);
            _kick.Tick(_settings.HoldTime * 0.5f, NearRightWall);

            Assert.That(_kick.ReleaseDash(), Is.EqualTo(WallKickResult.Dash));
            Assert.That(_kick.Phase, Is.EqualTo(WallKickPhase.None));
        }

        [Test]
        public void LongPress_StartsAimingAwayFromTheWall()
        {
            _kick.PressDash(NearRightWall, true);
            _kick.Tick(_settings.HoldTime + 0.01f, NearRightWall);

            Assert.That(_kick.Phase, Is.EqualTo(WallKickPhase.Aiming));
            Assert.That(_kick.WallNormal, Is.EqualTo(new Vector2(-1f, 0f)));
            Assert.That(_kick.AimDirection, Is.EqualTo(new Vector2(-1f, 0f)), "入力がなければ壁の正面");
            Assert.That(_kick.IsHoldingPosition, Is.True, "狙っている間は壁に張り付いて止まる");
        }

        // ---- 狙う ----

        [Test]
        public void Aim_FollowsTheStick()
        {
            StartAiming();
            _kick.SetAim(Vector2.Normalize(new Vector2(-1f, 1f)));

            Assert.That(_kick.AimDirection.X, Is.EqualTo(-0.7071f).Within(1e-3f));
            Assert.That(_kick.AimDirection.Y, Is.EqualTo(0.7071f).Within(1e-3f));
        }

        [Test]
        public void Aim_IntoTheWall_IsTurnedAway()
        {
            StartAiming();
            _kick.SetAim(new Vector2(1f, 0.2f)); // 壁の中へ向けた

            Assert.That(Vector2.Dot(_kick.AimDirection, _kick.WallNormal), Is.GreaterThanOrEqualTo(_settings.MinAwayDot - 1e-4f));
            Assert.That(_kick.AimDirection.Y, Is.GreaterThan(0f), "横の向きは残す");
            Assert.That(_kick.AimDirection.Length(), Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void Aim_StraightIntoTheWall_FacesStraightOut()
        {
            StartAiming();
            _kick.SetAim(new Vector2(1f, 0f));

            Assert.That(_kick.AimDirection, Is.EqualTo(new Vector2(-1f, 0f)));
        }

        [Test]
        public void Aim_TinyInput_KeepsThePreviousDirection()
        {
            StartAiming();
            _kick.SetAim(new Vector2(0f, 1f) * 0.9f + new Vector2(-0.3f, 0f));
            var before = _kick.AimDirection;

            _kick.SetAim(new Vector2(0.01f, 0f));
            Assert.That(_kick.AimDirection, Is.EqualTo(before));
        }

        // ---- 飛ぶ ----

        [Test]
        public void Release_LaunchesAlongTheAimAtBaseSpeed()
        {
            StartAiming();
            _kick.SetAim(new Vector2(-1f, 0f));

            Assert.That(_kick.ReleaseDash(), Is.EqualTo(WallKickResult.Launched));
            Assert.That(_kick.Phase, Is.EqualTo(WallKickPhase.Flying));
            Assert.That(_kick.Chain, Is.EqualTo(1));
            Assert.That(_kick.Velocity.X, Is.EqualTo(-_settings.BaseSpeed).Within(1e-4f));
            Assert.That(_kick.IsHoldingPosition, Is.False);
        }

        [Test]
        public void AimingTooLong_LaunchesByItself()
        {
            StartAiming();

            Assert.That(_kick.Tick(_settings.AimMaxTime + 0.01f, NearRightWall), Is.EqualTo(WallKickResult.Launched));
            Assert.That(_kick.Phase, Is.EqualTo(WallKickPhase.Flying));
        }

        [Test]
        public void Flying_ReachingAWall_ClingsAndStops()
        {
            Launch(new Vector2(-1f, 0f));

            _kick.Tick(0.1f, new Vector2(0f, 0f));
            Assert.That(_kick.Phase, Is.EqualTo(WallKickPhase.Flying));

            _kick.Tick(0.1f, new Vector2(-11.4f, 0f));
            Assert.That(_kick.Phase, Is.EqualTo(WallKickPhase.Clinging));
            Assert.That(_kick.WallNormal, Is.EqualTo(new Vector2(1f, 0f)));
            Assert.That(_kick.Velocity, Is.EqualTo(Vector2.Zero));
            Assert.That(_kick.IsHoldingPosition, Is.True);
        }

        [Test]
        public void Flying_PassingAlongAWall_DoesNotCling()
        {
            // 壁に沿って飛んでいる（壁に向かっていない）ときは張り付かない
            Launch(Vector2.Normalize(new Vector2(-1f, 0.05f)));
            _kick.Tick(0.1f, new Vector2(0f, 6.6f));

            Assert.That(_kick.Phase, Is.EqualTo(WallKickPhase.Flying));
        }

        [Test]
        public void FlyingTooLong_Ends()
        {
            Launch(new Vector2(-1f, 0f));

            _kick.Tick(_settings.MaxFlightTime + 0.01f, Center);
            Assert.That(_kick.Phase, Is.EqualTo(WallKickPhase.None));
            Assert.That(_kick.Chain, Is.EqualTo(0));
        }

        // ---- 連鎖 ----

        [Test]
        public void Chain_PressWhileClinging_AimsAgainWithoutHolding()
        {
            ClingOnLeftWall();

            Assert.That(_kick.PressDash(new Vector2(-11.4f, 0f), canKick: true), Is.True);
            Assert.That(_kick.Phase, Is.EqualTo(WallKickPhase.Aiming), "張り付いている間は押しただけで狙える");
            Assert.That(_kick.Chain, Is.EqualTo(1));
        }

        [Test]
        public void Chain_EachKickIsFasterUpToTheCap()
        {
            var speeds = new float[8];
            Launch(new Vector2(-1f, 0f));
            speeds[0] = _kick.Speed;
            for (var i = 1; i < speeds.Length; i++)
            {
                var wall = i % 2 == 1 ? new Vector2(-11.4f, 0f) : NearRightWall;
                _kick.Tick(0.01f, wall);
                Assert.That(_kick.Phase, Is.EqualTo(WallKickPhase.Clinging), $"{i} 回目");
                _kick.PressDash(wall, true);
                _kick.SetAim(-_kick.WallNormal * -1f);
                _kick.ReleaseDash();
                speeds[i] = _kick.Speed;
            }

            Assert.That(_kick.Chain, Is.EqualTo(8));
            Assert.That(speeds[1], Is.EqualTo(_settings.BaseSpeed * (1f + _settings.ChainSpeedBonus)).Within(1e-3f));
            Assert.That(speeds[2], Is.GreaterThan(speeds[1]));
            Assert.That(speeds[7], Is.EqualTo(_settings.MaxSpeed).Within(1e-3f), "速さには上限がある");
        }

        [Test]
        public void Chain_NotKickingInTime_Ends()
        {
            ClingOnLeftWall();

            _kick.Tick(_settings.ChainWindow + 0.01f, new Vector2(-11.4f, 0f));
            Assert.That(_kick.Phase, Is.EqualTo(WallKickPhase.None));
            Assert.That(_kick.Chain, Is.EqualTo(0));
            Assert.That(_kick.IsHoldingPosition, Is.False);
        }

        [Test]
        public void Chain_AfterEnding_StartsFromOneAgain()
        {
            ClingOnLeftWall();
            _kick.Tick(_settings.ChainWindow + 0.01f, new Vector2(-11.4f, 0f));

            _kick.PressDash(new Vector2(-11.4f, 0f), true);
            _kick.Tick(_settings.HoldTime + 0.01f, new Vector2(-11.4f, 0f));
            _kick.ReleaseDash();

            Assert.That(_kick.Chain, Is.EqualTo(1));
            Assert.That(_kick.Speed, Is.EqualTo(_settings.BaseSpeed).Within(1e-4f));
        }

        [Test]
        public void Cancel_FromAnyState_ResetsEverything()
        {
            ClingOnLeftWall();

            _kick.Cancel();
            Assert.That(_kick.Phase, Is.EqualTo(WallKickPhase.None));
            Assert.That(_kick.Chain, Is.EqualTo(0));
            Assert.That(_kick.Velocity, Is.EqualTo(Vector2.Zero));
        }

        [Test]
        public void ReleaseWhileFlying_DoesNothing()
        {
            Launch(new Vector2(-1f, 0f));

            Assert.That(_kick.ReleaseDash(), Is.EqualTo(WallKickResult.None));
            Assert.That(_kick.Phase, Is.EqualTo(WallKickPhase.Flying));
        }

        [Test]
        public void PressWhileFlying_IsIgnored()
        {
            Launch(new Vector2(-1f, 0f));

            Assert.That(_kick.PressDash(Center, true), Is.True, "飛んでいる間はダッシュもしない");
            Assert.That(_kick.Phase, Is.EqualTo(WallKickPhase.Flying));
        }

        private void StartAiming()
        {
            _kick.PressDash(NearRightWall, true);
            _kick.Tick(_settings.HoldTime + 0.01f, NearRightWall);
            Assert.That(_kick.Phase, Is.EqualTo(WallKickPhase.Aiming));
        }

        private void Launch(Vector2 aim)
        {
            StartAiming();
            _kick.SetAim(aim);
            Assert.That(_kick.ReleaseDash(), Is.EqualTo(WallKickResult.Launched));
        }

        private void ClingOnLeftWall()
        {
            Launch(new Vector2(-1f, 0f));
            _kick.Tick(0.01f, new Vector2(-11.4f, 0f));
            Assert.That(_kick.Phase, Is.EqualTo(WallKickPhase.Clinging));
        }
    }
}
