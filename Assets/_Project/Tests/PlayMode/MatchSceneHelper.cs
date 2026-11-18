using System.Collections;
using MochiHockey.Core;
using MochiHockey.Runtime;
using UnityEngine;

namespace MochiHockey.Tests.PlayMode
{
    /// <summary>Match シーンを使うテストの共通処理。</summary>
    public static class MatchSceneHelper
    {
        /// <summary>シーンに MatchManager があれば、キックオフのカウントダウンが終わって試合が始まるまで待つ。</summary>
        public static IEnumerator WaitUntilPlaying()
        {
            yield return null;
            var manager = Object.FindFirstObjectByType<MatchManager>();
            if (manager == null || !manager.enabled)
            {
                yield break;
            }

            var timeout = Time.time + 10f;
            while (manager.Rules.Phase == MatchPhase.Kickoff && Time.time < timeout)
            {
                yield return null;
            }
        }
    }
}
