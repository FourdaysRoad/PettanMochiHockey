using MochiHockey.Runtime;
using UnityEditor;
using UnityEngine;

namespace MochiHockey.Editor
{
    /// <summary>
    /// GameBalanceConfig.asset を決まった場所に作る（仕様書の値が入った状態で作られる）。
    /// すでにあれば何もしない。
    /// </summary>
    public static class GameBalanceConfigAsset
    {
        public const string Path = "Assets/_Project/ScriptableObjects/GameBalanceConfig.asset";

        [MenuItem("Tools/Mochi Hockey/Create Game Balance Config")]
        public static void Create()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(Path);
            if (existing != null)
            {
                Debug.Log($"[MochiHockey] {Path} already exists.");
                Selection.activeObject = existing;
                return;
            }

            var config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            AssetDatabase.CreateAsset(config, Path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = config;
            Debug.Log($"[MochiHockey] Created {Path}.");
        }
    }
}
