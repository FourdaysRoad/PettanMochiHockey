using System;
using MochiHockey.Core;
using UnityEngine;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// ゲームの調整値の置き場（REQ-402）。数値はここ（とこのアセット）にだけ置き、
    /// ToSettings() で Core 用の BalanceSettings に変換して渡す。
    /// 初期値は仕様書（docs/spec/pettan-mochi-hockey.md）の値。
    /// 「仮」と書いた項目は仕様書に値がないため、遊びながら調整する。
    /// </summary>
    [CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "Mochi Hockey/Game Balance Config")]
    public sealed class GameBalanceConfig : ScriptableObject
    {
        public MochiConfig Mochi = new MochiConfig();
        public PettanConfig Pettan = new PettanConfig();
        public MatchConfig Match = new MatchConfig();
        public RiceConfig Rice = new RiceConfig();
        public PlayerConfig Player = new PlayerConfig();
        public RinkConfig Rink = new RinkConfig();

        /// <summary>餅の物理・見た目まわり（Runtime だけが使う。Core には渡さない）。</summary>
        public MochiBallConfig MochiBall = new MochiBallConfig();

        /// <summary>杵の当たり判定の大きさ（Runtime だけが使う）。</summary>
        public MalletConfig Mallet = new MalletConfig();

        /// <summary>カメラ（Runtime だけが使う）。</summary>
        public CameraConfig Camera = new CameraConfig();

        /// <summary>演出（Runtime だけが使う）。</summary>
        public VfxConfig Vfx = new VfxConfig();

        /// <summary>音（Runtime だけが使う）。</summary>
        public AudioConfig Audio = new AudioConfig();

        /// <summary>Core 用の設定に変換する。値がおかしければ InvalidOperationException を投げる。</summary>
        public BalanceSettings ToSettings()
        {
            var settings = new BalanceSettings
            {
                Mochi = new MochiSettings
                {
                    Lv2Threshold = Mochi.Lv2Threshold,
                    Lv3Threshold = Mochi.Lv3Threshold,
                    ScaleByLevel = CopyOf(Mochi.ScaleByLevel),
                    PointsByLevel = CopyOf(Mochi.PointsByLevel),
                    SpeedMultiplierByLevel = CopyOf(Mochi.SpeedMultiplierByLevel),
                    RegrabCooldown = Mochi.RegrabCooldown,
                },
                Pettan = new PettanSettings
                {
                    HitStopDuration = Pettan.HitStopDuration,
                    LaunchMaxDuration = Pettan.LaunchMaxDuration,
                    LaunchSpeed = Pettan.LaunchSpeed,
                    LaunchWallBounces = Pettan.LaunchWallBounces,
                    SlideDownStartTime = Pettan.SlideDownStartTime,
                    RecoverTime = Pettan.RecoverTime,
                    InvincibleEndTime = Pettan.InvincibleEndTime,
                    SlideDownSpeed = Pettan.SlideDownSpeed,
                    StuckScale = new System.Numerics.Vector3(
                        Pettan.StuckScale.x, Pettan.StuckScale.y, Pettan.StuckScale.z),
                },
                Match = new MatchSettings
                {
                    MatchDuration = Match.MatchDuration,
                    KickoffCountdown = Match.KickoffCountdown,
                    MochiRespawnDelay = Match.MochiRespawnDelay,
                },
                Rice = new RiceSettings
                {
                    MinCount = Rice.MinCount,
                    MaxCount = Rice.MaxCount,
                    RefillInterval = Rice.RefillInterval,
                },
                Player = new PlayerSettings
                {
                    MaxSpeed = Player.MaxSpeed,
                    Acceleration = Player.Acceleration,
                    Friction = Player.Friction,
                    StaminaMax = Player.StaminaMax,
                    DashCost = Player.DashCost,
                    StaminaRegenPerSecond = Player.StaminaRegenPerSecond,
                    DashImpulse = Player.DashImpulse,
                    ChargeMaxTime = Player.ChargeMaxTime,
                    ChargeStages = Player.ChargeStages,
                    SwingActiveTime = Player.SwingActiveTime,
                    WhiffRecoveryTime = Player.WhiffRecoveryTime,
                },
                Rink = ToRinkBounds(Rink.Size),
            };

            settings.Validate();
            return settings;
        }

        /// <summary>原点を中心にした矩形にする。大きさが 0 以下なら default のままにして Validate で弾く。</summary>
        private static RinkBounds ToRinkBounds(Vector2 size)
        {
            if (size.x <= 0f || size.y <= 0f)
            {
                return default;
            }

            var half = new System.Numerics.Vector2(size.x / 2f, size.y / 2f);
            return new RinkBounds(-half, half);
        }

        private static T[] CopyOf<T>(T[] source) => source == null ? null : (T[])source.Clone();

        [Serializable]
        public sealed class MochiConfig
        {
            [Tooltip("この数以上のご飯粒で Lv.2")]
            [Min(1)] public int Lv2Threshold = 3;

            [Tooltip("この数以上のご飯粒で Lv.3")]
            [Min(1)] public int Lv3Threshold = 6;

            [Tooltip("レベル別の大きさ（Lv.1, Lv.2, Lv.3）")]
            public float[] ScaleByLevel = { 1.0f, 1.5f, 2.5f };

            [Tooltip("レベル別の得点（Lv.1, Lv.2, Lv.3）")]
            public int[] PointsByLevel = { 1, 2, 3 };

            [Tooltip("レベル別の持ち手の速度倍率（Lv.1, Lv.2, Lv.3）")]
            public float[] SpeedMultiplierByLevel = { 1.0f, 0.9f, 0.75f };

            [Tooltip("手放した直後に再び吸着できるまでの秒数")]
            [Min(0f)] public float RegrabCooldown = 0.3f;
        }

        [Serializable]
        public sealed class PettanConfig
        {
            [Tooltip("被弾直後に止まる秒数")]
            [Min(0f)] public float HitStopDuration = 0.08f;


            [Tooltip("仮：吹っ飛びを続けられる上限の秒数（被弾から）。これまでに貼り付けなければその場で復帰する")]
            [Min(0f)] public float LaunchMaxDuration = 2f;

            [Tooltip("仮：吹っ飛びの直進速度（m/s）。高さは変えずに水平にまっすぐ飛ぶ")]
            [Min(0f)] public float LaunchSpeed = 30f;

            [Tooltip("壁で跳ね返る回数。この回数だけ跳ね返ったあと、次に当たった壁に貼り付く")]
            [Min(0)] public int LaunchWallBounces = 1;

            [Tooltip("被弾からずり落ちが始まるまでの秒数")]
            [Min(0f)] public float SlideDownStartTime = 1.5f;

            [Tooltip("被弾から復帰するまでの秒数")]
            [Min(0f)] public float RecoverTime = 2.6f;

            [Tooltip("被弾から無敵が終わるまでの秒数")]
            [Min(0f)] public float InvincibleEndTime = 3.6f;

            [Tooltip("仮：壁沿いにずり落ちる速度（m/s）")]
            [Min(0f)] public float SlideDownSpeed = 0.5f;

            [Tooltip("壁に貼り付いている間の大きさ")]
            public Vector3 StuckScale = new Vector3(1.3f, 1.3f, 0.05f);

            [Tooltip("仮：無敵中の点滅の間隔（秒）。Runtime だけが使う")]
            [Min(0.01f)] public float BlinkInterval = 0.1f;

            [Tooltip("仮：ピヨり（無敵中に頭の上で回る星）の回る速さ（度/秒）。Runtime だけが使う")]
            [Min(0f)] public float DizzySpinSpeed = 240f;
        }

        [Serializable]
        public sealed class MatchConfig
        {
            [Tooltip("試合時間（秒）")]
            [Min(1f)] public float MatchDuration = 180f;

            [Tooltip("仮：キックオフ前のカウントダウン（秒）")]
            [Min(0f)] public float KickoffCountdown = 3f;

            [Tooltip("ゴール後に餅が中央に戻るまでの秒数")]
            [Min(0f)] public float MochiRespawnDelay = 1f;

            [Tooltip("仮：リザルト画面が出てから、ボタン（もう一度／タイトルへ）を受け付けるまでの秒数。試合中の連打で押されないように。Runtime だけが使う")]
            [Min(0f)] public float ResultInputDelay = 1f;
        }

        [Serializable]
        public sealed class RiceConfig
        {
            [Tooltip("リンク上のご飯粒の最小数（下回ると補充）")]
            [Min(0)] public int MinCount = 5;

            [Tooltip("リンク上のご飯粒の最大数")]
            [Min(0)] public int MaxCount = 10;

            [Tooltip("補充の間隔（秒）")]
            [Min(0f)] public float RefillInterval = 1.5f;

            [Tooltip("仮：出現位置を壁からどれだけ離すか（m）。Runtime だけが使う")]
            [Min(0f)] public float SpawnMargin = 1f;
        }

        [Serializable]
        public sealed class PlayerConfig
        {
            [Tooltip("仮：最高速度（m/s）")]
            [Min(0f)] public float MaxSpeed = 6f;

            [Tooltip("仮：加速度（m/s²）")]
            [Min(0f)] public float Acceleration = 20f;

            [Tooltip("仮：入力がないときの減速の強さ（1/s）")]
            [Min(0f)] public float Friction = 3f;

            [Tooltip("仮：スタミナの最大値")]
            [Min(0f)] public float StaminaMax = 100f;

            [Tooltip("仮：ダッシュ 1 回の消費スタミナ")]
            [Min(0f)] public float DashCost = 35f;

            [Tooltip("仮：1 秒あたりのスタミナ回復量")]
            [Min(0f)] public float StaminaRegenPerSecond = 20f;

            [Tooltip("仮：ダッシュで加える速度（m/s）")]
            [Min(0f)] public float DashImpulse = 8f;

            [Tooltip("チャージが最大になるまでの秒数")]
            [Min(0f)] public float ChargeMaxTime = 1f;

            [Tooltip("チャージの段階数")]
            [Min(1)] public int ChargeStages = 2;

            [Tooltip("仮：杵の当たり判定が続く秒数")]
            [Min(0f)] public float SwingActiveTime = 0.15f;

            [Tooltip("仮：空振りしたときの硬直の秒数")]
            [Min(0f)] public float WhiffRecoveryTime = 0.4f;
        }

        [Serializable]
        public sealed class RinkConfig
        {
            [Tooltip("仮：リンクの内側の大きさ（x = 横幅、y = 奥行き。m）。原点が中心")]
            public Vector2 Size = new Vector2(24f, 14f);
        }

        [Serializable]
        public sealed class MochiBallConfig
        {
            [Tooltip("仮：チャージ段階ごとのシュート速度（m/s）。段階 0（ノーマル）〜 ChargeStages の順で、要素数は ChargeStages + 1")]
            public float[] ShotSpeedByStage = { 10f, 14f, 18f };

            [Tooltip("仮：レベル別のシュート速度の倍率（Lv.1, Lv.2, Lv.3）。Lv.2 からシュートが強化される（REQ-015）")]
            public float[] ShotSpeedMultiplierByLevel = { 1.0f, 1.2f, 1.2f };

            [Tooltip("仮：吸着できる正面の角度（度）。プレイヤーの正面から左右にこの半分まで")]
            [Range(0f, 360f)] public float AttachAngle = 120f;

            [Tooltip("仮：ドリブル中の、プレイヤーの体と餅のすき間（m）")]
            [Min(0f)] public float DribbleGap = 0.1f;

            [Tooltip("仮：被弾で落としたときに跳ねる上向きの速さ（m/s）")]
            [Min(0f)] public float DropPopSpeed = 3f;

            [Tooltip("仮：誰も持っていない餅の減速の強さ（Rigidbody の Linear Damping）。氷の上でも少しずつ止まる")]
            [Min(0f)] public float LinearDamping = 0.5f;

            [Tooltip("仮：Lv.3 で打った餅が敵を巻き込んで吹き飛ばす（貫通する）間の最低速度（m/s）。これより遅くなったら普通の餅に戻る")]
            [Min(0f)] public float PierceMinSpeed = 5f;
        }

        [Serializable]
        public sealed class MalletConfig
        {
            [Tooltip("仮：体の中心から、杵の当たり判定の中心までの距離（m）")]
            [Min(0f)] public float Reach = 1.2f;

            [Tooltip("仮：杵の当たり判定（球）の半径（m）")]
            [Min(0.01f)] public float Radius = 0.8f;

            [Tooltip("仮：杵の見た目の構えの角度（度、+ が右）。見た目だけ")]
            [Range(-180f, 180f)] public float RestYaw = 60f;

            [Tooltip("仮：杵の見た目を振り抜く角度（度）。右から左へ振る。振る時間は Player.SwingActiveTime。見た目だけ")]
            [Range(0f, 360f)] public float SwingArcAngle = 150f;

            [Tooltip("仮：振り抜いた杵が構えに戻るまでの秒数。見た目だけ")]
            [Min(0f)] public float SwingReturnTime = 0.2f;
        }

        [Serializable]
        public sealed class CameraConfig
        {
            [Tooltip("仮：見下ろす角度（度）。90 で真上から")]
            [Range(20f, 90f)] public float Pitch = 55f;

            [Tooltip("仮：映すものを画面のどれだけに収めるか（0〜1）。小さいほど余白が広い")]
            [Range(0.1f, 1f)] public float FramingSize = 0.75f;

            [Tooltip("仮：カメラが近づける最小の距離（m）。これより寄らない")]
            [Min(1f)] public float MinDistance = 14f;

            [Tooltip("仮：カメラが離れられる最大の距離（m）。これより引かない")]
            [Min(1f)] public float MaxDistance = 34f;

            [Tooltip("仮：カメラが追いかける遅れ（秒）。0 ですぐ")]
            [Min(0f)] public float Damping = 0.6f;

            [Tooltip("仮：画面に収めるときに考えるプレイヤーの大きさ（半径 m）")]
            [Min(0f)] public float PlayerRadius = 1.5f;
        }

        [Serializable]
        public sealed class VfxConfig
        {
            [Tooltip("仮：氷しぶきを出す最低の速さ（m/s）")]
            [Min(0f)] public float SprayMinSpeed = 3f;

            [Tooltip("仮：氷しぶきを出す、進む向きと入力の向きのずれ（度）")]
            [Range(0f, 179f)] public float SprayTurnAngle = 70f;

            [Tooltip("仮：急な方向転換で 1 秒に出す氷しぶきの粒の数（いちばん強いとき）")]
            [Min(0f)] public float SprayRate = 80f;

            [Tooltip("仮：ダッシュしたときに一度に出す氷しぶきの粒の数")]
            [Min(0)] public int DashSprayCount = 16;

            [Tooltip("仮：Pettan!!! の吹き出しを出しておく秒数")]
            [Min(0.05f)] public float PettanBubbleDuration = 0.7f;

            [Tooltip("仮：Pettan!!! の吹き出しを出す高さ（叩かれたプレイヤーの中心から m）")]
            public float PettanBubbleHeight = 1.6f;

            [Tooltip("仮：Pettan!!! の吹き出しの大きさ（m）")]
            [Min(0.1f)] public float PettanBubbleSize = 2.2f;

            [Tooltip("仮：衝撃波（広がる輪）が広がりきるまでの秒数")]
            [Min(0.05f)] public float ShockwaveDuration = 0.3f;

            [Tooltip("仮：衝撃波が広がりきったときの直径（m）")]
            [Min(0.1f)] public float ShockwaveSize = 3.5f;
        }

        [Serializable]
        public sealed class AudioConfig
        {
            [Tooltip("仮：合成する音のサンプリング周波数（Hz）。低いほど作るのが速く、メモリも少ない")]
            [Range(8000, 48000)] public int SampleRate = 22050;

            [Tooltip("仮：効果音の音量（0〜1）")]
            [Range(0f, 1f)] public float SeVolume = 0.8f;

            [Tooltip("仮：BGM の音量（0〜1）")]
            [Range(0f, 1f)] public float BgmVolume = 0.35f;

            [Tooltip("仮：BGM のテンポ（1 分あたりの拍）")]
            [Range(40f, 240f)] public float BgmTempo = 132f;

            [Tooltip("仮：同じ効果音でも高さを少しずつ変える幅（0.06 なら ±6%）。くり返し聞いても飽きにくくする")]
            [Range(0f, 0.3f)] public float PitchJitter = 0.06f;

            [Tooltip("仮：同時に鳴らせる効果音の数")]
            [Range(1, 32)] public int Voices = 10;

            [Tooltip("仮：餅が壁などに当たったときに音を鳴らす最低の速さ（m/s）")]
            [Min(0f)] public float BallBounceMinSpeed = 2f;
        }
    }
}
