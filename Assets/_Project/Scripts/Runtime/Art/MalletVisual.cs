using MochiHockey.Core;
using UnityEngine;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// 杵の仮の見た目（REQ-025 の見た目）。円柱の柄と頭を組み合わせて体の右前に構え、
    /// 杵を振ると（PlayerController.Swung）右から左へ水平に振り抜いて構えに戻る。
    /// 振りの角度・時間は GameBalanceConfig（Mallet、Player.SwingActiveTime）。形と色は見た目のデータなのでここに置く。
    /// PlayerController が点滅に使う見た目を集める前に作るため、先に Awake する。
    /// </summary>
    [DefaultExecutionOrder(-250)]
    [RequireComponent(typeof(PlayerController))]
    public sealed class MalletVisual : MonoBehaviour
    {
        public const string RootName = "Mallet";

        [Header("形（m）")]
        [Tooltip("体の中心から見た、振りの回転の中心")]
        [SerializeField] private Vector3 pivotOffset = new Vector3(0f, 0.1f, 0f);

        [Tooltip("回転の中心から柄の根元までの距離（体の中に隠れる分）")]
        [SerializeField, Min(0f)] private float handleStart = 0.3f;

        [SerializeField, Min(0.01f)] private float handleLength = 0.9f;
        [SerializeField, Min(0.01f)] private float handleRadius = 0.05f;
        [SerializeField, Min(0.01f)] private float headLength = 0.55f;
        [SerializeField, Min(0.01f)] private float headRadius = 0.22f;

        [Header("色")]
        [SerializeField] private Color handleColor = new Color(0.85f, 0.68f, 0.45f);
        [SerializeField] private Color headColor = new Color(0.7f, 0.5f, 0.3f);

        private PlayerController _player;
        private MalletSwingPose _pose;
        private float _elapsed = float.PositiveInfinity;

        /// <summary>振りで回る部分（柄と頭の親）。</summary>
        public Transform Pivot { get; private set; }

        /// <summary>杵の頭（当たり判定の位置の目安）。</summary>
        public Transform Head { get; private set; }

        public bool IsSwinging => _pose.IsSwinging(_elapsed);

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
            var root = PrimitiveArt.CreateEmpty(RootName, transform, pivotOffset);
            Pivot = root;

            PrimitiveArt.Create(PrimitiveType.Cylinder, "Handle", root,
                new Vector3(0f, 0f, handleStart + handleLength / 2f), new Vector3(90f, 0f, 0f),
                new Vector3(handleRadius * 2f, handleLength / 2f, handleRadius * 2f), handleColor);

            Head = PrimitiveArt.Create(PrimitiveType.Cylinder, "Head", root,
                new Vector3(0f, 0f, handleStart + handleLength), new Vector3(0f, 0f, 90f),
                new Vector3(headRadius * 2f, headLength / 2f, headRadius * 2f), headColor).transform;

            ApplyPose();
        }

        private void Start()
        {
            _player.Swung += OnSwung;
        }

        private void OnDestroy()
        {
            if (_player != null)
            {
                _player.Swung -= OnSwung;
            }
        }

        private void Update()
        {
            // 当てた瞬間のヒットストップ中は、杵も振り抜く途中で止める
            if (IsSwinging && !_player.IsInHitStop)
            {
                _elapsed += Time.deltaTime;
            }

            ApplyPose();
        }

        private void OnSwung()
        {
            _elapsed = 0f;
            ApplyPose();
        }

        private void ApplyPose()
        {
            var config = _player != null ? _player.Config : null;
            if (config != null)
            {
                _pose = new MalletSwingPose(config.Mallet.RestYaw, config.Mallet.SwingArcAngle,
                    config.Player.SwingActiveTime, config.Mallet.SwingReturnTime);
            }

            Pivot.localRotation = Quaternion.Euler(0f, _pose.YawAt(_elapsed), 0f);
        }
    }
}
