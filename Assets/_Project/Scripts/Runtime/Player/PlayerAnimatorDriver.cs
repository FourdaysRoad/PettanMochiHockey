using System.Collections.Generic;
using UnityEngine;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// ゲームの状態をモデルの Animator のパラメータに渡す。どのモデルでも、Animator Controller に次の名前の
    /// パラメータを用意すれば動く（ないパラメータは黙って飛ばす）。
    /// - Speed（Float）：床面の速さ（m/s）
    /// - PettanState（Int）：0 Normal / 1 HitStop / 2 Launched / 3 Stuck / 4 Invincible
    /// - Swing（Trigger）：杵を振った瞬間
    /// - Dizzy（Bool）：ピヨり中（復帰直後の無敵中）
    /// - Charging（Bool）：シュートをチャージ中
    /// PlayerVisual がモデルを付けたときに追加する。
    /// </summary>
    public sealed class PlayerAnimatorDriver : MonoBehaviour
    {
        public const string SpeedParameter = "Speed";
        public const string PettanStateParameter = "PettanState";
        public const string SwingParameter = "Swing";
        public const string DizzyParameter = "Dizzy";
        public const string ChargingParameter = "Charging";

        private static readonly int SpeedHash = Animator.StringToHash(SpeedParameter);
        private static readonly int PettanStateHash = Animator.StringToHash(PettanStateParameter);
        private static readonly int SwingHash = Animator.StringToHash(SwingParameter);
        private static readonly int DizzyHash = Animator.StringToHash(DizzyParameter);
        private static readonly int ChargingHash = Animator.StringToHash(ChargingParameter);

        private readonly HashSet<int> _available = new HashSet<int>();
        private Animator _animator;
        private PlayerController _player;

        public Animator Animator => _animator;

        internal void Bind(Animator animator)
        {
            _animator = animator;
            _available.Clear();
            if (animator.runtimeAnimatorController == null)
            {
                return;
            }

            foreach (var parameter in animator.parameters)
            {
                _available.Add(parameter.nameHash);
            }
        }

        private void Start()
        {
            _player = GetComponent<PlayerController>();
            if (_player != null)
            {
                _player.Swung += OnSwung;
            }
        }

        private void OnDestroy()
        {
            if (_player != null)
            {
                _player.Swung -= OnSwung;
            }
        }

        private void LateUpdate()
        {
            if (_animator == null || _player == null || !_animator.isActiveAndEnabled)
            {
                return;
            }

            if (_available.Contains(SpeedHash))
            {
                _animator.SetFloat(SpeedHash, _player.PlanarVelocity.magnitude);
            }

            if (_available.Contains(PettanStateHash))
            {
                _animator.SetInteger(PettanStateHash, (int)_player.PettanState);
            }

            if (_available.Contains(DizzyHash))
            {
                _animator.SetBool(DizzyHash, _player.PettanState == Core.PettanState.Invincible);
            }

            if (_available.Contains(ChargingHash))
            {
                _animator.SetBool(ChargingHash, _player.ShotChargeNormalized > 0f);
            }
        }

        private void OnSwung()
        {
            if (_animator != null && _available.Contains(SwingHash))
            {
                _animator.SetTrigger(SwingHash);
            }
        }
    }
}
