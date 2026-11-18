using MochiHockey.Core;
using UnityEngine;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// ピヨり（めまい）の演出：壁から復帰した直後の無敵中だけ、頭の上で黄色い星が 3 つ回る（TASK-0027 で決定）。
    /// 子にはせず、プレイヤーの頭の上の位置だけを追う。WorldGaugePresenter が作り、毎フレーム Refresh を呼ぶ。
    /// </summary>
    public sealed class DizzyStars : MonoBehaviour
    {
        // 頭のてっぺんから星の輪までの高さ（m）
        private const float HeightAboveHead = 0.3f;
        private const int Stars = 3;

        private static readonly Color StarColor = new Color(1f, 0.88f, 0.25f);

        private PlayerController _player;
        private Collider _playerCollider;
        private float _spinSpeed;
        private float _angle;

        public int StarCount => Stars;

        internal static DizzyStars Create(PlayerController player)
        {
            var pivot = new GameObject($"{player.name}DizzyStars");
            var stars = pivot.AddComponent<DizzyStars>();
            stars._player = player;
            stars._playerCollider = player.GetComponent<Collider>();
            stars._spinSpeed = player.Config != null ? player.Config.Pettan.DizzySpinSpeed : 0f;

            // 星の輪を床と平行に寝かせ、輪ごと Y 軸で回す
            var canvas = WorldUi.CreateCanvas("Ring", new Vector2(140f, 140f), 15);
            canvas.transform.SetParent(pivot.transform, false);
            canvas.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            for (var i = 0; i < Stars; i++)
            {
                var angle = i * Mathf.PI * 2f / Stars;
                var position = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 50f;
                WorldUi.CreateStar(canvas.transform, $"Star{i}", position, 32f, StarColor);
            }

            pivot.SetActive(false);
            return stars;
        }

        internal void Refresh(float deltaTime)
        {
            if (_player == null)
            {
                Destroy(gameObject);
                return;
            }

            var dizzy = _player.PettanState == PettanState.Invincible;
            if (gameObject.activeSelf != dizzy)
            {
                gameObject.SetActive(dizzy);
            }

            if (!dizzy)
            {
                return;
            }

            _angle = Mathf.Repeat(_angle + _spinSpeed * deltaTime, 360f);
            var position = _player.transform.position;
            var top = _playerCollider != null && _playerCollider.enabled ? _playerCollider.bounds.max.y : position.y + 1f;
            transform.SetPositionAndRotation(new Vector3(position.x, top + HeightAboveHead, position.z),
                Quaternion.Euler(0f, _angle, 0f));
        }
    }
}
