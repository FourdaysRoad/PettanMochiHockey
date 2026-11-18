using System.Collections;
using System.Linq;
using MochiHockey.Core;
using MochiHockey.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MochiHockey.Tests.PlayMode
{
    /// <summary>TASK-0021：参加受付 → MatchSession → Match シーンでのプレイヤー生成。</summary>
    public class JoinAndSpawnTests
    {
        private const int Keyboard = 1;
        private const int PadA = 9001;
        private const int PadB = 9002;

        private GameBalanceConfig _config;
        private PlayerController _template;
        private Transform[] _teamASpots;
        private Transform[] _teamBSpots;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            _template = TestWorld.CreatePlayer(_config, Vector3.zero, Vector3.forward);
            _template.name = "PlayerTemplate";
            _template.gameObject.SetActive(false);
            _teamASpots = new[] { Spot(-4f, 2.5f), Spot(-4f, -2.5f) };
            _teamBSpots = new[] { Spot(4f, 2.5f), Spot(4f, -2.5f) };
        }

        [TearDown]
        public void TearDown()
        {
            MatchSession.Clear();
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go.name.StartsWith("Player") || go.name is "Spot" or "Spawner" or "JoinManager")
                {
                    Object.Destroy(go);
                }
            }

            Object.Destroy(_config);
        }

        // ---- MatchSession ----

        [UnityTest]
        public IEnumerator MatchSession_KeepsPlayersAcrossSceneLoads()
        {
            MatchSession.Create(new[]
            {
                new JoinedPlayer(0, Keyboard, PlayerInputReader.KeyboardP1Scheme, Team.A),
                new JoinedPlayer(1, PadA, PlayerInputReader.GamepadScheme, Team.B),
            });

            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(
                "Menu", UnityEngine.SceneManagement.LoadSceneMode.Single);

            Assert.That(MatchSession.Current, Is.Not.Null);
            Assert.That(MatchSession.Current.Players.Count, Is.EqualTo(2));
            Assert.That(MatchSession.Current.Players[1].DeviceId, Is.EqualTo(PadA));
        }

        [Test]
        public void MatchSession_CreateAgain_ReplacesPrevious()
        {
            MatchSession.Create(new[] { new JoinedPlayer(0, Keyboard, PlayerInputReader.KeyboardP1Scheme, Team.A) });
            var first = MatchSession.Current;

            MatchSession.Create(new[]
            {
                new JoinedPlayer(0, PadA, PlayerInputReader.GamepadScheme, Team.A),
                new JoinedPlayer(1, PadB, PlayerInputReader.GamepadScheme, Team.B),
            });

            Assert.That(MatchSession.Current, Is.Not.SameAs(first));
            Assert.That(MatchSession.Current.Players.Count, Is.EqualTo(2));
        }

        // ---- 生成（MatchPlayerSpawner）----

        [UnityTest]
        public IEnumerator Spawner_WithoutSession_SpawnsTwoKeyboardPlayersOneVsOne()
        {
            var spawner = CreateSpawner();
            yield return null;

            var players = spawner.Spawned;
            Assert.That(players.Count, Is.EqualTo(2));
            Assert.That(players[0].name, Is.EqualTo("Player1"));
            Assert.That(players[0].Team, Is.EqualTo(Team.A));
            Assert.That(players[1].name, Is.EqualTo("Player2"));
            Assert.That(players[1].Team, Is.EqualTo(Team.B));
        }

        [UnityTest]
        public IEnumerator Spawner_OneVsOne_PlacesEachPlayerBetweenTheirTeamSpotsFacingCenter()
        {
            var spawner = CreateSpawner();
            yield return null;

            var a = spawner.Spawned[0].transform;
            var b = spawner.Spawned[1].transform;
            Assert.That(Vector3.Distance(a.position, new Vector3(-4f, 1f, 0f)), Is.LessThan(0.01f));
            Assert.That(Vector3.Distance(b.position, new Vector3(4f, 1f, 0f)), Is.LessThan(0.01f));
            Assert.That(Vector3.Dot(a.forward, Vector3.right), Is.GreaterThan(0.99f));
            Assert.That(Vector3.Dot(b.forward, Vector3.left), Is.GreaterThan(0.99f));
        }

        [UnityTest]
        public IEnumerator Spawner_TwoVsTwo_UsesSessionNumbersTeamsAndSpots()
        {
            MatchSession.Create(new[]
            {
                new JoinedPlayer(0, Keyboard, PlayerInputReader.KeyboardP1Scheme, Team.A),
                new JoinedPlayer(1, Keyboard, PlayerInputReader.KeyboardP2Scheme, Team.A),
                new JoinedPlayer(2, PadA, PlayerInputReader.GamepadScheme, Team.B),
                new JoinedPlayer(3, PadB, PlayerInputReader.GamepadScheme, Team.B),
            });

            var spawner = CreateSpawner();
            yield return null;

            var players = spawner.Spawned;
            Assert.That(players.Select(p => p.name), Is.EqualTo(new[] { "Player1", "Player2", "Player3", "Player4" }));
            Assert.That(players.Select(p => p.Team), Is.EqualTo(new[] { Team.A, Team.A, Team.B, Team.B }));
            Assert.That(Vector3.Distance(players[0].transform.position, _teamASpots[0].position), Is.LessThan(0.01f));
            Assert.That(Vector3.Distance(players[1].transform.position, _teamASpots[1].position), Is.LessThan(0.01f));
            Assert.That(Vector3.Distance(players[2].transform.position, _teamBSpots[0].position), Is.LessThan(0.01f));
            Assert.That(Vector3.Distance(players[3].transform.position, _teamBSpots[1].position), Is.LessThan(0.01f));
            Assert.That(players.All(p => p.enabled), Is.True);
        }

        [UnityTest]
        public IEnumerator Spawner_SpawnedPlayersCanMove()
        {
            var spawner = CreateSpawner();
            yield return null;
            var player = spawner.Spawned[0];
            var start = player.transform.position;

            player.SetMoveInput(Vector2.up);
            yield return new WaitForSeconds(0.4f);

            Assert.That(player.transform.position.z, Is.GreaterThan(start.z + 0.3f));
        }

        [UnityTest]
        public IEnumerator Spawner_MissingPrefab_LogsErrorAndSpawnsNothing()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("prefab"));
            var spawner = CreateSpawner(withPrefab: false);
            yield return null;

            Assert.That(spawner.Spawned, Is.Empty);
        }

        // ---- 参加受付（PlayerJoinManager）----

        [Test]
        public void JoinManager_TryJoin_AddsPlayersAndRaisesRosterChanged()
        {
            var join = CreateJoinManager();
            var changes = 0;
            join.RosterChanged += () => changes++;

            Assert.That(join.TryJoin(Keyboard, PlayerInputReader.KeyboardP1Scheme), Is.EqualTo(0));
            Assert.That(join.TryJoin(Keyboard, PlayerInputReader.KeyboardP1Scheme), Is.EqualTo(-1));
            Assert.That(join.TryJoin(PadA, PlayerInputReader.GamepadScheme), Is.EqualTo(1));

            Assert.That(join.Roster.Players.Count, Is.EqualTo(2));
            Assert.That(changes, Is.EqualTo(2));
        }

        [Test]
        public void JoinManager_StartMatch_WithoutEnoughPlayers_DoesNothing()
        {
            var join = CreateJoinManager();
            join.TryJoin(Keyboard, PlayerInputReader.KeyboardP1Scheme);

            Assert.That(join.StartMatch(), Is.False);
            Assert.That(MatchSession.Current, Is.Null);
        }

        [Test]
        public void JoinManager_SetTeamAndLeave_UpdateRoster()
        {
            var join = CreateJoinManager();
            join.TryJoin(Keyboard, PlayerInputReader.KeyboardP1Scheme);
            join.TryJoin(Keyboard, PlayerInputReader.KeyboardP2Scheme);

            join.SetTeam(1, Team.A);
            Assert.That(join.Roster.CanStart, Is.False);

            Assert.That(join.Leave(Keyboard, PlayerInputReader.KeyboardP1Scheme), Is.True);
            Assert.That(join.Roster.Players.Count, Is.EqualTo(1));
        }

        private MatchPlayerSpawner CreateSpawner(bool withPrefab = true)
        {
            var go = new GameObject("Spawner");
            go.SetActive(false);
            var spawner = go.AddComponent<MatchPlayerSpawner>();
            spawner.PlayerPrefab = withPrefab ? _template : null;
            spawner.TeamASpots = _teamASpots;
            spawner.TeamBSpots = _teamBSpots;
            go.SetActive(true);
            return spawner;
        }

        private static PlayerJoinManager CreateJoinManager()
        {
            return new GameObject("JoinManager").AddComponent<PlayerJoinManager>();
        }

        private static Transform Spot(float x, float z)
        {
            var spot = new GameObject("Spot").transform;
            spot.position = new Vector3(x, 1f, z);
            return spot;
        }
    }
}
