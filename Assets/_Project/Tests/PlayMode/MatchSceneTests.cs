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
        [UnityTest]
        public IEnumerator MatchScene_HasTwoWorkingPlayers()
        {
            yield return SceneManager.LoadSceneAsync("Match", LoadSceneMode.Single);
            yield return null;

            var controllers = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

            Assert.That(controllers.Length, Is.EqualTo(2));
            Assert.That(controllers.All(c => c.enabled), Is.True, "Config が入っていない PlayerController がある");
            Assert.That(controllers.All(c => c.GetComponent<PlayerInputReader>().enabled), Is.True,
                "Actions が入っていない PlayerInputReader がある");
        }

        [UnityTest]
        public IEnumerator MatchScene_PlayerMovesWhenGivenInput()
        {
            yield return SceneManager.LoadSceneAsync("Match", LoadSceneMode.Single);
            var player = GameObject.Find("Player1").GetComponent<PlayerController>();
            player.GetComponent<PlayerInputReader>().enabled = false;
            var start = player.transform.position;

            player.SetMoveInput(new Vector2(1f, 0f));
            yield return new WaitForSeconds(0.5f);

            Assert.That(player.transform.position.x, Is.GreaterThan(start.x + 0.5f));
        }
    }
}
