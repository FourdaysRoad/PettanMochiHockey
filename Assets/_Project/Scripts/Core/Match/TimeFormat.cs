using System;

namespace MochiHockey.Core
{
    /// <summary>時間の表示用の整形。</summary>
    public static class TimeFormat
    {
        /// <summary>
        /// 残り秒数を「分:秒」（例：179.4 → "2:59"）にする。秒の端数は切り捨て、負の値は "0:00"。
        /// </summary>
        public static string MinutesSeconds(float seconds)
        {
            var whole = seconds > 0f ? (int)MathF.Floor(seconds) : 0;
            return $"{whole / 60}:{whole % 60:00}";
        }
    }
}
