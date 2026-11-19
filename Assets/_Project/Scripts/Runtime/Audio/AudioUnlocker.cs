using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// 音声を、最初のユーザー入力（キー・クリック・ゲームパッドのボタン）まで止めておく（REQ-052）。
    /// ブラウザは操作があるまで音を鳴らせないので、最初の入力をきっかけに鳴らし始める。
    /// タイトル画面を通らずに Match シーンを開いた場合も、最初の入力で鳴るようにしておく。
    /// </summary>
    public static class AudioUnlocker
    {
        public static bool Unlocked { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            Unlocked = false;
            AudioListener.pause = true;
            InputSystem.onAnyButtonPress.CallOnce(_ => Unlock());
        }

        /// <summary>テスト用：最初の入力の前（音を鳴らせない状態）に戻す。</summary>
        public static void LockForTests()
        {
            Unlocked = false;
            AudioListener.pause = true;
        }

        /// <summary>音声を鳴らせるようにする。何度呼んでもよい。</summary>
        public static void Unlock()
        {
            Unlocked = true;
            AudioListener.pause = false;
        }
    }
}
