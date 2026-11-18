using System.Collections.Generic;
using MochiHockey.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// Match シーンで、MatchSession の参加者の数だけプレイヤーを生成する（REQ-005, REQ-041）。
    /// 対戦設定を通らずに Match シーンを開いたとき（エディタで直接 Play など）は、キーボード 2 人（1P = A、2P = B）で生成する。
    /// 位置は自陣の 2 つの地点：2vs2 ならそれぞれに、1vs1 なら 2 地点の真ん中に置き、リンクの中央を向かせる。
    /// MatchManager がキックオフの位置を覚える前（Awake）に生成する。
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class MatchPlayerSpawner : MonoBehaviour
    {
        [SerializeField] private PlayerController playerPrefab;

        [Tooltip("チーム A（左側）のプレイヤーを置く 2 地点")]
        [SerializeField] private Transform[] teamASpots = new Transform[2];

        [Tooltip("チーム B（右側）のプレイヤーを置く 2 地点")]
        [SerializeField] private Transform[] teamBSpots = new Transform[2];

        private readonly List<PlayerController> _spawned = new List<PlayerController>();

        public PlayerController PlayerPrefab
        {
            get => playerPrefab;
            set => playerPrefab = value;
        }

        public Transform[] TeamASpots
        {
            get => teamASpots;
            set => teamASpots = value;
        }

        public Transform[] TeamBSpots
        {
            get => teamBSpots;
            set => teamBSpots = value;
        }

        /// <summary>生成したプレイヤー（番号順）。</summary>
        public IReadOnlyList<PlayerController> Spawned => _spawned;

        private void Awake()
        {
            if (playerPrefab == null || !HasTwoSpots(teamASpots) || !HasTwoSpots(teamBSpots))
            {
                Debug.LogError($"[MochiHockey] {name}: Player prefab or team spots (2 per team) are not assigned. No players are spawned.", this);
                return;
            }

            var participants = MatchSession.Current != null ? MatchSession.Current.Players : DefaultParticipants();
            var teamCounts = new Dictionary<Team, int> { [Team.A] = 0, [Team.B] = 0 };
            foreach (var participant in participants)
            {
                teamCounts[participant.Team]++;
            }

            var placed = new Dictionary<Team, int> { [Team.A] = 0, [Team.B] = 0 };
            foreach (var participant in participants)
            {
                var spots = participant.Team == Team.A ? teamASpots : teamBSpots;
                var position = teamCounts[participant.Team] == 1
                    ? (spots[0].position + spots[1].position) / 2f
                    : spots[Mathf.Min(placed[participant.Team], spots.Length - 1)].position;
                placed[participant.Team]++;

                _spawned.Add(Spawn(participant, position));
            }
        }

        private PlayerController Spawn(JoinedPlayer participant, Vector3 position)
        {
            var toCenter = new Vector3(-position.x, 0f, 0f);
            var rotation = toCenter.sqrMagnitude > 0f ? Quaternion.LookRotation(toCenter, Vector3.up) : Quaternion.identity;

            var player = Instantiate(playerPrefab, position, rotation);
            player.name = $"Player{participant.Slot + 1}";
            player.Team = participant.Team;
            player.gameObject.SetActive(true);

            var reader = player.GetComponent<PlayerInputReader>();
            if (reader != null)
            {
                reader.Configure(participant.ControlScheme, participant.DeviceId);
            }

            return player;
        }

        private static IReadOnlyList<JoinedPlayer> DefaultParticipants()
        {
            var keyboard = Keyboard.current != null ? Keyboard.current.deviceId : PlayerInputReader.AnyDevice;
            return new[]
            {
                new JoinedPlayer(0, keyboard, PlayerInputReader.KeyboardP1Scheme, Team.A),
                new JoinedPlayer(1, keyboard, PlayerInputReader.KeyboardP2Scheme, Team.B),
            };
        }

        private static bool HasTwoSpots(Transform[] spots) => spots != null && spots.Length >= 2 && spots[0] != null && spots[1] != null;
    }
}
