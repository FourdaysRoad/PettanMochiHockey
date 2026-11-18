using System;
using MochiHockey.Core;
using UnityEngine;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// 臼型ゴール（REQ-003）。トリガーのコライダーで、持たれていない餅（打った餅・転がった餅）が入ったことを知らせる。
    /// 餅を持ったまま歩いて入っても得点にはしない（「叩き込む」）。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class GoalPost : MonoBehaviour
    {
        [Tooltip("このゴールを守るチーム。入った得点は相手チームに入る")]
        [SerializeField] private Team defendingTeam;

        public Team DefendingTeam
        {
            get => defendingTeam;
            set => defendingTeam = value;
        }

        /// <summary>餅が入った（このゴール、入った餅）。</summary>
        public event Action<GoalPost, MochiBall> BallEntered;

        /// <summary>床面の位置（x, z）がゴールの中か（ご飯粒を出さない場所の判定に使う）。</summary>
        public bool ContainsOnFloor(Vector3 point)
        {
            var bounds = GetComponent<Collider>().bounds;
            point.y = bounds.center.y;
            return bounds.Contains(point);
        }

        private void OnTriggerEnter(Collider other) => Check(other);

        // 持ったままゴールの中で手放した場合も、離れた瞬間から数える
        private void OnTriggerStay(Collider other) => Check(other);

        private void Check(Collider other)
        {
            var ball = other.GetComponentInParent<MochiBall>();
            if (ball != null && ball.isActiveAndEnabled && !ball.IsHeld)
            {
                BallEntered?.Invoke(this, ball);
            }
        }
    }
}
