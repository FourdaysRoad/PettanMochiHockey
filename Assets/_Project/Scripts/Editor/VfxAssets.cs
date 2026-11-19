using UnityEditor;
using UnityEngine;

namespace MochiHockey.Editor
{
    /// <summary>
    /// 演出（TASK-0028）のマテリアルを作る。すでにあれば設定だけ合わせ直す。
    /// - IceSpray：氷しぶきの粒（URP の Particles/Unlit。不透明の白い四角い粒＝氷のかけら）
    /// </summary>
    public static class VfxAssets
    {
        public const string IceSprayPath = "Assets/_Project/VFX/IceSpray.mat";
        private const string ParticleShader = "Universal Render Pipeline/Particles/Unlit";

        [MenuItem("Tools/Mochi Hockey/Create VFX Materials")]
        public static void Create()
        {
            var shader = Shader.Find(ParticleShader);
            if (shader == null)
            {
                Debug.LogError($"[MochiHockey] Shader '{ParticleShader}' is missing.");
                return;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(IceSprayPath);
            var created = material == null;
            if (created)
            {
                material = new Material(shader);
            }
            else
            {
                material.shader = shader;
            }

            material.SetColor("_BaseColor", Color.white);
            material.enableInstancing = true;

            if (created)
            {
                AssetDatabase.CreateAsset(material, IceSprayPath);
            }
            else
            {
                EditorUtility.SetDirty(material);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[MochiHockey] {(created ? "Created" : "Updated")} {IceSprayPath}.");
        }
    }
}
