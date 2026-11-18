using UnityEngine;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// プレイヤーの見た目の設定：モデル、Animator Controller、プレイヤー（カプセル）に対する位置・向き・大きさ。
    /// 本番のモデルはリポジトリにこのアセットを作って Player プレハブの PlayerVisual に指定する。
    /// 手元だけで試す仮のモデル（ユニティちゃんなど）は、Git が無視するフォルダの Resources に
    /// "LocalPlayerVisual" という名前で置く（PlayerVisual が自動で読む）。
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerVisualProfile", menuName = "Mochi Hockey/Player Visual Profile")]
    public sealed class PlayerVisualProfile : ScriptableObject
    {
        [Tooltip("見た目のモデル（プレハブまたは FBX）。当たり判定はプレイヤーのカプセルを使うので、モデルのコライダーは外される")]
        public GameObject ModelPrefab;

        [Tooltip("モデルの Animator に使う Animator Controller。パラメータ名は PlayerAnimatorDriver を参照")]
        public RuntimeAnimatorController AnimatorController;

        [Tooltip("プレイヤーの中心からのモデルの位置。カプセル（高さ 2m）の中心は足元から 1m 上なので、足元に置くなら (0, -1, 0)")]
        public Vector3 LocalPosition = new Vector3(0f, -1f, 0f);

        [Tooltip("モデルの向き（度）。モデルの正面がプレイヤーの正面（+Z）を向くように合わせる")]
        public Vector3 LocalEulerAngles = Vector3.zero;

        [Tooltip("モデルの大きさ")]
        public Vector3 LocalScale = Vector3.one;
    }
}
