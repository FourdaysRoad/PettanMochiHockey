using UnityEngine;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// マテリアルを増やさずに、Renderer の色とつやだけを変える（氷の床・壁・餅・ご飯粒の仮の見た目）。
    /// 本番のモデル・マテリアルに差し替えたら外してよい。
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public sealed class ColorTint : MonoBehaviour
    {
        [SerializeField] private Color color = Color.white;

        [Tooltip("つや（0 でつや消し、1 で鏡のよう）")]
        [SerializeField, Range(0f, 1f)] private float smoothness = 0.5f;

        public Color Color
        {
            get => color;
            set
            {
                color = value;
                Apply();
            }
        }

        public float Smoothness
        {
            get => smoothness;
            set
            {
                smoothness = value;
                Apply();
            }
        }

        private void Awake() => Apply();

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                Apply();
            }
        }

        private void Apply()
        {
            var target = GetComponent<Renderer>();
            if (target != null)
            {
                PrimitiveArt.Tint(target, color, smoothness);
            }
        }
    }
}
