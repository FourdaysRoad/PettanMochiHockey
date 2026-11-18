using System.Collections.Generic;
using MochiHockey.Core;
using UnityEngine;

namespace MochiHockey.Runtime
{
    /// <summary>
    /// RiceSpawnSchedule に従ってご飯粒を出す（REQ-011）。吸収されたご飯粒はプールに戻して使い回す。
    /// 出現範囲はリンクから SpawnMargin だけ内側（ゴールの中は除く）、高さはこのオブジェクトの Y。
    /// </summary>
    public sealed class MochiRiceSpawner : MonoBehaviour
    {
        [SerializeField] private GameBalanceConfig config;
        [SerializeField] private MochiRice ricePrefab;

        private readonly List<MochiRice> _pool = new List<MochiRice>();
        private RiceSpawnSchedule _schedule;
        private GoalPost[] _goals = System.Array.Empty<GoalPost>();

        public GameBalanceConfig Config
        {
            get => config;
            set => config = value;
        }

        public MochiRice RicePrefab
        {
            get => ricePrefab;
            set => ricePrefab = value;
        }

        /// <summary>リンク上に出ているご飯粒の数。</summary>
        public int ActiveCount { get; private set; }

        /// <summary>これまでに作ったご飯粒の数（使い回し分を含む）。</summary>
        public int PoolSize => _pool.Count;

        private void Awake()
        {
            if (config == null || ricePrefab == null)
            {
                Debug.LogError($"[MochiHockey] {name}: GameBalanceConfig or rice prefab is not assigned. MochiRiceSpawner is disabled.", this);
                enabled = false;
                return;
            }

            var settings = config.ToSettings();
            var margin = new System.Numerics.Vector2(config.Rice.SpawnMargin, config.Rice.SpawnMargin);
            var min = settings.Rink.Min + margin;
            var max = settings.Rink.Max - margin;
            if (!(min.X < max.X && min.Y < max.Y))
            {
                Debug.LogError($"[MochiHockey] {name}: Rice.SpawnMargin is too large for the rink. MochiRiceSpawner is disabled.", this);
                enabled = false;
                return;
            }

            _schedule = new RiceSpawnSchedule(settings.Rice, new RinkBounds(min, max), new SystemRandom());
        }

        private void Start()
        {
            _goals = FindObjectsByType<GoalPost>(FindObjectsSortMode.None);
        }

        private void Update()
        {
            var count = _schedule.Tick(Time.deltaTime, ActiveCount);
            for (var i = 0; i < count; i++)
            {
                Spawn();
            }
        }

        /// <summary>吸収されたご飯粒をプールに戻す。</summary>
        public void Release(MochiRice rice)
        {
            if (rice == null || !rice.gameObject.activeSelf)
            {
                return;
            }

            rice.gameObject.SetActive(false);
            ActiveCount--;
        }

        private void Spawn()
        {
            var rice = TakeFromPool();
            var point = _schedule.PickPosition(IsInsideGoal);
            rice.transform.position = new Vector3(point.X, transform.position.y, point.Y);
            rice.gameObject.SetActive(true);
            ActiveCount++;
        }

        private bool IsInsideGoal(System.Numerics.Vector2 point)
        {
            var world = new Vector3(point.X, transform.position.y, point.Y);
            foreach (var goal in _goals)
            {
                if (goal != null && goal.ContainsOnFloor(world))
                {
                    return true;
                }
            }

            return false;
        }

        private MochiRice TakeFromPool()
        {
            foreach (var pooled in _pool)
            {
                if (!pooled.gameObject.activeSelf)
                {
                    return pooled;
                }
            }

            var rice = Instantiate(ricePrefab, transform);
            rice.name = ricePrefab.name;
            rice.gameObject.SetActive(false);
            rice.Spawner = this;
            _pool.Add(rice);
            return rice;
        }
    }
}
