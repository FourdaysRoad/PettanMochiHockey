using System.Collections.Generic;
using UnityEngine;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// ワールド空間のゲージ（uGUI）をまとめて作って動かす（REQ-053）。
    /// 始まったときにシーンにある餅とプレイヤー全員に、頭上・足元のゲージを付ける。
    /// 餅とプレイヤーが動いた後に追いかけるよう、LateUpdate で更新する。
    /// </summary>
    public sealed class WorldGaugePresenter : MonoBehaviour
    {
        [Tooltip("頭上のゲージを付ける餅。空ならシーンから探す")]
        [SerializeField] private MochiBall ball;

        private readonly Dictionary<PlayerController, PlayerGauge> _players = new Dictionary<PlayerController, PlayerGauge>();
        private readonly Dictionary<PlayerController, DizzyStars> _stars = new Dictionary<PlayerController, DizzyStars>();
        private readonly Dictionary<PlayerController, WallKickArrow> _kicks = new Dictionary<PlayerController, WallKickArrow>();

        public MochiBall Ball
        {
            get => ball;
            set => ball = value;
        }

        public BallGauge BallGauge { get; private set; }

        public PlayerGauge GaugeOf(PlayerController player) =>
            player != null && _players.TryGetValue(player, out var gauge) ? gauge : null;

        /// <summary>そのプレイヤーの壁キックの矢印と連鎖の表示。</summary>
        public WallKickArrow WallKickOf(PlayerController player) =>
            player != null && _kicks.TryGetValue(player, out var kick) ? kick : null;

        /// <summary>そのプレイヤーのピヨり（頭の上の星）。</summary>
        public DizzyStars StarsOf(PlayerController player) =>
            player != null && _stars.TryGetValue(player, out var stars) ? stars : null;

        private void Start()
        {
            if (ball == null)
            {
                ball = FindFirstObjectByType<MochiBall>(FindObjectsInactive.Include);
            }

            if (ball != null)
            {
                BallGauge = BallGauge.Create(ball);
            }

            foreach (var player in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            {
                _players[player] = PlayerGauge.Create(player);
                _stars[player] = DizzyStars.Create(player);
                _kicks[player] = WallKickArrow.Create(player);
            }
        }

        private void LateUpdate()
        {
            var viewer = Camera.main;
            if (BallGauge != null)
            {
                BallGauge.Refresh(viewer);
            }

            foreach (var gauge in _players.Values)
            {
                if (gauge != null)
                {
                    gauge.Refresh();
                }
            }

            foreach (var stars in _stars.Values)
            {
                if (stars != null)
                {
                    stars.Refresh(Time.deltaTime);
                }
            }

            foreach (var kick in _kicks.Values)
            {
                if (kick != null)
                {
                    kick.Refresh(viewer);
                }
            }
        }

        private void OnDestroy()
        {
            if (BallGauge != null)
            {
                Destroy(BallGauge.gameObject);
            }

            foreach (var gauge in _players.Values)
            {
                if (gauge != null)
                {
                    Destroy(gauge.gameObject);
                }
            }

            foreach (var stars in _stars.Values)
            {
                if (stars != null)
                {
                    Destroy(stars.gameObject);
                }
            }

            foreach (var kick in _kicks.Values)
            {
                if (kick != null)
                {
                    Destroy(kick.gameObject);
                }
            }
        }
    }
}
