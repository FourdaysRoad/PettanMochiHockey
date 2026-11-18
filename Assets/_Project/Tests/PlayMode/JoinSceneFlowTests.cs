using System.Collections;
using System.Linq;
using MochiHockey.Core;
using MochiHockey.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MochiHockey.Tests.PlayMode
{
    /// <summary>TASK-0021：Menu で参加 → Match シーンで同じ番号・チームのプレイヤーが生成される（実際のシーンで確認）。</summary>
    public class JoinSceneFlowTests
    {
        private const int PadA = 9001;
        private const int PadB = 9002;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            MatchSession.Clear();
            var empty = SceneManager.CreateScene("EmptyAfterJoinSceneFlowTests" + Time.frameCount);
            SceneManager.SetActiveScene(empty);
            for (var i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene != empty && scene.isLoaded)
                {
                    yield return SceneManager.UnloadSceneAsync(scene);
                }
            }
        }

        [UnityTest]
        public IEnumerator MatchSceneOpenedDirectly_SpawnsTwoKeyboardPlayersOneVsOne()
        {
            MatchSession.Clear();
            yield return SceneManager.LoadSceneAsync("Match", LoadSceneMode.Single);
            yield return null;

            var players = Players();
            Assert.That(players.Select(p => p.name), Is.EqualTo(new[] { "Player1", "Player2" }));
            Assert.That(players.Select(p => p.Team), Is.EqualTo(new[] { Team.A, Team.B }));
            Assert.That(players[0].transform.position.x, Is.LessThan(0f));
            Assert.That(players[1].transform.position.x, Is.GreaterThan(0f));
            Assert.That(players.All(p => p.GetComponent<PlayerInputReader>().enabled), Is.True);
        }

        [UnityTest]
        public IEnumerator MatchSceneWithFourJoined_SpawnsTwoVsTwoOnTheirSides()
        {
            MatchSession.Create(new[]
            {
                new JoinedPlayer(0, 1, PlayerInputReader.KeyboardP1Scheme, Team.A),
                new JoinedPlayer(1, PadA, PlayerInputReader.GamepadScheme, Team.B),
                new JoinedPlayer(2, 1, PlayerInputReader.KeyboardP2Scheme, Team.A),
                new JoinedPlayer(3, PadB, PlayerInputReader.GamepadScheme, Team.B),
            });

            yield return SceneManager.LoadSceneAsync("Match", LoadSceneMode.Single);
            yield return MatchSceneHelper.WaitUntilPlaying();

            var players = Players();
            Assert.That(players.Select(p => p.name), Is.EqualTo(new[] { "Player1", "Player2", "Player3", "Player4" }));
            Assert.That(players.Select(p => p.Team), Is.EqualTo(new[] { Team.A, Team.B, Team.A, Team.B }));
            foreach (var player in players)
            {
                var side = player.Team == Team.A ? -1f : 1f;
                Assert.That(Mathf.Sign(player.transform.position.x), Is.EqualTo(side), $"{player.name} が自陣にいない");
            }

            // 同じチームの 2 人は別の地点
            Assert.That(Vector3.Distance(players[0].transform.position, players[2].transform.position), Is.GreaterThan(1f));
            Assert.That(Object.FindFirstObjectByType<MatchManager>().Rules.Phase, Is.EqualTo(MatchPhase.Playing));
        }

        [UnityTest]
        public IEnumerator MenuJoinThenStart_LoadsMatchWithJoinedPlayers()
        {
            yield return SceneManager.LoadSceneAsync("Menu", LoadSceneMode.Single);
            var join = Object.FindFirstObjectByType<PlayerJoinManager>();
            Assert.That(join, Is.Not.Null, "Menu シーンに PlayerJoinManager がない");

            join.TryJoin(PadA, PlayerInputReader.GamepadScheme);
            join.TryJoin(PadB, PlayerInputReader.GamepadScheme);
            join.SetTeam(0, Team.B);
            join.SetTeam(1, Team.A);
            Assert.That(join.StartMatch(), Is.True);

            yield return null;
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Match"));

            var players = Players();
            Assert.That(players.Select(p => p.Team), Is.EqualTo(new[] { Team.B, Team.A }));
            Assert.That(players[0].transform.position.x, Is.GreaterThan(0f), "チーム B の 1P が右側にいない");
        }

        private static PlayerController[] Players()
        {
            return Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None)
                .OrderBy(p => p.name)
                .ToArray();
        }
    }
}
