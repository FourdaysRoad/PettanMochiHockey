namespace MochiHockey.Core
{
    /// <summary>壁キック（REQ-028）と連鎖（REQ-029）の調整値。値は GameBalanceConfig から渡す。</summary>
    public sealed class WallKickSettings
    {
        /// <summary>壁キックできる、プレイヤーの中心から壁までの距離（m）。</summary>
        public float Range;

        /// <summary>ダッシュボタンをこの秒数押し続けたら壁キックの狙いに入る（短ければふつうのダッシュ）。</summary>
        public float HoldTime;

        /// <summary>狙っていられる最長の秒数。過ぎたら今の向きへ自動で飛ぶ。</summary>
        public float AimMaxTime;

        /// <summary>
        /// 狙える向きの限度。壁の法線との内積がこれ以上（0 なら壁と平行まで、1 なら正面だけ）。
        /// 飛んでいるときも、壁へ向かう向きの内積がこれ以上のときだけ、その壁に張り付く。
        /// </summary>
        public float MinAwayDot;

        /// <summary>1 回目の壁キックの速さ（m/s）。</summary>
        public float BaseSpeed;

        /// <summary>連鎖 1 回ごとに足す速さの割合（0.25 なら 1.25 倍、1.5 倍…）。</summary>
        public float ChainSpeedBonus;

        /// <summary>速さの上限（m/s）。</summary>
        public float MaxSpeed;

        /// <summary>壁に着いてから、次の壁キックを受け付ける秒数。過ぎたら連鎖が終わる。</summary>
        public float ChainWindow;

        /// <summary>飛んでいられる最長の秒数（壁に着かなかったときの保険）。</summary>
        public float MaxFlightTime;
    }
}
