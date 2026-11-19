using UnityEngine;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// 球・円柱・立方体を組み合わせて、仮の見た目（杵・臼など）を実行時に作る部品。
    /// 見た目だけなので、コライダーはすぐに外す（当たり判定・物理は本体のものだけを使う）。
    /// 色はマテリアルを増やさずに MaterialPropertyBlock で付ける。
    /// </summary>
    internal static class PrimitiveArt
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");

        public static GameObject Create(PrimitiveType type, string name, Transform parent,
            Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale, Color color, float smoothness = 0.2f)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(localEulerAngles);
            go.transform.localScale = localScale;
            Tint(go.GetComponent<Renderer>(), color, smoothness);
            return go;
        }

        /// <summary>空の親（向き・位置の基準）を作る。</summary>
        public static Transform CreateEmpty(string name, Transform parent, Vector3 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go.transform;
        }

        public static void Tint(Renderer renderer, Color color, float smoothness)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            block.SetFloat(SmoothnessId, smoothness);
            renderer.SetPropertyBlock(block);
        }

        public static Color TintOf(Renderer renderer)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            return block.GetColor(BaseColorId);
        }
    }
}
