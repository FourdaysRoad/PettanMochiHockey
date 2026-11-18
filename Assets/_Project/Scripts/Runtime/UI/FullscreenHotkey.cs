using UnityEngine;
using UnityEngine.InputSystem;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// F キーでフルスクリーンを切り替える（REQ-054）。どのシーンでも効くよう、起動時に自分で作ってシーンをまたいで残す。
    /// ブラウザはユーザーの操作の中でしかフルスクリーンにできないので、キーを押したときだけ切り替える。
    /// WebGL のページ下のボタンでも全画面にできる（WebGL テンプレート）。
    /// </summary>
    public sealed class FullscreenHotkey : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var go = new GameObject(nameof(FullscreenHotkey));
            DontDestroyOnLoad(go);
            go.AddComponent<FullscreenHotkey>();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
            {
                Screen.fullScreen = !Screen.fullScreen;
            }
        }
    }
}
