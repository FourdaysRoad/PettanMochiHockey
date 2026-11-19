using System.Collections;
using MochiHockey.Core;
using MochiHockey.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MochiHockey.Tests.PlayMode
{
    /// <summary>TASK-0028：ヒットストップ、Pettan!!! の吹き出しと衝撃波、氷しぶき。</summary>
    public class HitEffectsTests
    {
        private GameBalanceConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<GameBalanceConfig>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go.name is "Player" or "PettanVfx" or "PettanBurst")
                {
                    Object.Destroy(go);
                }
            }

            Object.Destroy(_config);
            yield return null;
        }

        // ---- ヒットストップ ----

        [UnityTest]
        public IEnumerator Hit_AttackerAlsoStopsForHitStopDuration()
        {
            var attacker = TestWorld.CreatePlayer(_config, new Vector3(6.5f, 1f, 0f), Vector3.right, Team.A);
            var target = TestWorld.CreatePlayer(_config, new Vector3(8f, 1f, 0f), Vector3.left, Team.B);
            yield return null;
            Assert.That(attacker.IsInHitStop, Is.False);

            var hitStopAtHit = false;
            var targetState = PettanState.Normal;
            void OnHit(PlayerController hit)
            {
                hitStopAtHit = attacker.IsInHitStop;
                targetState = hit.PettanState;
            }

            PlayerController.AnyHitTaken += OnHit;
            attacker.RequestSwing();
            yield return WaitForHit(target);
            PlayerController.AnyHitTaken -= OnHit;

            Assert.That(targetState, Is.EqualTo(PettanState.HitStop));
            Assert.That(hitStopAtHit, Is.True, "叩いた側も止まる");
            Assert.That(attacker.PettanState, Is.EqualTo(PettanState.Normal), "叩いた側は被弾していない");

            yield return new WaitForSeconds(_config.Pettan.HitStopDuration + 0.1f);
            Assert.That(attacker.IsInHitStop, Is.False);
            Assert.That(target.PettanState, Is.Not.EqualTo(PettanState.HitStop));
        }

        [UnityTest]
        public IEnumerator HitStop_PausesTheMalletSwing()
        {
            // ヒットストップを長くして、止まっている間に何フレームか見る（設定は Awake で読むので先に変える）
            _config.Pettan.HitStopDuration = 0.5f;
            var attacker = TestWorld.CreatePlayer(_config, new Vector3(6.5f, 1f, 0f), Vector3.right, Team.A);
            attacker.gameObject.SetActive(false);
            var mallet = attacker.gameObject.AddComponent<MalletVisual>();
            attacker.gameObject.SetActive(true);
            var target = TestWorld.CreatePlayer(_config, new Vector3(8f, 1f, 0f), Vector3.left, Team.B);
            yield return null;

            attacker.RequestSwing();
            yield return WaitForHit(target);
            Assert.That(attacker.IsInHitStop, Is.True);
            var yaw = mallet.Pivot.localEulerAngles.y;
            yield return null;
            yield return null;

            Assert.That(attacker.IsInHitStop, Is.True);
            Assert.That(mallet.Pivot.localEulerAngles.y, Is.EqualTo(yaw).Within(0.01f), "止まっている間は杵も止まる");

            yield return new WaitForSeconds(1f);
            Assert.That(mallet.IsSwinging, Is.False, "ヒットストップのあとは振り抜いて戻る");
        }

        [UnityTest]
        public IEnumerator WithoutAttacker_NobodyElseStops()
        {
            // Lv.3 シュートの巻き込みなど、杵以外で吹き飛んだときは、叩いた人がいないので誰も止まらない
            var bystander = TestWorld.CreatePlayer(_config, new Vector3(-5f, 1f, 0f), Vector3.right, Team.A);
            var target = TestWorld.CreatePlayer(_config, new Vector3(0f, 1f, 0f), Vector3.left, Team.B);
            yield return null;

            target.ReceiveHit(Vector3.right);
            yield return null;

            Assert.That(target.PettanState, Is.EqualTo(PettanState.HitStop));
            Assert.That(bystander.IsInHitStop, Is.False);
        }

        [UnityTest]
        public IEnumerator Kickoff_ClearsHitStop()
        {
            _config.Pettan.HitStopDuration = 0.5f;
            var attacker = TestWorld.CreatePlayer(_config, new Vector3(6.5f, 1f, 0f), Vector3.right, Team.A);
            var target = TestWorld.CreatePlayer(_config, new Vector3(8f, 1f, 0f), Vector3.left, Team.B);
            yield return null;

            attacker.RequestSwing();
            yield return WaitForHit(target);
            Assert.That(attacker.IsInHitStop, Is.True);

            attacker.ResetForKickoff(Vector3.zero, Quaternion.identity);
            Assert.That(attacker.IsInHitStop, Is.False);
        }

        // ---- Pettan!!! ----

        [UnityTest]
        public IEnumerator Hit_ShowsPettanBubbleAboveTarget()
        {
            var vfx = CreatePettanVfx();
            var attacker = TestWorld.CreatePlayer(_config, new Vector3(6.5f, 1f, 0f), Vector3.right, Team.A);
            var target = TestWorld.CreatePlayer(_config, new Vector3(8f, 1f, 0f), Vector3.left, Team.B);
            yield return null;
            Assert.That(vfx.LastBurst, Is.Null);

            attacker.RequestSwing();
            yield return WaitForHit(target);

            var burst = vfx.LastBurst;
            Assert.That(burst, Is.Not.Null);
            Assert.That(burst.Text, Is.EqualTo("Pettan!!!"));
            Assert.That(burst.transform.Find("Bubble").position.y,
                Is.EqualTo(target.transform.position.y + _config.Vfx.PettanBubbleHeight).Within(0.3f));
            Assert.That(burst.transform.Find("Bubble").position.x, Is.EqualTo(8f).Within(0.3f));
        }

        [UnityTest]
        public IEnumerator Burst_PopsUpThenFadesAndDisappears()
        {
            var vfx = CreatePettanVfx();
            var target = TestWorld.CreatePlayer(_config, new Vector3(0f, 1f, 0f), Vector3.left, Team.B);
            yield return null;

            target.ReceiveHit(Vector3.right);
            yield return null;
            var burst = vfx.LastBurst;
            Assert.That(burst, Is.Not.Null);

            yield return new WaitForSeconds(0.2f);
            Assert.That(burst.BubbleScale, Is.EqualTo(_config.Vfx.PettanBubbleSize).Within(0.05f), "ポンと出て元の大きさに落ち着く");
            Assert.That(burst.BubbleAlpha, Is.EqualTo(1f));
            Assert.That(burst.WaveDiameter, Is.GreaterThan(_config.Vfx.ShockwaveSize * 0.5f), "衝撃波が広がっている");

            yield return new WaitForSeconds(_config.Vfx.PettanBubbleDuration * 0.85f - 0.2f);
            Assert.That(burst.BubbleAlpha, Is.LessThan(1f), "最後は消えていく");

            yield return new WaitForSeconds(_config.Vfx.PettanBubbleDuration);
            Assert.That(burst == null, Is.True, "時間がたつと消える");
        }

        [UnityTest]
        public IEnumerator EveryHit_GetsItsOwnBubble()
        {
            CreatePettanVfx();
            var a = TestWorld.CreatePlayer(_config, new Vector3(-5f, 1f, 0f), Vector3.left, Team.A);
            var b = TestWorld.CreatePlayer(_config, new Vector3(5f, 1f, 0f), Vector3.left, Team.B);
            yield return null;

            a.ReceiveHit(Vector3.left);
            b.ReceiveHit(Vector3.right);
            yield return null;

            Assert.That(Object.FindObjectsByType<PettanBurst>(FindObjectsSortMode.None).Length, Is.EqualTo(2));
        }

        // ---- 氷しぶき ----

        [UnityTest]
        public IEnumerator SharpTurnAtSpeed_Sprays()
        {
            var skate = CreateSkater();
            var player = skate.GetComponent<PlayerController>();
            player.SetMoveInput(Vector2.right);
            yield return new WaitForSeconds(0.6f);
            Assert.That(skate.Particles.particleCount, Is.EqualTo(0), "まっすぐ滑っている間は出ない");

            player.SetMoveInput(Vector2.left);
            yield return new WaitForSeconds(0.1f);

            Assert.That(skate.Intensity, Is.GreaterThan(0f));
            Assert.That(skate.Particles.particleCount, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator StandingStill_NoSpray()
        {
            var skate = CreateSkater();
            var player = skate.GetComponent<PlayerController>();
            player.SetMoveInput(Vector2.left);
            yield return new WaitForSeconds(0.2f);

            Assert.That(skate.Particles.particleCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Dash_SpraysBurst()
        {
            var skate = CreateSkater();
            var player = skate.GetComponent<PlayerController>();
            yield return null;

            player.RequestDash();
            yield return null;

            Assert.That(skate.Particles.particleCount, Is.GreaterThanOrEqualTo(_config.Vfx.DashSprayCount));
        }

        [UnityTest]
        public IEnumerator SprayParticles_StayOnTheIceAndFade()
        {
            var skate = CreateSkater();
            var player = skate.GetComponent<PlayerController>();
            yield return null;

            player.RequestDash();
            yield return null;
            Assert.That(skate.Particles.main.simulationSpace, Is.EqualTo(ParticleSystemSimulationSpace.World));

            yield return new WaitForSeconds(0.8f);
            Assert.That(skate.Particles.particleCount, Is.EqualTo(0), "粒はすぐ消える");
        }

        /// <summary>叩かれるまで待つ（物理の位置の反映などで、当たるのが数フレーム後になることがある）。</summary>
        private static IEnumerator WaitForHit(PlayerController target)
        {
            var limit = Time.time + 1f;
            while (target.PettanState == PettanState.Normal && Time.time < limit)
            {
                yield return null;
            }

            Assert.That(target.PettanState, Is.Not.EqualTo(PettanState.Normal), "杵が当たらなかった");
        }

        private PettanVfx CreatePettanVfx()
        {
            var go = new GameObject("PettanVfx");
            go.SetActive(false);
            var vfx = go.AddComponent<PettanVfx>();
            vfx.Config = _config;
            go.SetActive(true);
            return vfx;
        }

        private SkateVfx CreateSkater()
        {
            var player = TestWorld.CreatePlayer(_config, new Vector3(0f, 1f, 0f), Vector3.right, Team.A);
            player.gameObject.SetActive(false);
            var skate = player.gameObject.AddComponent<SkateVfx>();
            player.gameObject.SetActive(true);
            return skate;
        }
    }
}
