using MochiHockey.Core;
using UnityEngine;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// 氷しぶき（REQ-021 の演出）。速く滑っているときに急に向きを変えると、足元から氷の粒を後ろへ飛ばす。
    /// ダッシュした瞬間にもまとめて飛ばす。出すかどうかは Core の IceSprayRule、量としきい値は GameBalanceConfig の Vfx。
    /// Particle System だけを使う（Compute Shader は使わない）。粒の動き・色は見た目のデータなのでここに置く。
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public sealed class SkateVfx : MonoBehaviour
    {
        public const string ParticlesName = "IceSpray";

        [Tooltip("氷の粒のマテリアル（Tools → Mochi Hockey → Create VFX Materials で作る IceSpray）")]
        [SerializeField] private Material material;

        [Header("粒")]
        [SerializeField] private Color color = new Color(0.9f, 0.97f, 1f);
        [SerializeField] private Vector2 sizeRange = new Vector2(0.06f, 0.14f);
        [SerializeField] private Vector2 lifetimeRange = new Vector2(0.3f, 0.5f);

        [Tooltip("後ろへ飛ぶ速さ（m/s）")]
        [SerializeField] private Vector2 backSpeedRange = new Vector2(1.5f, 3.5f);

        [Tooltip("上へ跳ねる速さ（m/s）")]
        [SerializeField] private Vector2 upSpeedRange = new Vector2(1.5f, 3f);

        [Tooltip("横へ散る速さ（m/s）")]
        [SerializeField, Min(0f)] private float sideSpread = 1.5f;

        private PlayerController _player;
        private Collider _playerCollider;
        private IceSprayRule _rule;
        private float _rate;
        private int _dashCount;
        private float _pending;

        public ParticleSystem Particles { get; private set; }

        /// <summary>このフレームのしぶきの強さ（0 なら出していない）。</summary>
        public float Intensity { get; private set; }

        public Material Material
        {
            get => material;
            set
            {
                material = value;
                if (Particles != null)
                {
                    Particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = value;
                }
            }
        }

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
            _playerCollider = GetComponent<Collider>();

            var go = new GameObject(ParticlesName);
            go.transform.SetParent(transform, false);
            Particles = go.AddComponent<ParticleSystem>();
            Particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = Particles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Local; // 貼り付き中の体のつぶれを粒に移さない
            main.gravityModifier = 1.5f;
            main.maxParticles = 200;
            main.startColor = color;

            var emission = Particles.emission;
            emission.enabled = false; // 粒は Emit で 1 つずつ出す

            var shape = Particles.shape;
            shape.enabled = false;

            var sizeOverLifetime = Particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            var particleRenderer = go.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.sharedMaterial = material;

            Particles.Play();
        }

        private void Start()
        {
            var config = _player.Config;
            if (config != null)
            {
                _rule = new IceSprayRule(config.Vfx.SprayMinSpeed, config.Vfx.SprayTurnAngle);
                _rate = config.Vfx.SprayRate;
                _dashCount = config.Vfx.DashSprayCount;
            }

            _player.Dashed += OnDashed;

            if (material == null)
            {
                Debug.LogWarning($"[MochiHockey] {name}: SkateVfx の Material が空です。氷しぶきが正しく表示されません。", this);
            }
        }

        private void OnDestroy()
        {
            if (_player != null)
            {
                _player.Dashed -= OnDashed;
            }
        }

        private void Update()
        {
            var velocity = _player.PlanarVelocity;
            var input = _player.MoveInput;
            Intensity = _player.PettanState == PettanState.Normal || _player.PettanState == PettanState.Invincible
                ? _rule.Intensity(new System.Numerics.Vector2(velocity.x, velocity.y), new System.Numerics.Vector2(input.x, input.y))
                : 0f;

            if (Intensity <= 0f)
            {
                _pending = 0f;
                return;
            }

            _pending += _rate * Intensity * Time.deltaTime;
            var count = Mathf.FloorToInt(_pending);
            _pending -= count;
            Spray(count, velocity);
        }

        private void OnDashed()
        {
            // ダッシュ直後の速度はまだ反映されていないので、向いている方向の逆へ飛ばす
            var forward = transform.forward;
            Spray(_dashCount, new Vector2(forward.x, forward.z));
        }

        private void Spray(int count, Vector2 planarVelocity)
        {
            if (count <= 0)
            {
                return;
            }

            var back = planarVelocity.sqrMagnitude > 0f
                ? -new Vector3(planarVelocity.x, 0f, planarVelocity.y).normalized
                : -transform.forward;
            var side = Vector3.Cross(Vector3.up, back);
            var feet = transform.position;
            if (_playerCollider != null && _playerCollider.enabled)
            {
                feet.y = _playerCollider.bounds.min.y + 0.05f;
            }

            var emit = new ParticleSystem.EmitParams { applyShapeToPosition = false, startColor = color };
            for (var i = 0; i < count; i++)
            {
                emit.position = feet + side * Random.Range(-0.25f, 0.25f);
                emit.velocity = back * Random.Range(backSpeedRange.x, backSpeedRange.y)
                                + Vector3.up * Random.Range(upSpeedRange.x, upSpeedRange.y)
                                + side * Random.Range(-sideSpread, sideSpread);
                emit.startSize = Random.Range(sizeRange.x, sizeRange.y);
                emit.startLifetime = Random.Range(lifetimeRange.x, lifetimeRange.y);
                Particles.Emit(emit, 1);
            }
        }
    }
}
