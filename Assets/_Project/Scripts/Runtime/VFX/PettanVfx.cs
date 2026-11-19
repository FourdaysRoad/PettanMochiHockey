using UnityEngine;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// Pettan!!! の演出（REQ-031）。どれかのプレイヤーが叩かれて HitStop に入ったら、
    /// 叩かれたプレイヤーの頭の上に「Pettan!!!」の吹き出しと、体の位置に衝撃波を出す。
    /// Match シーンに 1 つ置く。大きさ・時間は GameBalanceConfig の Vfx。
    /// </summary>
    public sealed class PettanVfx : MonoBehaviour
    {
        [SerializeField] private GameBalanceConfig config;

        public GameBalanceConfig Config
        {
            get => config;
            set => config = value;
        }

        /// <summary>最後に出した演出（テスト用）。</summary>
        public PettanBurst LastBurst { get; private set; }

        private void OnEnable()
        {
            PlayerController.AnyHitTaken += OnHitTaken;
        }

        private void OnDisable()
        {
            PlayerController.AnyHitTaken -= OnHitTaken;
        }

        private void OnHitTaken(PlayerController target)
        {
            var settings = config != null ? config : target.Config;
            if (settings == null)
            {
                return;
            }

            var vfx = settings.Vfx;
            var center = target.transform.position;
            LastBurst = PettanBurst.Create(center + Vector3.up * vfx.PettanBubbleHeight, center, vfx);
        }
    }
}
