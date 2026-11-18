using UnityEngine;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// プレイヤー番号ごとの色（足元のリング）。番号の文字（1P〜4P）も一緒に出すので、色だけで見分ける必要はない。
    /// 色の見分けにくさを減らすため、明るさと色合いが離れた 4 色にしている。
    /// </summary>
    public static class PlayerColors
    {
        public static readonly Color P1 = new Color(0.94f, 0.33f, 0.31f);   // 赤
        public static readonly Color P2 = new Color(0.26f, 0.55f, 0.96f);   // 青
        public static readonly Color P3 = new Color(0.36f, 0.80f, 0.40f);   // 緑
        public static readonly Color P4 = new Color(0.99f, 0.82f, 0.25f);   // 黄
        public static readonly Color Unknown = new Color(0.6f, 0.6f, 0.6f);

        public static Color For(int number)
        {
            switch (number)
            {
                case 1: return P1;
                case 2: return P2;
                case 3: return P3;
                case 4: return P4;
                default: return Unknown;
            }
        }
    }
}
