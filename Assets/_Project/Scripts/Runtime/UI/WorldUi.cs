using UnityEngine;
using UnityEngine.UI;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// ワールド空間のゲージ（uGUI）を実行時に組み立てる部品。プレハブを作らずに済むよう、ここでまとめて作る。
    /// 1 単位 = 1 cm（Canvas を 0.01 倍にする）。
    /// </summary>
    internal static class WorldUi
    {
        public const float UnitsPerMeter = 100f;

        private static Sprite s_white;
        private static Sprite s_ring;
        private static Font s_font;

        public static Font Font => s_font != null ? s_font : s_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static Canvas CreateCanvas(string name, Vector2 sizeInUnits, int sortingOrder)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = sortingOrder;
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = sizeInUnits;
            rect.localScale = Vector3.one / UnitsPerMeter;
            return canvas;
        }

        public static RectTransform CreateRect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public static Text CreateText(Transform parent, string name, Vector2 position, Vector2 size, int fontSize, Color color)
        {
            var rect = CreateRect(name, parent, position, size);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Font;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            rect.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
            return text;
        }

        /// <summary>背景と、左から伸びる塗りつぶしのバー。塗りつぶしの Image を返す。</summary>
        public static Image CreateBar(Transform parent, string name, Vector2 position, Vector2 size, Color fill)
        {
            var background = CreateRect(name, parent, position, size).gameObject.AddComponent<Image>();
            background.sprite = White;
            background.color = new Color(0f, 0f, 0f, 0.6f);

            var fillRect = CreateRect("Fill", background.transform, Vector2.zero, size - new Vector2(4f, 4f));
            var image = fillRect.gameObject.AddComponent<Image>();
            image.sprite = White;
            image.color = fill;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
            image.fillAmount = 1f;
            return image;
        }

        public static Image CreateRing(Transform parent, string name, Vector2 size)
        {
            var image = CreateRect(name, parent, Vector2.zero, size).gameObject.AddComponent<Image>();
            image.sprite = Ring;
            return image;
        }

        private static Sprite White
        {
            get
            {
                if (s_white == null)
                {
                    var texture = Texture2D.whiteTexture;
                    s_white = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                }

                return s_white;
            }
        }

        /// <summary>白い輪の画像（色は Image.color で付ける）。</summary>
        private static Sprite Ring
        {
            get
            {
                if (s_ring != null)
                {
                    return s_ring;
                }

                const int size = 128;
                const float outer = size / 2f - 1f;
                const float inner = outer * 0.78f;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                var center = new Vector2(size / 2f - 0.5f, size / 2f - 0.5f);
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        var d = Vector2.Distance(new Vector2(x, y), center);
                        // 縁を 1 ピクセルぼかしてギザギザを抑える
                        var alpha = Mathf.Clamp01(outer - d + 0.5f) * Mathf.Clamp01(d - inner + 0.5f);
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }

                texture.Apply();
                s_ring = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
                return s_ring;
            }
        }
    }
}
