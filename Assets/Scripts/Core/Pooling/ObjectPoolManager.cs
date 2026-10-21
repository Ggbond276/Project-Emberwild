using System.Collections.Generic;
using Emberwild.Core.Singleton;
using UnityEngine;
using CoreLog = Emberwild.Core.AppLog.AppLog;

namespace Emberwild.Core.Pooling
{
    /// <summary>
    /// 全局对象池管理中心。内部按 Prefab 分池，统一维护活动、空闲和累计创建数量。
    /// 管理器不依赖怪物或战斗系统，可同时服务怪物、特效和投射物。
    /// </summary>
    public class ObjectPoolManager : Singleton<ObjectPoolManager>
    {
        /// <summary>
        /// 保存单个 Prefab 对应的活动对象、空闲对象和累计创建数量。
        /// </summary>
        private sealed class PoolBucket
        {
            /// <summary>
            /// 当前池对应的原始 Prefab，同时作为池的字典键。
            /// </summary>
            public GameObject Prefab;

            /// <summary>
            /// 该 Prefab 所有空闲实例的父节点。
            /// </summary>
            public Transform InactiveRoot;

            /// <summary>
            /// 可以被下一次生成直接复用的空闲实例。
            /// </summary>
            public readonly Stack<GameObject> Inactive = new Stack<GameObject>();

            /// <summary>
            /// 已经从池中取出并处于启用状态的对象。
            /// </summary>
            public readonly HashSet<GameObject> Active = new HashSet<GameObject>();

            /// <summary>
            /// 该池自初始化以来累计创建过的实例数量。
            /// </summary>
            public int CreatedCount;
        }

        /// <summary>
        /// 以原始 Prefab 为键保存所有独立对象池。
        /// </summary>
        private readonly Dictionary<GameObject, PoolBucket> _buckets =
            new Dictionary<GameObject, PoolBucket>();

        /// <summary>
        /// 池管理器自动创建的隐藏根节点。
        /// </summary>
        private GameObject _root;

        /// <summary>
        /// 所有活动池对象的父节点。
        /// </summary>
        private Transform _activeRoot;

        /// <summary>
        /// 所有空闲池对象的父节点。
        /// </summary>
        private Transform _inactiveRoot;

        /// <summary>
        /// 标记管理器当前是否已经完成运行时初始化。
        /// </summary>
        private bool _initialized;

        /// <summary>
        /// 池管理器是否已经完成初始化。
        /// </summary>
        public bool IsInitialized => _initialized && _root != null;

        /// <summary>
        /// 获取当前由对象池管理的活动对象总数。
        /// </summary>
        public int TotalActiveCount
        {
            get
            {
                int count = 0;
                foreach (var bucket in _buckets.Values)
                {
                    PruneDestroyed(bucket);
                    count += bucket.Active.Count;
                }
                return count;
            }
        }

        /// <summary>
        /// 获取当前所有池中可以立即复用的空闲对象总数。
        /// </summary>
        public int TotalInactiveCount
        {
            get
            {
                int count = 0;
                foreach (var bucket in _buckets.Values)
                    count += CountValidInactive(bucket);
                return count;
            }
        }

        /// <summary>
        /// 创建隐藏的池根节点。重复调用是安全的，不需要在场景中手动挂载管理器。
        /// </summary>
        public void Initialize()
        {
            if (IsInitialized) return;

            _buckets.Clear();
            _root = new GameObject("[ObjectPoolManager]");
            Object.DontDestroyOnLoad(_root);

            _activeRoot = new GameObject("Active").transform;
            _activeRoot.SetParent(_root.transform, false);

            _inactiveRoot = new GameObject("Inactive").transform;
            _inactiveRoot.SetParent(_root.transform, false);

            _initialized = true;
            CoreLog.Info("Pool",
                $"initialized active={TotalActiveCount} inactive={TotalInactiveCount}");
        }

        /// <summary>
        /// 从指定 Prefab 对应的池中取出对象。没有空闲实例时才会创建新实例。
        /// </summary>
        /// <param name="prefab">需要生成对象时使用的原始 Prefab，同时决定使用哪一个对象池。</param>
        /// <param name="position">实例生成后的世界坐标。</param>
        /// <param name="rotation">实例生成后的世界旋转。</param>
        /// <param name="parent">可选的父节点；为空时放入池管理器的活动对象根节点。</param>
        /// <returns>已经激活并完成位置设置的池对象；Prefab 无效时返回 null。</returns>
        public GameObject Spawn(
            GameObject prefab,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null)
        {
            if (prefab == null)
            {
                CoreLog.Error("Pool", "spawn failed: prefab is null.");
                return null;
            }

            EnsureInitialized();
            PoolBucket bucket = GetOrCreateBucket(prefab);

            GameObject instance = null;
            while (bucket.Inactive.Count > 0 && instance == null)
                instance = bucket.Inactive.Pop();

            if (instance == null)
                instance = CreateInactiveInstance(bucket);

            PooledObject pooled = instance.GetComponent<PooledObject>();
            if (pooled == null)
                pooled = instance.AddComponent<PooledObject>();

            pooled.Bind(this, prefab);
            pooled.CancelRelease();
            pooled.MarkTaken();

            Transform targetParent = parent != null ? parent : _activeRoot;
            instance.transform.SetParent(targetParent, false);
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);
            bucket.Active.Add(instance);

            NotifySpawned(instance);
            CoreLog.Info("Pool",
                $"spawn '{prefab.name}' active={bucket.Active.Count} "
                + $"inactive={CountValidInactive(bucket)} created={bucket.CreatedCount} "
                + $"| totalActive={TotalActiveCount} totalInactive={TotalInactiveCount}");
            return instance;
        }

        /// <summary>
        /// 将池对象停用并放回其来源池。重复回收会被拒绝。
        /// </summary>
        /// <param name="instance">需要停用并回池的池对象实例。</param>
        /// <returns>成功回池时返回 true；对象为空、不属于本池或已经回池时返回 false。</returns>
        public bool Despawn(GameObject instance)
        {
            if (instance == null) return false;
            if (!IsInitialized)
            {
                CoreLog.Warning("Pool", $"despawn ignored: pool manager is not initialized for '{instance.name}'.");
                return false;
            }

            PooledObject pooled = instance.GetComponent<PooledObject>();
            if (pooled == null || pooled.Owner != this || pooled.SourcePrefab == null)
            {
                CoreLog.Warning("Pool", $"despawn ignored: '{instance.name}' is not managed by this pool.");
                return false;
            }
            if (pooled.IsInPool)
            {
                CoreLog.Warning("Pool", $"despawn ignored: '{instance.name}' is already in the pool.");
                return false;
            }
            if (!_buckets.TryGetValue(pooled.SourcePrefab, out PoolBucket bucket))
            {
                CoreLog.Warning("Pool", $"despawn ignored: source pool for '{instance.name}' was not found.");
                return false;
            }

            pooled.CancelRelease();
            NotifyReturned(instance);
            bucket.Active.Remove(instance);

            instance.SetActive(false);
            instance.transform.SetParent(bucket.InactiveRoot, false);
            pooled.MarkReturned();
            bucket.Inactive.Push(instance);

            CoreLog.Info("Pool",
                $"despawn '{pooled.SourcePrefab.name}' active={bucket.Active.Count} "
                + $"inactive={CountValidInactive(bucket)} created={bucket.CreatedCount} "
                + $"| totalActive={TotalActiveCount} totalInactive={TotalInactiveCount}");
            return true;
        }

        /// <summary>
        /// 在指定延迟后自动回收对象，适用于命中特效等短生命周期资源。
        /// </summary>
        /// <param name="instance">需要延迟回收的池对象实例。</param>
        /// <param name="delay">从调用开始计算的等待秒数；小于零时按零处理。</param>
        public void DespawnAfter(GameObject instance, float delay)
        {
            if (instance == null) return;
            PooledObject pooled = instance.GetComponent<PooledObject>();
            if (pooled == null)
            {
                CoreLog.Warning("Pool", $"delayed despawn ignored: '{instance.name}' is not pooled.");
                return;
            }
            pooled.ScheduleRelease(Mathf.Max(0f, delay));
        }

        /// <summary>
        /// 预先创建指定数量的空闲实例，避免运行期间首次创建造成尖峰。
        /// </summary>
        /// <param name="prefab">需要预热的原始 Prefab。</param>
        /// <param name="count">要提前创建的实例数量；小于等于零时不执行。</param>
        public void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null || count <= 0) return;

            EnsureInitialized();
            PoolBucket bucket = GetOrCreateBucket(prefab);
            for (int i = 0; i < count; i++)
                bucket.Inactive.Push(CreateInactiveInstance(bucket));

            CoreLog.Info("Pool",
                $"prewarm '{prefab.name}' +{count} inactive={CountValidInactive(bucket)} "
                + $"created={bucket.CreatedCount}");
        }

        /// <summary>
        /// 返回某个 Prefab 当前的活动对象数量。
        /// </summary>
        /// <param name="prefab">要查询的原始 Prefab。</param>
        /// <returns>该 Prefab 当前处于活动状态的对象数量。</returns>
        public int GetActiveCount(GameObject prefab)
        {
            if (prefab == null || !_buckets.TryGetValue(prefab, out PoolBucket bucket))
                return 0;
            PruneDestroyed(bucket);
            return bucket.Active.Count;
        }

        /// <summary>
        /// 返回某个 Prefab 当前可复用的空闲对象数量。
        /// </summary>
        /// <param name="prefab">要查询的原始 Prefab。</param>
        /// <returns>该 Prefab 当前存放在池中的有效空闲实例数量。</returns>
        public int GetInactiveCount(GameObject prefab)
        {
            if (prefab == null || !_buckets.TryGetValue(prefab, out PoolBucket bucket))
                return 0;
            return CountValidInactive(bucket);
        }

        /// <summary>
        /// 清空所有池对象。通常只在退出 Play 或重置测试场景时调用。
        /// </summary>
        public void Clear()
        {
            if (!IsInitialized) return;

            int active = TotalActiveCount;
            int inactive = TotalInactiveCount;
            Object.Destroy(_root);
            _root = null;
            _activeRoot = null;
            _inactiveRoot = null;
            _buckets.Clear();
            _initialized = false;
            CoreLog.Info("Pool", $"cleared active={active} inactive={inactive}");
        }

        /// <summary>
        /// 确保管理器已经初始化；第一次访问池时自动创建运行时根节点。
        /// </summary>
        private void EnsureInitialized()
        {
            if (!IsInitialized) Initialize();
        }

        /// <summary>
        /// 获取指定 Prefab 的对象池；不存在时创建该 Prefab 的独立池。
        /// </summary>
        /// <param name="prefab">用于查询或创建对象池的原始 Prefab。</param>
        /// <returns>该 Prefab 对应的独立对象池。</returns>
        private PoolBucket GetOrCreateBucket(GameObject prefab)
        {
            if (_buckets.TryGetValue(prefab, out PoolBucket bucket))
                return bucket;

            Transform inactiveRoot = new GameObject($"{prefab.name}_Pool").transform;
            inactiveRoot.SetParent(_inactiveRoot, false);

            bucket = new PoolBucket
            {
                Prefab = prefab,
                InactiveRoot = inactiveRoot
            };
            _buckets.Add(prefab, bucket);
            return bucket;
        }

        /// <summary>
        /// 创建一个停用状态的池实例，并写入来源池信息。
        /// </summary>
        /// <param name="bucket">负责管理该实例的对象池。</param>
        /// <returns>已经挂到空闲根节点下但尚未加入空闲栈的新实例。</returns>
        private GameObject CreateInactiveInstance(PoolBucket bucket)
        {
            GameObject instance = Object.Instantiate(bucket.Prefab, bucket.InactiveRoot);
            instance.name = bucket.Prefab.name;
            instance.SetActive(false);

            PooledObject pooled = instance.GetComponent<PooledObject>();
            if (pooled == null)
                pooled = instance.AddComponent<PooledObject>();
            pooled.Bind(this, bucket.Prefab);
            pooled.MarkReturned();

            bucket.CreatedCount++;
            return instance;
        }

        /// <summary>
        /// 通知实例上的所有池化组件：对象已经被取出并激活。
        /// </summary>
        /// <param name="instance">刚刚从池中取出的对象实例。</param>
        private static void NotifySpawned(GameObject instance)
        {
            foreach (MonoBehaviour component in instance.GetComponents<MonoBehaviour>())
            {
                if (component is IPoolable poolable)
                    poolable.OnSpawnedFromPool();
            }
        }

        /// <summary>
        /// 通知实例上的所有池化组件：对象即将被停用并放回池。
        /// </summary>
        /// <param name="instance">即将回池的对象实例。</param>
        private static void NotifyReturned(GameObject instance)
        {
            foreach (MonoBehaviour component in instance.GetComponents<MonoBehaviour>())
            {
                if (component is IPoolable poolable)
                    poolable.OnReturnedToPool();
            }
        }

        /// <summary>
        /// 从活动集合中移除已经被外部销毁的 Unity 空引用。
        /// </summary>
        /// <param name="bucket">需要清理活动对象记录的对象池。</param>
        private static void PruneDestroyed(PoolBucket bucket)
        {
            bucket.Active.RemoveWhere(instance => instance == null);
        }

        /// <summary>
        /// 统计对象池中仍然有效的空闲实例数量。
        /// </summary>
        /// <param name="bucket">需要统计的对象池。</param>
        /// <returns>空闲栈中未被外部销毁的实例数量。</returns>
        private static int CountValidInactive(PoolBucket bucket)
        {
            int count = 0;
            foreach (GameObject instance in bucket.Inactive)
            {
                if (instance != null)
                    count++;
            }
            return count;
        }
    }
}
