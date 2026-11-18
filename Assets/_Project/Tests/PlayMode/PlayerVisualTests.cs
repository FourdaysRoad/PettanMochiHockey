using System.Collections;
using System.Linq;
using MochiHockey.Core;
using MochiHockey.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MochiHockey.Tests.PlayMode
{
    /// <summary>
    /// TASK-0027：プレイヤーの見た目の差し替え（PlayerVisual）と、ゲームの状態を Animator に渡す仕組み（PlayerAnimatorDriver）。
    /// </summary>
    public class PlayerVisualTests
    {
        private GameBalanceConfig _config;
        private PlayerVisualProfile _profile;
        private GameObject _model;
        private RuntimeAnimatorController _controller;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<GameBalanceConfig>();

            // 仮のモデル：Animator 付きの箱
            _model = new GameObject("TestModel");
            _model.SetActive(false);
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(_model.transform, false);
            _model.AddComponent<Animator>();

#if UNITY_EDITOR
            var controller = new UnityEditor.Animations.AnimatorController();
            controller.AddLayer("Base");
            controller.AddParameter(PlayerAnimatorDriver.SpeedParameter, AnimatorControllerParameterType.Float);
            controller.AddParameter(PlayerAnimatorDriver.PettanStateParameter, AnimatorControllerParameterType.Int);
            controller.AddParameter(PlayerAnimatorDriver.SwingParameter, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(PlayerAnimatorDriver.DizzyParameter, AnimatorControllerParameterType.Bool);
            controller.AddParameter(PlayerAnimatorDriver.ChargingParameter, AnimatorControllerParameterType.Bool);
            _controller = controller;
#endif

            _profile = ScriptableObject.CreateInstance<PlayerVisualProfile>();
            _profile.ModelPrefab = _model;
            _profile.AnimatorController = _controller;
            _profile.LocalPosition = new Vector3(0f, -1f, 0f);
            _profile.LocalScale = Vector3.one * 0.5f;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go.name is "Player" or "TestModel")
                {
                    Object.Destroy(go);
                }
            }

            Object.Destroy(_profile);
            Object.Destroy(_config);
            yield return null;
        }

        // ---- 見た目の差し替え ----

        [Test]
        public void WithoutProfile_KeepsCapsule()
        {
            var player = CreatePlayer(Vector3.zero, Team.A, null);

            Assert.That(player.GetComponent<MeshRenderer>(), Is.Not.Null);
            Assert.That(player.GetComponent<PlayerVisual>().HasModel, Is.False);
            Assert.That(player.transform.Find("Visual"), Is.Null);
        }

        [Test]
        public void WithProfile_ReplacesCapsuleWithModel()
        {
            var player = CreatePlayer(Vector3.zero, Team.A, _profile);

            var visual = player.transform.Find("Visual");
            Assert.That(visual, Is.Not.Null);
            Assert.That(player.GetComponent<PlayerVisual>().HasModel, Is.True);
            Assert.That(player.GetComponent<MeshRenderer>(), Is.Null, "カプセルの見た目が残っている");
            Assert.That(player.GetComponent<Collider>(), Is.Not.Null, "当たり判定は残す");
            Assert.That(visual.localPosition, Is.EqualTo(new Vector3(0f, -1f, 0f)));
            Assert.That(visual.localScale, Is.EqualTo(Vector3.one * 0.5f));
            Assert.That(visual.gameObject.activeSelf, Is.True);
            Assert.That(visual.GetComponentsInChildren<Collider>(), Is.Empty);
        }

        [UnityTest]
        public IEnumerator ModelBlinksWhileInvincible()
        {
            var attacker = CreatePlayer(new Vector3(6.5f, 1f, 0f), Team.A, null);
            var target = CreatePlayer(new Vector3(8f, 1f, 0f), Team.B, _profile);
            attacker.transform.rotation = Quaternion.LookRotation(Vector3.right);
            var renderer = target.transform.Find("Visual").GetComponentInChildren<Renderer>();
            yield return null;

            attacker.RequestSwing();
            while (target.PettanState != PettanState.Invincible)
            {
                yield return null;
            }

            var sawHidden = false;
            while (target.PettanState == PettanState.Invincible)
            {
                sawHidden |= !renderer.enabled;
                yield return null;
            }

            Assert.That(sawHidden, Is.True);
            Assert.That(renderer.enabled, Is.True);
        }

        // ---- Animator に渡す値 ----

        [UnityTest]
        public IEnumerator Moving_SetsSpeed()
        {
            var player = CreatePlayer(Vector3.zero, Team.A, _profile);
            var animator = AnimatorOf(player);

            player.SetMoveInput(Vector2.right);
            yield return new WaitForSeconds(0.3f);

            Assert.That(animator.GetFloat(PlayerAnimatorDriver.SpeedParameter), Is.GreaterThan(1f));
        }

        [UnityTest]
        public IEnumerator Swing_SetsTrigger()
        {
            var player = CreatePlayer(Vector3.zero, Team.A, _profile);
            var animator = AnimatorOf(player);
            yield return null;

            player.RequestSwing();
            yield return null;

            Assert.That(animator.GetBool(PlayerAnimatorDriver.SwingParameter), Is.True);
        }

        [UnityTest]
        public IEnumerator Charging_SetsCharging()
        {
            var player = CreatePlayer(Vector3.zero, Team.A, _profile);
            var animator = AnimatorOf(player);

            player.BeginShoot();
            yield return new WaitForSeconds(0.1f);
            Assert.That(animator.GetBool(PlayerAnimatorDriver.ChargingParameter), Is.True);

            player.ReleaseShoot();
            yield return null;
            Assert.That(animator.GetBool(PlayerAnimatorDriver.ChargingParameter), Is.False);
        }

        [UnityTest]
        public IEnumerator Hit_SetsPettanStateAndDizzyWhileInvincible()
        {
            var attacker = CreatePlayer(new Vector3(6.5f, 1f, 0f), Team.A, null);
            var target = CreatePlayer(new Vector3(8f, 1f, 0f), Team.B, _profile);
            attacker.transform.rotation = Quaternion.LookRotation(Vector3.right);
            var animator = AnimatorOf(target);
            yield return null;

            attacker.RequestSwing();
            yield return new WaitForSeconds(0.5f);
            Assert.That(animator.GetInteger(PlayerAnimatorDriver.PettanStateParameter), Is.EqualTo((int)target.PettanState));
            Assert.That(animator.GetBool(PlayerAnimatorDriver.DizzyParameter), Is.False);

            while (target.PettanState != PettanState.Invincible)
            {
                yield return null;
            }

            yield return null;
            Assert.That(animator.GetBool(PlayerAnimatorDriver.DizzyParameter), Is.True);
        }

        [UnityTest]
        public IEnumerator ControllerWithoutParameters_IsIgnoredQuietly()
        {
#if UNITY_EDITOR
            var empty = new UnityEditor.Animations.AnimatorController();
            empty.AddLayer("Base");
            _profile.AnimatorController = empty;
#endif
            var player = CreatePlayer(Vector3.zero, Team.A, _profile);
            player.SetMoveInput(Vector2.right);
            player.RequestSwing();

            yield return new WaitForSeconds(0.2f);

            LogAssert.NoUnexpectedReceived();
        }

        private PlayerController CreatePlayer(Vector3 position, Team team, PlayerVisualProfile profile)
        {
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.SetActive(false);
            player.transform.position = position;
            player.AddComponent<Rigidbody>().useGravity = false;
            var visual = player.AddComponent<PlayerVisual>();
            visual.Profile = profile;
            visual.UseLocalProfile = false;
            var controller = player.AddComponent<PlayerController>();
            controller.Config = _config;
            controller.Team = team;
            player.AddComponent<MalletHitbox>();
            player.SetActive(true);
            return controller;
        }

        private static Animator AnimatorOf(PlayerController player)
        {
            var animator = player.GetComponentsInChildren<Animator>(true).FirstOrDefault();
            Assert.That(animator, Is.Not.Null);
            return animator;
        }
    }
}
