using UnityEditor;
using UnityEngine;

namespace MochiHockey.Editor
{
    /// <summary>
    /// GitHub Pages で動かすための WebGL 公開設定をまとめて適用する。
    /// Pages は "Content-Encoding: br" を返せないため、Brotli のまま
    /// Decompression Fallback（ブラウザ側の JavaScript で展開）を有効にする。
    /// </summary>
    public static class WebGLPublishSettings
    {
        [MenuItem("Tools/Mochi Hockey/Apply WebGL Settings for GitHub Pages")]
        public static void Apply()
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            AssetDatabase.SaveAssets();
            Debug.Log("[MochiHockey] WebGL settings applied: Brotli + Decompression Fallback");
        }
    }
}
