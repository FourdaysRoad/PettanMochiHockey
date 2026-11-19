using UnityEngine;
using UnityEngine.UI;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// 被弾した瞬間の演出 1 回分（REQ-031 の HitStop）：「Pettan!!!」のトゲトゲ吹き出しと、広がる衝撃波の輪。
    /// どちらもカメラの方を向くワールド空間の uGUI（Particle System・Compute Shader は使わない）。
    /// 時間がたつと自分で消える。PettanVfx が作る。
    /// </summary>
    public sealed class PettanBurst : MonoBehaviour
    {
        // 吹き出しが「ポン」と出る動き（秒）と、最後に消えていく割合
        private const float PopTime = 0.08f;
        private const float SettleTime = 0.16f;
        private const float PopOvershoot = 1.25f;
        private const float FadeFrom = 0.7f;

        // 1 単位 = 1 cm
        private const float BubblePixels = 100f;
        private const float WavePixels = 100f;

        private static readonly Color Outline = new Color(0.85f, 0.15f, 0.1f);
        private static readonly Color Fill = new Color(1f, 0.9f, 0.2f);
        private static readonly Color TextColor = new Color(0.55f, 0.05f, 0.05f);
        private static readonly Color WaveColor = new Color(1f, 1f, 1f, 0.9f);

        private Transform _bubble;
        private CanvasGroup _bubbleGroup;
        private Transform _wave;
        private Image _waveImage;
        private float _bubbleSize;
        private float _bubbleDuration;
        private float _waveSize;
        private float _waveDuration;

        public float Elapsed { get; private set; }

        public float Duration => Mathf.Max(_bubbleDuration, _waveDuration);

        /// <summary>吹き出しの今の大きさ（m）。</summary>
        public float BubbleScale => _bubble.localScale.x * BubblePixels;

        /// <summary>衝撃波の今の直径（m）。</summary>
        public float WaveDiameter => _wave.localScale.x * WavePixels;

        public float BubbleAlpha => _bubbleGroup.alpha;

        public string Text { get; private set; }

        internal static PettanBurst Create(Vector3 bubblePosition, Vector3 wavePosition, GameBalanceConfig.VfxConfig config)
        {
            var root = new GameObject("PettanBurst");
            var burst = root.AddComponent<PettanBurst>();
            burst._bubbleSize = config.PettanBubbleSize;
            burst._bubbleDuration = config.PettanBubbleDuration;
            burst._waveSize = config.ShockwaveSize;
            burst._waveDuration = config.ShockwaveDuration;
            burst.Text = UiText.Pettan;

            // 衝撃波：輪を作り、大きさを変えて広げる
            var wave = WorldUi.CreateCanvas("Shockwave", new Vector2(WavePixels, WavePixels), 18);
            wave.transform.SetParent(root.transform, false);
            wave.transform.position = wavePosition;
            burst._wave = wave.transform;
            burst._waveImage = WorldUi.CreateRing(wave.transform, "Ring", new Vector2(WavePixels, WavePixels), WaveColor);

            // 吹き出し：赤い縁取り → 黄色 → 文字
            var bubble = WorldUi.CreateCanvas("Bubble", new Vector2(BubblePixels, BubblePixels), 20);
            bubble.transform.SetParent(root.transform, false);
            bubble.transform.position = bubblePosition;
            burst._bubble = bubble.transform;
            burst._bubbleGroup = bubble.gameObject.AddComponent<CanvasGroup>();
            WorldUi.CreateBurst(bubble.transform, "Outline", new Vector2(BubblePixels, BubblePixels), Outline);
            WorldUi.CreateBurst(bubble.transform, "Fill", new Vector2(BubblePixels * 0.88f, BubblePixels * 0.88f), Fill);
            var label = WorldUi.CreateText(bubble.transform, "Text", Vector2.zero, new Vector2(BubblePixels, 30f), 18, TextColor);
            label.text = burst.Text;
            label.transform.localRotation = Quaternion.Euler(0f, 0f, 8f);

            burst.Apply(Camera.main);
            return burst;
        }

        private void LateUpdate()
        {
            Elapsed += Time.deltaTime;
            if (Elapsed >= Duration)
            {
                Destroy(gameObject);
                return;
            }

            Apply(Camera.main);
        }

        private void Apply(Camera viewer)
        {
            // 吹き出し：0 → 少し大きく → 元の大きさ、最後の 3 割で消える
            float pop;
            if (Elapsed < PopTime)
            {
                pop = PopOvershoot * (Elapsed / PopTime);
            }
            else if (Elapsed < SettleTime)
            {
                pop = Mathf.Lerp(PopOvershoot, 1f, (Elapsed - PopTime) / (SettleTime - PopTime));
            }
            else
            {
                pop = 1f;
            }

            var bubbleT = Elapsed / _bubbleDuration;
            _bubble.gameObject.SetActive(bubbleT < 1f);
            _bubble.localScale = Vector3.one * (pop * _bubbleSize / BubblePixels);
            _bubbleGroup.alpha = bubbleT < FadeFrom ? 1f : Mathf.Clamp01((1f - bubbleT) / (1f - FadeFrom));

            // 衝撃波：すぐ大きく広がって、広がるほど薄くなる
            var waveT = Mathf.Clamp01(Elapsed / _waveDuration);
            var eased = 1f - (1f - waveT) * (1f - waveT);
            _wave.gameObject.SetActive(waveT < 1f);
            _wave.localScale = Vector3.one * (Mathf.Max(0.01f, eased) * _waveSize / WavePixels);
            var color = WaveColor;
            color.a *= 1f - waveT;
            _waveImage.color = color;

            if (viewer != null)
            {
                var facing = viewer.transform.rotation;
                _bubble.rotation = facing;
                _wave.rotation = facing;
            }
        }
    }
}
