using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// 試合のカメラ（REQ-054）。全プレイヤーと餅がいつも画面に収まる、斜め見下ろしのカメラ。
    /// Main Camera に付けておくと、始まったときに Cinemachine の部品（Brain・Target Group・Camera）を組み立てる。
    /// - 角度は Pitch で固定し、Group Framing が前後（ドリー）だけを動かして全員を収める
    /// - 近づく・離れる距離は MinDistance〜MaxDistance の間に限る
    /// - 後から生成されたプレイヤーも映す対象に加え、ゴール後に消えている餅は対象から外す
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(100)]
    public sealed class CameraRig : MonoBehaviour
    {
        [SerializeField] private GameBalanceConfig config;

        private readonly HashSet<PlayerController> _players = new HashSet<PlayerController>();
        private MochiBall _ball;

        public GameBalanceConfig Config
        {
            get => config;
            set => config = value;
        }

        /// <summary>映す対象（プレイヤー全員と餅）。</summary>
        public CinemachineTargetGroup TargetGroup { get; private set; }

        public CinemachineCamera VirtualCamera { get; private set; }

        public bool IsFraming(Transform target) => TargetGroup != null && TargetGroup.FindMember(target) >= 0;

        private void Start()
        {
            if (config == null)
            {
                Debug.LogError($"[MochiHockey] {name}: GameBalanceConfig is not assigned. CameraRig is disabled.", this);
                enabled = false;
                return;
            }

            var settings = config.Camera;
            if (!TryGetComponent<CinemachineBrain>(out _))
            {
                gameObject.AddComponent<CinemachineBrain>();
            }

            TargetGroup = new GameObject("CameraTargets").AddComponent<CinemachineTargetGroup>();

            var baseDistance = (settings.MinDistance + settings.MaxDistance) / 2f;
            var vcamObject = new GameObject("MatchCamera");
            vcamObject.transform.SetPositionAndRotation(transform.position, Quaternion.Euler(settings.Pitch, 0f, 0f));
            VirtualCamera = vcamObject.AddComponent<CinemachineCamera>();
            VirtualCamera.Follow = TargetGroup.transform;
            VirtualCamera.Lens.FieldOfView = GetComponent<Camera>().fieldOfView;

            var composer = vcamObject.AddComponent<CinemachinePositionComposer>();
            composer.CameraDistance = baseDistance;
            composer.Damping = Vector3.one * settings.Damping;

            var framing = vcamObject.AddComponent<CinemachineGroupFraming>();
            framing.FramingMode = CinemachineGroupFraming.FramingModes.HorizontalAndVertical;
            framing.FramingSize = settings.FramingSize;
            framing.SizeAdjustment = CinemachineGroupFraming.SizeAdjustmentModes.DollyOnly;
            // マイナスが近づく方向。元の距離から、最小距離まで近づき、最大距離まで離れられる
            framing.DollyRange = new Vector2(settings.MinDistance - baseDistance, settings.MaxDistance - baseDistance);
            framing.Damping = settings.Damping;

            _ball = FindFirstObjectByType<MochiBall>(FindObjectsInactive.Include);
            SyncTargets();
        }

        private void Update()
        {
            if (TargetGroup != null)
            {
                SyncTargets();
            }
        }

        private void OnDestroy()
        {
            if (TargetGroup != null)
            {
                Destroy(TargetGroup.gameObject);
            }

            if (VirtualCamera != null)
            {
                Destroy(VirtualCamera.gameObject);
            }
        }

        /// <summary>映す対象を、今いるプレイヤーと（出ていれば）餅に合わせる。</summary>
        private void SyncTargets()
        {
            foreach (var player in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            {
                if (_players.Add(player))
                {
                    TargetGroup.AddMember(player.transform, 1f, config.Camera.PlayerRadius);
                }
            }

            _players.RemoveWhere(player =>
            {
                if (player != null)
                {
                    return false;
                }

                TargetGroup.Targets.RemoveAll(t => t.Object == null);
                return true;
            });

            if (_ball == null)
            {
                return;
            }

            var shown = _ball.isActiveAndEnabled;
            var framed = IsFraming(_ball.transform);
            if (shown && !framed)
            {
                TargetGroup.AddMember(_ball.transform, 1f, BallRadius());
            }
            else if (!shown && framed)
            {
                TargetGroup.RemoveMember(_ball.transform);
            }
            else if (shown)
            {
                // 餅が育つと大きくなるので、半径を合わせ直す
                TargetGroup.Targets[TargetGroup.FindMember(_ball.transform)].Radius = BallRadius();
            }
        }

        private float BallRadius()
        {
            var ballCollider = _ball.GetComponent<Collider>();
            return ballCollider != null ? ballCollider.bounds.extents.magnitude : 0.5f;
        }
    }
}
