using UnityEngine;
using UnityEngine.UI;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// 餅の頭上のゲージ（REQ-053）：レベル（Lv.1〜3）、得点倍率（x1〜x3）、次の成長までのゲージ。
    /// 餅の大きさに引きずられないよう子にはせず、位置だけを追いかけ、いつもカメラの方を向く。
    /// 餅が消えている間（ゴール後）は隠れる。WorldGaugePresenter が作り、毎フレーム Refresh を呼ぶ。
    /// </summary>
    public sealed class BallGauge : MonoBehaviour
    {
        // 餅の上端からゲージの中心までの高さ（m）
        private const float HeightAboveBall = 0.6f;

        private MochiBall _ball;
        private Collider _ballCollider;

        public Text LevelText { get; private set; }
        public Text MultiplierText { get; private set; }
        public Image GrowthFill { get; private set; }

        internal static BallGauge Create(MochiBall ball)
        {
            var canvas = WorldUi.CreateCanvas("BallGauge", new Vector2(220f, 110f), 10);
            var gauge = canvas.gameObject.AddComponent<BallGauge>();
            gauge._ball = ball;
            gauge._ballCollider = ball.GetComponent<Collider>();

            var root = canvas.transform;
            gauge.LevelText = WorldUi.CreateText(root, "Level", new Vector2(-45f, 20f), new Vector2(120f, 60f), 44, Color.white);
            gauge.MultiplierText = WorldUi.CreateText(root, "Multiplier", new Vector2(65f, 20f), new Vector2(90f, 60f), 44,
                new Color(1f, 0.84f, 0.4f));
            gauge.GrowthFill = WorldUi.CreateBar(root, "Growth", new Vector2(0f, -30f), new Vector2(200f, 22f),
                new Color(0.55f, 0.9f, 0.55f));
            gauge.Refresh(null);
            return gauge;
        }

        internal void Refresh(Camera viewer)
        {
            if (_ball == null)
            {
                Destroy(gameObject);
                return;
            }

            var visible = _ball.isActiveAndEnabled;
            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
            }

            if (!visible)
            {
                return;
            }

            LevelText.text = UiText.LevelLabel(_ball.Level);
            MultiplierText.text = UiText.MultiplierLabel(_ball.Points);
            GrowthFill.fillAmount = _ball.GrowthProgress;

            var top = _ballCollider != null ? _ballCollider.bounds.max.y : _ball.transform.position.y;
            var position = _ball.transform.position;
            transform.position = new Vector3(position.x, top + HeightAboveBall, position.z);
            if (viewer != null)
            {
                transform.rotation = viewer.transform.rotation;
            }
        }
    }
}
