using System.Collections;
using System.Linq;
using MochiHockey.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MochiHockey.Tests.PlayMode
{
    /// <summary>Match シーンの組み立て（TASK-0014 のエディタ手順）が崩れていないか。</summary>
    public class MatchSceneTests
    {
        /// <summary>Match シーンを開いたままにすると、後のテストでシーンのプレイヤーが動き回るので閉じる。</summary>
        [UnityTearDown]
        public IEnumerator UnloadMatchScene()
        {
            var match = SceneManager.GetSceneByName("Match");
            if (!match.isLoaded)
            {
                yield break;
            }

            var empty = SceneManager.CreateScene("EmptyAfterMatchSceneTests");
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(match);
        }

        [UnityTest]
        public IEnumerator MatchScene_HasTwoWorkingPlayers()
        {
            yield return SceneManager.LoadSceneAsync("Match", LoadSceneMode.Single);
            yield return MatchSceneHelper.WaitUntilPlaying();
            yield return null;

            var controllers = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

            Assert.That(controllers.Length, Is.EqualTo(2));
            Assert.That(controllers.All(c => c.enabled), Is.True, "Config が入っていない PlayerController がある");
            Assert.That(controllers.All(c => c.GetComponent<PlayerInputReader>().enabled), Is.True,
                "Actions が入っていない PlayerInputReader がある");
        }

        [UnityTest]
        public IEnumerator MatchScene_HasWorkingMochiBallAndRiceSpawner()
        {
            yield return SceneManager.LoadSceneAsync("Match", LoadSceneMode.Single);
            yield return MatchSceneHelper.WaitUntilPlaying();
            yield return null;
            yield return null;

            var balls = Object.FindObjectsByType<MochiBall>(FindObjectsSortMode.None);
            var spawner = Object.FindFirstObjectByType<MochiRiceSpawner>();

            Assert.That(balls.Length, Is.EqualTo(1));
            Assert.That(balls[0].enabled, Is.True, "MochiBall の Config が入っていない");
            Assert.That(spawner, Is.Not.Null);
            Assert.That(spawner.enabled, Is.True, "MochiRiceSpawner の Config か Rice Prefab が入っていない");
            Assert.That(spawner.ActiveCount, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator MatchScene_PlayersHaveMalletsAndOpposingTeams()
        {
            yield return SceneManager.LoadSceneAsync("Match", LoadSceneMode.Single);
            yield return MatchSceneHelper.WaitUntilPlaying();

            var player1 = GameObject.Find("Player1").GetComponent<PlayerController>();
            var player2 = GameObject.Find("Player2").GetComponent<PlayerController>();

            Assert.That(player1.GetComponent<MalletHitbox>(), Is.Not.Null, "Player1 に Mallet Hitbox がない");
            Assert.That(player2.GetComponent<MalletHitbox>(), Is.Not.Null, "Player2 に Mallet Hitbox がない");
            Assert.That(player1.Team, Is.Not.EqualTo(player2.Team), "Player1 と Player2 が同じチーム");
        }

        [UnityTest]
        public IEnumerator MatchScene_HasMatchManagerAndAGoalOnEachTeamsSide()
        {
            yield return SceneManager.LoadSceneAsync("Match", LoadSceneMode.Single);
            yield return MatchSceneHelper.WaitUntilPlaying();

            var manager = Object.FindFirstObjectByType<MatchManager>();
            Assert.That(manager, Is.Not.Null, "MatchManager がない");
            Assert.That(manager.enabled, Is.True, "MatchManager の Config か Ball が入っていない");
            Assert.That(manager.Rules.Phase, Is.EqualTo(MochiHockey.Core.MatchPhase.Playing));

            var goals = Object.FindObjectsByType<GoalPost>(FindObjectsSortMode.None);
            Assert.That(goals.Length, Is.EqualTo(2));
            Assert.That(goals.Select(g => g.DefendingTeam).Distinct().Count(), Is.EqualTo(2), "2 つのゴールが同じチーム");

            // 各チームは自分の側（キックオフの位置の側）にあるゴールを守る
            foreach (var player in Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            {
                var ownGoal = goals.Single(g => g.DefendingTeam == player.Team);
                Assert.That(Mathf.Sign(ownGoal.transform.position.x), Is.EqualTo(Mathf.Sign(player.transform.position.x)),
                    $"{player.name}（チーム {player.Team}）のゴールが反対側にある");
            }
        }

        [UnityTest]
        public IEnumerator MatchScene_PlayerMovesWhenGivenInput()
        {
            yield return SceneManager.LoadSceneAsync("Match", LoadSceneMode.Single);
            yield return MatchSceneHelper.WaitUntilPlaying();
            var player = GameObject.Find("Player1").GetComponent<PlayerController>();
            player.GetComponent<PlayerInputReader>().enabled = false;
            var start = player.transform.position;

            player.SetMoveInput(new Vector2(1f, 0f));
            yield return new WaitForSeconds(0.5f);

            Assert.That(player.transform.position.x, Is.GreaterThan(start.x + 0.5f));
        }
    }
}
