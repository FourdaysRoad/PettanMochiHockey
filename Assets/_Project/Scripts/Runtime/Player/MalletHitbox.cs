using System.Collections.Generic;
using MochiHockey.Core;
using UnityEngine;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// 杵の当たり判定（REQ-025）。スイング中だけ、体の正面の球（Reach・Radius）にいる相手を集め、
    /// HitRules で当たるかを決める。1 回のスイングで同じ相手には 1 度しか当てない。
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public sealed class MalletHitbox : MonoBehaviour
    {
        private readonly HashSet<PlayerController> _alreadyHit = new HashSet<PlayerController>();
        private readonly List<PlayerController> _hits = new List<PlayerController>();

        // 当たり判定で見つかったコライダーの入れ物（毎回配列を作らないように使い回す。TASK-0031）
        private readonly Collider[] _overlaps = new Collider[32];
        private PlayerController _owner;

        private void Awake()
        {
            _owner = GetComponent<PlayerController>();
        }

        /// <summary>スイングを始めた。前のスイングで当てた相手の記録を消す。</summary>
        public void BeginSwing()
        {
            _alreadyHit.Clear();
        }

        /// <summary>このフレームで新しく当たった相手を返す。</summary>
        public IReadOnlyList<PlayerController> CollectHits()
        {
            _hits.Clear();
            var mallet = _owner.Config.Mallet;
            var forward = transform.forward;
            forward.y = 0f;
            var center = transform.position + forward.normalized * mallet.Reach;

            var count = Physics.OverlapSphereNonAlloc(center, mallet.Radius, _overlaps, ~0, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                var hit = _overlaps[i];
                var target = hit.GetComponentInParent<PlayerController>();
                if (target == null || target == _owner || !target.enabled || _alreadyHit.Contains(target))
                {
                    continue;
                }

                if (HitRules.CanHit(_owner.Id, _owner.Team, target.Id, target.Team, target.CanBeHit))
                {
                    _alreadyHit.Add(target);
                    _hits.Add(target);
                }
            }

            return _hits;
        }

        private void OnDrawGizmosSelected()
        {
            var controller = GetComponent<PlayerController>();
            if (controller == null || controller.Config == null)
            {
                return;
            }

            var mallet = controller.Config.Mallet;
            var forward = transform.forward;
            forward.y = 0f;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + forward.normalized * mallet.Reach, mallet.Radius);
        }
    }
}
