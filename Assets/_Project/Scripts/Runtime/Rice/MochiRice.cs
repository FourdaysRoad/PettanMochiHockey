using UnityEngine;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// ご飯粒（もち米）。トリガーのコライダーを付ける。
    /// 持たれている餅が触れたら吸収されて消え（スポナーのプールに戻る）、それ以外は素通りする（REQ-012）。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class MochiRice : MonoBehaviour
    {
        /// <summary>このご飯粒を出したスポナー。スポナーを使わずに置いた場合は null。</summary>
        public MochiRiceSpawner Spawner { get; internal set; }

        private void OnTriggerEnter(Collider other)
        {
            var ball = other.GetComponentInParent<MochiBall>();
            if (ball == null || !ball.TryAbsorbRice())
            {
                return;
            }

            if (Spawner != null)
            {
                Spawner.Release(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
