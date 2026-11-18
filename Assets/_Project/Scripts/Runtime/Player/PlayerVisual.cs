using UnityEngine;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// プレイヤーの見た目を差し替える。ゲームの動き・当たり判定（カプセルのコライダー）はそのままで、見た目だけを変える。
    /// - Profile が指定されていればそのモデルを使う（本番のモデル）
    /// - なければ Resources の "LocalPlayerVisual" を探す（手元だけで試す仮のモデル。リポジトリには入れない）
    /// - どちらもなければ、カプセルのまま（CI のビルド・公開版はこうなる）
    /// PlayerController が点滅などに使う見た目を集める前に差し替えるため、先に Awake する。
    /// </summary>
    [DefaultExecutionOrder(-300)]
    public sealed class PlayerVisual : MonoBehaviour
    {
        public const string LocalProfileResource = "LocalPlayerVisual";

        [Tooltip("本番の見た目。空なら Resources の LocalPlayerVisual（手元の仮のモデル）を探し、なければカプセルのまま")]
        [SerializeField] private PlayerVisualProfile profile;

        [Tooltip("Profile が空のとき、手元の仮のモデル（Resources/LocalPlayerVisual）を探すか")]
        [SerializeField] private bool useLocalProfile = true;

        public PlayerVisualProfile Profile
        {
            get => profile;
            set => profile = value;
        }

        public bool UseLocalProfile
        {
            get => useLocalProfile;
            set => useLocalProfile = value;
        }

        /// <summary>モデルに差し替わっているか（false ならカプセルのまま）。</summary>
        public bool HasModel { get; private set; }

        private void Awake()
        {
            var chosen = profile;
            if (chosen == null && useLocalProfile)
            {
                chosen = Resources.Load<PlayerVisualProfile>(LocalProfileResource);
            }

            if (chosen == null || chosen.ModelPrefab == null)
            {
                return;
            }

            var model = Instantiate(chosen.ModelPrefab, transform);
            model.name = "Visual";
            model.transform.localPosition = chosen.LocalPosition;
            model.transform.localRotation = Quaternion.Euler(chosen.LocalEulerAngles);
            model.transform.localScale = chosen.LocalScale;

            // 当たり判定・物理はプレイヤー本体のものだけを使う
            foreach (var modelCollider in model.GetComponentsInChildren<Collider>(true))
            {
                DestroyImmediate(modelCollider);
            }

            foreach (var modelBody in model.GetComponentsInChildren<Rigidbody>(true))
            {
                DestroyImmediate(modelBody);
            }

            model.SetActive(true);

            var animator = model.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                animator.applyRootMotion = false;
                if (chosen.AnimatorController != null)
                {
                    animator.runtimeAnimatorController = chosen.AnimatorController;
                }

                gameObject.AddComponent<PlayerAnimatorDriver>().Bind(animator);
            }

            // カプセルの見た目を外す（点滅などで表示し直されないよう、すぐ消す）
            var capsuleRenderer = GetComponent<MeshRenderer>();
            if (capsuleRenderer != null)
            {
                DestroyImmediate(capsuleRenderer);
            }

            var capsuleMesh = GetComponent<MeshFilter>();
            if (capsuleMesh != null)
            {
                DestroyImmediate(capsuleMesh);
            }

            HasModel = true;
        }
    }
}
