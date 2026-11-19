using MochiHockey.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// 壁キックの表示（REQ-028, REQ-029）。
    /// - 狙っている間：足元から狙っている向きへ伸びる矢印（床に寝かせて置く）
    /// - 2 回以上続けて蹴っている間：頭の上に「2 CHAIN!」のような連鎖の数
    /// 子にはせず位置だけを追う。WorldGaugePresenter が作り、毎フレーム Refresh を呼ぶ。
    /// </summary>
    public sealed class WallKickArrow : MonoBehaviour
    {
        // 矢印の長さ・幅（1 単位 = 1 cm）と、床・頭の上からの高さ（m）
        private const float ArrowLength = 260f;
        private const float ArrowWidth = 120f;
        private const float HeightAboveFloor = 0.08f;
        private const float ChainHeightAboveHead = 0.6f;

        private static readonly Color ArrowColor = new Color(1f, 0.85f, 0.3f, 0.95f);
        private static readonly Color ChainColor = new Color(1f, 0.6f, 0.2f);

        private PlayerController _player;
        private Collider _playerCollider;
        private Transform _arrow;
        private Transform _chain;
        private Text _chainText;

        public bool ArrowVisible => _arrow.gameObject.activeSelf;

        public bool ChainVisible => _chain.gameObject.activeSelf;

        public string ChainText => _chainText.text;

        /// <summary>矢印の向き（床面。x, z）。</summary>
        public Vector2 ArrowDirection
        {
            get
            {
                var forward = _arrow.up; // 寝かせた Canvas の上方向が、矢印の先
                return new Vector2(forward.x, forward.z).normalized;
            }
        }

        internal static WallKickArrow Create(PlayerController player)
        {
            var root = new GameObject($"{player.name}WallKick");
            var view = root.AddComponent<WallKickArrow>();
            view._player = player;
            view._playerCollider = player.GetComponent<Collider>();

            // 矢印：足元を根元にして、先へ伸びる（Pivot を下端にする）
            var arrowCanvas = WorldUi.CreateCanvas("Arrow", new Vector2(ArrowWidth, ArrowLength), 6);
            arrowCanvas.transform.SetParent(root.transform, false);
            var arrowRect = (RectTransform)arrowCanvas.transform;
            arrowRect.pivot = new Vector2(0.5f, 0f);
            WorldUi.CreateArrow(arrowCanvas.transform, "Shape", new Vector2(ArrowWidth, ArrowLength), ArrowColor);
            view._arrow = arrowCanvas.transform;

            var chainCanvas = WorldUi.CreateCanvas("Chain", new Vector2(300f, 60f), 16);
            chainCanvas.transform.SetParent(root.transform, false);
            view._chainText = WorldUi.CreateText(chainCanvas.transform, "Text", Vector2.zero, new Vector2(300f, 60f), 40, ChainColor);
            view._chain = chainCanvas.transform;

            arrowCanvas.gameObject.SetActive(false);
            chainCanvas.gameObject.SetActive(false);
            return view;
        }

        internal void Refresh(Camera viewer)
        {
            if (_player == null)
            {
                Destroy(gameObject);
                return;
            }

            var phase = _player.WallKickPhase;
            var position = _player.transform.position;

            var aiming = phase == WallKickPhase.Aiming;
            if (_arrow.gameObject.activeSelf != aiming)
            {
                _arrow.gameObject.SetActive(aiming);
            }

            if (aiming)
            {
                var floor = _playerCollider != null && _playerCollider.enabled ? _playerCollider.bounds.min.y : position.y - 1f;
                var aim = _player.WallKickAim;
                var yaw = Mathf.Atan2(aim.x, aim.y) * Mathf.Rad2Deg;
                _arrow.SetPositionAndRotation(
                    new Vector3(position.x, Mathf.Max(0f, floor) + HeightAboveFloor, position.z),
                    Quaternion.Euler(90f, yaw, 0f));
            }

            var chain = _player.WallKickChain;
            var showChain = chain >= 2 && phase != WallKickPhase.None;
            if (_chain.gameObject.activeSelf != showChain)
            {
                _chain.gameObject.SetActive(showChain);
            }

            if (showChain)
            {
                _chainText.text = UiText.ChainLabel(chain);
                var top = _playerCollider != null && _playerCollider.enabled ? _playerCollider.bounds.max.y : position.y + 1f;
                _chain.position = new Vector3(position.x, top + ChainHeightAboveHead, position.z);
                if (viewer != null)
                {
                    _chain.rotation = viewer.transform.rotation;
                }
            }
        }
    }
}
