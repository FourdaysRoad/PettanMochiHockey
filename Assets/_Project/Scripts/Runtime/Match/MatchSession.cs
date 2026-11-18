using System.Collections.Generic;
using MochiHockey.Core;
using UnityEngine;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// 対戦設定で決めた参加者（番号・デバイス・操作の割り当て・チーム）を、Menu シーンから Match シーンへ引き継ぐ。
    /// シーンを切り替えても消えないオブジェクトとして 1 つだけ持つ。
    /// </summary>
    public sealed class MatchSession : MonoBehaviour
    {
        private static MatchSession s_current;

        private readonly List<JoinedPlayer> _players = new List<JoinedPlayer>();

        /// <summary>今の参加者。対戦設定を通らずに Match シーンを開いたときは null。</summary>
        public static MatchSession Current => s_current;

        public IReadOnlyList<JoinedPlayer> Players => _players;

        /// <summary>参加者を決めて保存する。前のものがあれば置き換える。</summary>
        public static MatchSession Create(IEnumerable<JoinedPlayer> players)
        {
            Clear();
            var go = new GameObject(nameof(MatchSession));
            DontDestroyOnLoad(go);
            s_current = go.AddComponent<MatchSession>();
            s_current._players.AddRange(players);
            return s_current;
        }

        /// <summary>抜けたデバイスの代わりに挿されたデバイスを、その参加者のデバイスにする（もう一度遊ぶときに使う）。</summary>
        public void ReplaceDevice(int oldDeviceId, int newDeviceId, string controlScheme)
        {
            for (var i = 0; i < _players.Count; i++)
            {
                var player = _players[i];
                if (player.DeviceId == oldDeviceId && player.ControlScheme == controlScheme)
                {
                    _players[i] = new JoinedPlayer(player.Slot, newDeviceId, controlScheme, player.Team);
                    return;
                }
            }
        }

        /// <summary>保存した参加者を捨てる（タイトルに戻るときなど）。</summary>
        public static void Clear()
        {
            if (s_current != null)
            {
                Destroy(s_current.gameObject);
                s_current = null;
            }
        }

        private void OnDestroy()
        {
            if (s_current == this)
            {
                s_current = null;
            }
        }
    }
}
