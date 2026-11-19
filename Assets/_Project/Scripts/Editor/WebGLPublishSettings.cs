using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace MochiHockey.Editor
{
    /// <summary>
    /// GitHub Pages で動かすための WebGL 公開設定をまとめて適用する。
    /// Pages は "Content-Encoding: br" を返せないため、Brotli のまま
    /// Decompression Fallback（ブラウザ側の JavaScript で展開）を有効にする。
    /// ブラウザのウィンドウに合わせて 16:9 で表示する WebGL テンプレート（Assets/WebGLTemplates/MochiHockey）を使う。
    /// ダウンロード量を減らすため、使わないコードを強めに削り（Managed Stripping: High）、
    /// IL2CPP のコード生成をサイズ優先にする（TASK-0031）。このプロジェクトのコードは link.xml で守る。
    /// </summary>
    public static class WebGLPublishSettings
    {
        [MenuItem("Tools/Mochi Hockey/Apply WebGL Settings for GitHub Pages")]
        public static void Apply()
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.template = "PROJECT:MochiHockey";
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, Il2CppCodeGeneration.OptimizeSize);
            AssetDatabase.SaveAssets();
            Debug.Log("[MochiHockey] WebGL settings applied: Brotli + Decompression Fallback + MochiHockey template"
                      + " + Managed Stripping High + IL2CPP OptimizeSize");
        }
    }
}
