using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MochiHockey.Editor
{
    /// <summary>
    /// UI Toolkit の PanelSettings を作る（16:9 の 1920×1080 を基準に、画面の大きさに合わせて拡大縮小する）。
    /// すでにあれば設定だけ合わせ直す。
    /// </summary>
    public static class UiAssets
    {
        public const string ThemePath = "Assets/_Project/UI/MochiTheme.tss";
        public const string PanelSettingsPath = "Assets/_Project/UI/MochiPanelSettings.asset";

        [MenuItem("Tools/Mochi Hockey/Create or Update UI Panel Settings")]
        public static void CreatePanelSettings()
        {
            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            if (theme == null)
            {
                Debug.LogError($"[MochiHockey] {ThemePath} is missing.");
                return;
            }

            var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            var created = settings == null;
            if (created)
            {
                settings = ScriptableObject.CreateInstance<PanelSettings>();
            }

            settings.themeStyleSheet = theme;
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1920, 1080);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 0.5f;

            if (created)
            {
                AssetDatabase.CreateAsset(settings, PanelSettingsPath);
            }
            else
            {
                EditorUtility.SetDirty(settings);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[MochiHockey] {(created ? "Created" : "Updated")} {PanelSettingsPath}.");
        }
    }
}
