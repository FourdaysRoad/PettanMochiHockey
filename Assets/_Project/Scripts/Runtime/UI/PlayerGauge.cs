using UnityEngine;
using UnityEngine.UI;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// プレイヤーの足元のゲージ（REQ-053）：番号の色のリングと番号（1P〜4P）、スタミナ、チャージ（チャージ中だけ）。
    /// 床に寝かせて置き、上から見て読めるようにする。体が壁に貼り付いてつぶれても崩れないよう、子にはせず位置だけを追う。
    /// WorldGaugePresenter が作り、毎フレーム Refresh を呼ぶ。
    /// </summary>
    public sealed class PlayerGauge : MonoBehaviour
    {
        // 床からの高さ（m）。床に埋まらず、影とも重ならない程度
        private const float HeightAboveFloor = 0.05f;

        private PlayerController _player;
        private Collider _playerCollider;

        public Image Ring { get; private set; }
        public Text NumberText { get; private set; }
        public Image StaminaFill { get; private set; }
        public Image ChargeFill { get; private set; }
        public GameObject ChargeBar { get; private set; }

        internal static PlayerGauge Create(PlayerController player)
        {
            var canvas = WorldUi.CreateCanvas($"{player.name}Gauge", new Vector2(240f, 300f), 5);
            canvas.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var gauge = canvas.gameObject.AddComponent<PlayerGauge>();
            gauge._player = player;
            gauge._playerCollider = player.GetComponent<Collider>();

            var root = canvas.transform;
            gauge.Ring = WorldUi.CreateRing(root, "Ring", new Vector2(160f, 160f));
            gauge.NumberText = WorldUi.CreateText(root, "Number", new Vector2(0f, -110f), new Vector2(120f, 50f), 40, Color.white);
            gauge.StaminaFill = WorldUi.CreateBar(root, "Stamina", new Vector2(0f, -140f), new Vector2(140f, 14f),
                new Color(0.45f, 0.85f, 1f));
            gauge.ChargeFill = WorldUi.CreateBar(root, "Charge", new Vector2(0f, 100f), new Vector2(140f, 18f),
                new Color(1f, 0.6f, 0.2f));
            gauge.ChargeBar = gauge.ChargeFill.transform.parent.gameObject;
            gauge.Refresh();
            return gauge;
        }

        internal void Refresh()
        {
            if (_player == null)
            {
                Destroy(gameObject);
                return;
            }

            Ring.color = PlayerColors.For(_player.Number);
            NumberText.text = UiText.PlayerLabel(_player.Number);
            NumberText.color = PlayerColors.For(_player.Number);
            StaminaFill.fillAmount = _player.StaminaNormalized;

            var charging = _player.ShotChargeNormalized > 0f;
            if (ChargeBar.activeSelf != charging)
            {
                ChargeBar.SetActive(charging);
            }

            ChargeFill.fillAmount = _player.ShotChargeNormalized;

            var position = _player.transform.position;
            var floor = _playerCollider != null && _playerCollider.enabled
                ? _playerCollider.bounds.min.y
                : position.y - 1f;
            transform.position = new Vector3(position.x, Mathf.Max(0f, floor) + HeightAboveFloor, position.z);
        }
    }
}
