using UnityEngine;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// 臼型ゴールの仮の見た目（REQ-003）。木の色の太い円柱に、上面のくぼみ（濃い色の円）を付ける。
    /// ゴールのトリガー（コライダー）はそのままで、元の見た目（MeshRenderer）だけを隠す。
    /// ゴールの大きさ（Scale）に関係なく、ここで決めた大きさ（m）で床の上に置く。
    /// マテリアルはゴールの元の見た目のものを使う（公開版でピンクにならないように）。
    /// </summary>
    [RequireComponent(typeof(GoalPost))]
    public sealed class UsuVisual : MonoBehaviour
    {
        public const string RootName = "Usu";

        [Header("形（m）")]
        [SerializeField, Min(0.01f)] private float radius = 0.9f;
        [SerializeField, Min(0.01f)] private float height = 0.8f;
        [SerializeField, Min(0.01f)] private float innerRadius = 0.7f;

        [Tooltip("上面からくぼみの底までの深さ")]
        [SerializeField, Min(0.01f)] private float innerDepth = 0.12f;

        [Tooltip("胴のくびれ（帯）の高さ。0 なら付けない")]
        [SerializeField, Min(0f)] private float bandHeight = 0.12f;

        [Header("色")]
        [SerializeField] private Color bodyColor = new Color(0.6f, 0.4f, 0.22f);
        [SerializeField] private Color bandColor = new Color(0.45f, 0.28f, 0.14f);
        [SerializeField] private Color innerColor = new Color(0.25f, 0.15f, 0.08f);

        public Transform Root { get; private set; }

        public float Height => height;

        private void Awake()
        {
            // 床の高さ：ゴールのコライダーの底
            var bottom = transform.position.y;
            var goalCollider = GetComponent<Collider>();
            if (goalCollider != null)
            {
                bottom = goalCollider.bounds.min.y;
            }

            var ownRenderer = GetComponent<MeshRenderer>();
            Material material = null;
            if (ownRenderer != null)
            {
                material = ownRenderer.sharedMaterial;
                ownRenderer.enabled = false;
            }

            if (material == null)
            {
                Debug.LogWarning($"[MochiHockey] {name}: UsuVisual に使うマテリアルがありません（ゴールの MeshRenderer がない）。", this);
            }

            // ゴールの Scale を打ち消して、子は m で組み立てる
            var root = new GameObject(RootName).transform;
            root.SetParent(transform, false);
            root.position = new Vector3(transform.position.x, bottom, transform.position.z);
            var scale = transform.lossyScale;
            root.localScale = new Vector3(Inverse(scale.x), Inverse(scale.y), Inverse(scale.z));
            Root = root;

            // Unity の円柱は高さ 2・半径 0.5
            PrimitiveArt.Create(PrimitiveType.Cylinder, "Body", root,
                new Vector3(0f, height / 2f, 0f), Vector3.zero,
                new Vector3(radius * 2f, height / 2f, radius * 2f), material, bodyColor);

            if (bandHeight > 0f)
            {
                PrimitiveArt.Create(PrimitiveType.Cylinder, "Band", root,
                    new Vector3(0f, height * 0.6f, 0f), Vector3.zero,
                    new Vector3(radius * 2.04f, bandHeight / 2f, radius * 2.04f), material, bandColor);
            }

            // くぼみ：上面の少し下に濃い色の円盤（上から見ると穴に見える）
            PrimitiveArt.Create(PrimitiveType.Cylinder, "Hollow", root,
                new Vector3(0f, height - innerDepth / 2f + 0.005f, 0f), Vector3.zero,
                new Vector3(innerRadius * 2f, innerDepth / 2f, innerRadius * 2f), material, innerColor, 0f);
        }

        private static float Inverse(float value) => Mathf.Approximately(value, 0f) ? 1f : 1f / value;
    }
}
