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
        private static Sprite s_star;
        private static Sprite s_burst;
        private static Sprite s_arrow;
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

        public static Image CreateStar(Transform parent, string name, Vector2 position, float size, Color color)
        {
            var image = CreateRect(name, parent, position, new Vector2(size, size)).gameObject.AddComponent<Image>();
            image.sprite = Star;
            image.color = color;
            return image;
        }

        /// <summary>まんがの「ドカン」のようなトゲトゲの吹き出し（色は color で付ける）。</summary>
        public static Image CreateBurst(Transform parent, string name, Vector2 size, Color color)
        {
            var image = CreateRect(name, parent, Vector2.zero, size).gameObject.AddComponent<Image>();
            image.sprite = Burst;
            image.color = color;
            return image;
        }

        /// <summary>白い輪（衝撃波など）。</summary>
        public static Image CreateRing(Transform parent, string name, Vector2 size, Color color)
        {
            var image = CreateRing(parent, name, size);
            image.color = color;
            return image;
        }

        /// <summary>上を向いた矢印（壁キックの狙い）。色は color で付ける。</summary>
        public static Image CreateArrow(Transform parent, string name, Vector2 size, Color color)
        {
            var image = CreateRect(name, parent, Vector2.zero, size).gameObject.AddComponent<Image>();
            image.sprite = Arrow;
            image.color = color;
            return image;
        }

        /// <summary>白い矢印の画像（上向き。下 6 割が軸、上 4 割が矢じり）。</summary>
        private static Sprite Arrow
        {
            get
            {
                if (s_arrow != null)
                {
                    return s_arrow;
                }

                const int width = 64;
                const int height = 128;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                var points = new[]
                {
                    new Vector2(width * 0.35f, 0f),
                    new Vector2(width * 0.65f, 0f),
                    new Vector2(width * 0.65f, height * 0.6f),
                    new Vector2(width - 1f, height * 0.6f),
                    new Vector2(width / 2f, height - 1f),
                    new Vector2(0f, height * 0.6f),
                    new Vector2(width * 0.35f, height * 0.6f),
                };
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var inside = IsInside(new Vector2(x + 0.5f, y + 0.5f), points);
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, inside ? 1f : 0f));
                    }
                }

                texture.Apply();
                s_arrow = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
                return s_arrow;
            }
        }

        /// <summary>白いトゲトゲの吹き出しの画像。</summary>
        private static Sprite Burst
        {
            get
            {
                if (s_burst != null)
                {
                    return s_burst;
                }

                const int size = 128;
                const int spikes = 14;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                var center = new Vector2(size / 2f - 0.5f, size / 2f - 0.5f);
                var outer = size / 2f - 1f;
                var inner = outer * 0.72f;
                var points = new Vector2[spikes * 2];
                for (var i = 0; i < points.Length; i++)
                {
                    var angle = i * Mathf.PI / spikes;
                    // トゲの長さを少しずつ変えて、手描きらしくする
                    var radius = i % 2 == 0 ? outer * (i % 4 == 0 ? 1f : 0.88f) : inner;
                    points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * 0.8f) * radius;
                }

                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        var inside = IsInside(new Vector2(x, y), points);
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, inside ? 1f : 0f));
                    }
                }

                texture.Apply();
                s_burst = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
                return s_burst;
            }
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

        /// <summary>白い 5 つ角の星の画像（色は Image.color で付ける）。</summary>
        private static Sprite Star
        {
            get
            {
                if (s_star != null)
                {
                    return s_star;
                }

                const int size = 64;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                var center = new Vector2(size / 2f - 0.5f, size / 2f - 0.5f);
                var outer = size / 2f - 1f;
                var inner = outer * 0.45f;
                var points = new Vector2[10];
                for (var i = 0; i < points.Length; i++)
                {
                    var angle = Mathf.PI / 2f + i * Mathf.PI / 5f;
                    var radius = i % 2 == 0 ? outer : inner;
                    points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                }

                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        var inside = IsInside(new Vector2(x, y), points);
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, inside ? 1f : 0f));
                    }
                }

                texture.Apply();
                s_star = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
                return s_star;
            }
        }

        /// <summary>点が多角形の内側か（偶奇判定）。</summary>
        private static bool IsInside(Vector2 point, Vector2[] polygon)
        {
            var inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                var a = polygon[i];
                var b = polygon[j];
                if ((a.y > point.y) != (b.y > point.y)
                    && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                {
                    inside = !inside;
                }
            }

            return inside;
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
