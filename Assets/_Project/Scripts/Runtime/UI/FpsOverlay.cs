using MochiHockey.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// F3 キーで、画面の左上に fps を出す（TASK-0031 の計測用。公開版でも使える）。
    /// 0.5 秒ごとの平均 fps・平均のフレーム時間・いちばん遅かったフレームを出す。最初は隠れている。
    /// どのシーンでも効くよう、起動時に自分で作ってシーンをまたいで残す。
    /// 計測の間隔はゲームの調整値ではないので、ここに置く。
    /// </summary>
    public sealed class FpsOverlay : MonoBehaviour
    {
        private const float Interval = 0.5f;

        private readonly FpsMeter _meter = new FpsMeter(Interval);
        private Canvas _canvas;
        private Text _text;

        public bool Visible => _canvas != null && _canvas.gameObject.activeSelf;

        public FpsMeter Meter => _meter;

        public string Text => _text != null ? _text.text : string.Empty;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var go = new GameObject(nameof(FpsOverlay));
            DontDestroyOnLoad(go);
            go.AddComponent<FpsOverlay>();
        }

        private void Awake()
        {
            var canvasObject = new GameObject("FpsCanvas", typeof(RectTransform));
            canvasObject.transform.SetParent(transform, false);
            _canvas = canvasObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 1000;

            var rect = WorldUi.CreateRect("Fps", canvasObject.transform, Vector2.zero, new Vector2(360f, 30f));
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(8f, -6f);
            _text = rect.gameObject.AddComponent<Text>();
            _text.font = WorldUi.Font;
            _text.fontSize = 18;
            _text.fontStyle = FontStyle.Bold;
            _text.color = Color.white;
            _text.alignment = TextAnchor.UpperLeft;
            rect.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
            _text.text = "-- fps";

            canvasObject.SetActive(false);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f3Key.wasPressedThisFrame)
            {
                Toggle();
            }

            // ポーズ中（timeScale 0）でも描画の速さを測るので、unscaled の時間を使う
            if (_meter.Tick(Time.unscaledDeltaTime) && Visible)
            {
                ShowReading();
            }
        }

        /// <summary>表示を切り替える。</summary>
        public void Toggle()
        {
            _canvas.gameObject.SetActive(!_canvas.gameObject.activeSelf);
            if (Visible && _meter.HasReading)
            {
                ShowReading();
            }
        }

        private void ShowReading()
        {
            _text.text = $"{_meter.Fps:0} fps  {_meter.AverageMilliseconds:0.0} ms  (worst {_meter.WorstMilliseconds:0.0} ms)";
        }
    }
}
