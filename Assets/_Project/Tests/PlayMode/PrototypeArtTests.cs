using System.Collections;
using MochiHockey.Core;
using MochiHockey.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MochiHockey.Tests.PlayMode
{
    /// <summary>TASK-0027：プリミティブで作る仮の見た目（杵・臼・色付け）。</summary>
    public class PrototypeArtTests
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

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
                if (go.name is "Player" or "Goal" or "Tinted" or "TestModel")
                {
                    Object.Destroy(go);
                }
            }

            Object.Destroy(_config);
            yield return null;
        }

        // ---- 杵 ----

        [Test]
        public void Mallet_IsBuiltWithoutColliders()
        {
            var player = CreatePlayerWithMallet(Vector3.zero, Vector3.forward, Team.A);
            var mallet = player.GetComponent<MalletVisual>();

            Assert.That(mallet.Pivot, Is.Not.Null);
            Assert.That(mallet.Pivot.name, Is.EqualTo(MalletVisual.RootName));
            Assert.That(mallet.Pivot.GetComponentsInChildren<Renderer>().Length, Is.EqualTo(2));
            Assert.That(mallet.Pivot.GetComponentsInChildren<Collider>(true), Is.Empty);
        }

        [Test]
        public void Mallet_UsesThePlayersOwnMaterial()
        {
            // 公開版では CreatePrimitive の標準マテリアルが URP で使えずピンクになるので、プレイヤーのマテリアルを使う
            var player = CreatePlayerWithMallet(Vector3.zero, Vector3.forward, Team.A, out var capsuleMaterial);
            var mallet = player.GetComponent<MalletVisual>();

            foreach (var r in mallet.Pivot.GetComponentsInChildren<Renderer>())
            {
                Assert.That(r.sharedMaterial, Is.SameAs(capsuleMaterial), r.name);
            }
        }

        [Test]
        public void Mallet_KeepsMaterialEvenWhenModelReplacesCapsule()
        {
            // 見た目のモデル（PlayerVisual）がカプセルの見た目を外しても、杵はカプセルのマテリアルを使う
            var model = new GameObject("TestModel");
            model.SetActive(false);
            var profile = ScriptableObject.CreateInstance<PlayerVisualProfile>();
            profile.ModelPrefab = model;

            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.SetActive(false);
            var capsuleMaterial = new Material(player.GetComponent<Renderer>().sharedMaterial) { name = "CapsuleMaterial" };
            player.GetComponent<Renderer>().sharedMaterial = capsuleMaterial;
            player.AddComponent<Rigidbody>().useGravity = false;
            var visual = player.AddComponent<PlayerVisual>();
            visual.Profile = profile;
            visual.UseLocalProfile = false;
            var controller = player.AddComponent<PlayerController>();
            controller.Config = _config;
            player.AddComponent<MalletHitbox>();
            var mallet = player.AddComponent<MalletVisual>();
            player.SetActive(true);

            Assert.That(visual.HasModel, Is.True);
            Assert.That(player.GetComponent<MeshRenderer>(), Is.Null, "カプセルの見た目は外れている");
            foreach (var r in mallet.Pivot.GetComponentsInChildren<Renderer>())
            {
                Assert.That(r.sharedMaterial, Is.SameAs(capsuleMaterial), r.name);
            }

            Object.Destroy(model);
            Object.Destroy(profile);
        }

        [Test]
        public void Mallet_RestsAtConfiguredYaw()
        {
            var player = CreatePlayerWithMallet(Vector3.zero, Vector3.forward, Team.A);
            var mallet = player.GetComponent<MalletVisual>();

            Assert.That(YawOf(mallet.Pivot), Is.EqualTo(_config.Mallet.RestYaw).Within(0.01f));
            Assert.That(mallet.IsSwinging, Is.False);
        }

        [Test]
        public void Mallet_HeadIsAroundTheHitboxReach()
        {
            var player = CreatePlayerWithMallet(Vector3.zero, Vector3.forward, Team.A);
            var head = player.GetComponent<MalletVisual>().Head;

            var planar = head.position - player.transform.position;
            planar.y = 0f;
            Assert.That(planar.magnitude, Is.EqualTo(_config.Mallet.Reach).Within(0.3f));
        }

        [UnityTest]
        public IEnumerator Mallet_SwingsThroughAndReturnsToRest()
        {
            var player = CreatePlayerWithMallet(Vector3.zero, Vector3.forward, Team.A);
            var mallet = player.GetComponent<MalletVisual>();
            yield return null;

            player.RequestSwing();
            Assert.That(mallet.IsSwinging, Is.True);

            yield return new WaitForSeconds(_config.Player.SwingActiveTime * 0.8f);
            Assert.That(YawOf(mallet.Pivot), Is.LessThan(_config.Mallet.RestYaw - 30f), "右から左へ振っている");

            yield return new WaitForSeconds(_config.Player.SwingActiveTime + _config.Mallet.SwingReturnTime + 0.2f);
            Assert.That(mallet.IsSwinging, Is.False);
            Assert.That(YawOf(mallet.Pivot), Is.EqualTo(_config.Mallet.RestYaw).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator Mallet_DoesNotChangeWhoGetsHit()
        {
            // 杵の見た目に当たり判定はない。叩けば今までどおり相手が飛ぶ
            var attacker = CreatePlayerWithMallet(new Vector3(6.5f, 1f, 0f), Vector3.right, Team.A);
            var target = CreatePlayerWithMallet(new Vector3(8f, 1f, 0f), Vector3.left, Team.B);
            yield return null;

            attacker.RequestSwing();
            yield return new WaitForSeconds(0.2f);

            Assert.That(target.PettanState, Is.Not.EqualTo(PettanState.Normal));
            Assert.That(attacker.PettanState, Is.EqualTo(PettanState.Normal));
        }

        // ---- 臼 ----

        [Test]
        public void Usu_HidesGoalRendererButKeepsTrigger()
        {
            var goal = CreateGoalWithUsu(new Vector3(-10f, 1f, 0f));
            var usu = goal.GetComponent<UsuVisual>();

            Assert.That(goal.GetComponent<MeshRenderer>().enabled, Is.False);
            var trigger = goal.GetComponent<Collider>();
            Assert.That(trigger.enabled, Is.True);
            Assert.That(trigger.isTrigger, Is.True);
            Assert.That(usu.Root.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(usu.Root.GetComponentsInChildren<Renderer>().Length, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void Usu_UsesTheGoalsOwnMaterial()
        {
            var goal = TestWorld.CreateGoal(new Vector3(-10f, 1f, 0f), Team.A);
            goal.gameObject.SetActive(false);
            var goalMaterial = new Material(goal.GetComponent<Renderer>().sharedMaterial) { name = "GoalMaterial" };
            goal.GetComponent<Renderer>().sharedMaterial = goalMaterial;
            var usu = goal.gameObject.AddComponent<UsuVisual>();
            goal.gameObject.SetActive(true);

            foreach (var r in usu.Root.GetComponentsInChildren<Renderer>())
            {
                Assert.That(r.sharedMaterial, Is.SameAs(goalMaterial), r.name);
            }
        }

        [Test]
        public void Usu_StandsOnTheFloorWithItsOwnSize()
        {
            // ゴールの Scale（1.5, 2, 3）に関係なく、底が床（コライダーの底）にあり、高さは UsuVisual の値
            var goal = CreateGoalWithUsu(new Vector3(-10f, 1f, 0f));
            var usu = goal.GetComponent<UsuVisual>();

            var bounds = BoundsOf(usu.Root);
            Assert.That(bounds.min.y, Is.EqualTo(0f).Within(0.02f));
            Assert.That(bounds.size.y, Is.EqualTo(usu.Height).Within(0.05f));
            Assert.That(bounds.size.x, Is.EqualTo(bounds.size.z).Within(0.02f), "丸い（Scale でつぶれない）");
        }

        [UnityTest]
        public IEnumerator Usu_StillCountsGoals()
        {
            var goal = CreateGoalWithUsu(new Vector3(-10f, 1f, 0f));
            var ball = TestWorld.CreateBall(_config, new Vector3(-6f, 1f, 0f));
            var entered = false;
            goal.BallEntered += (_, __) => entered = true;
            yield return null;

            ball.GetComponent<Rigidbody>().linearVelocity = Vector3.left * 10f;
            yield return new WaitForSeconds(0.6f);

            Assert.That(entered, Is.True);
            Object.Destroy(ball.gameObject);
        }

        // ---- 色付け ----

        [Test]
        public void ColorTint_SetsColorWithoutNewMaterial()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Tinted";
            var renderer = go.GetComponent<Renderer>();
            var material = renderer.sharedMaterial;

            var tint = go.AddComponent<ColorTint>();
            tint.Color = new Color(0.2f, 0.4f, 0.9f);

            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            var color = block.GetColor(BaseColorId);
            Assert.That(color.r, Is.EqualTo(0.2f).Within(0.01f));
            Assert.That(color.b, Is.EqualTo(0.9f).Within(0.01f));
            Assert.That(renderer.sharedMaterial, Is.SameAs(material));
        }

        // ---- 組み立て ----

        private PlayerController CreatePlayerWithMallet(Vector3 position, Vector3 facing, Team team)
        {
            return CreatePlayerWithMallet(position, facing, team, out _);
        }

        private PlayerController CreatePlayerWithMallet(Vector3 position, Vector3 facing, Team team, out Material capsuleMaterial)
        {
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.SetActive(false);
            capsuleMaterial = new Material(player.GetComponent<Renderer>().sharedMaterial) { name = "CapsuleMaterial" };
            player.GetComponent<Renderer>().sharedMaterial = capsuleMaterial;
            player.transform.SetPositionAndRotation(position, Quaternion.LookRotation(facing, Vector3.up));
            player.AddComponent<Rigidbody>().useGravity = false;
            var controller = player.AddComponent<PlayerController>();
            controller.Config = _config;
            controller.Team = team;
            player.AddComponent<MalletHitbox>();
            player.AddComponent<MalletVisual>();
            player.SetActive(true);
            return controller;
        }

        private static GoalPost CreateGoalWithUsu(Vector3 position)
        {
            var goal = TestWorld.CreateGoal(position, Team.A);
            goal.gameObject.SetActive(false);
            goal.gameObject.AddComponent<UsuVisual>();
            goal.gameObject.SetActive(true);
            return goal;
        }

        private static float YawOf(Transform pivot) => Mathf.DeltaAngle(0f, pivot.localEulerAngles.y);

        private static Bounds BoundsOf(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var r in renderers)
            {
                bounds.Encapsulate(r.bounds);
            }

            return bounds;
        }
    }
}
