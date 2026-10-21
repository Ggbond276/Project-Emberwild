using System.Collections.Generic;
using Emberwild.AI.Enemy;
using Emberwild.Combat;
using Emberwild.Core.AppLog;
using Emberwild.Core.Events;
using Emberwild.Core.Pooling;
using Emberwild.Manager;
using UnityEngine;

namespace Emberwild.AI
{
    public enum EnemySpawnSelectionMode
    {
        // 始终生成 spawnTid 指定的怪物
        FixedTid,

        // 从 randomTids 中随机选择怪物
        RandomTid
    }

    public class EnemySpawner : MonoBehaviour
    {
        #region 刷怪内部类型 (Spawn Types)
        // 绑定一只活动怪物、AI 控制器和回池时间
        private sealed class ActiveEnemy
        {
            public GameObject Instance;
            public EnemyBrain Brain;
            public float ReleaseAt = -1f;
        }
        #endregion

        #region 刷怪配置 (Spawn Configuration)
        // 刷怪配置
        [Header("生成设置")]
        [Tooltip("指定 TID 或从随机列表中生成。")]
        public EnemySpawnSelectionMode spawnMode = EnemySpawnSelectionMode.FixedTid;

        [Tooltip("FixedTid 模式下要生成的怪物 TID。")]
        public int spawnTid = 2001;

        [Tooltip("RandomTid 模式下参与随机选择的怪物 TID 列表。")]
        public List<int> randomTids = new List<int>();

        [Tooltip("生成时间间隔(秒)")]
        public float spawnInterval = 3f;

        [Tooltip("最大同时存在的怪物数量，包含等待回收的尸体")]
        public int maxEnemies = 10;

        [Tooltip("怪物生成的随机半径范围")]
        public float spawnRadius = 5f;

        [Tooltip("怪物死亡后保留尸体并等待回池的时间(秒)")]
        public float corpseLifetime = 3f;
        #endregion

        #region 刷怪运行时状态 (Spawn Runtime State)
        // 当前活动怪物与 Prefab 缓存
        private readonly List<ActiveEnemy> _activeEnemies = new List<ActiveEnemy>();
        private readonly Dictionary<int, GameObject> _prefabsByTid =
            new Dictionary<int, GameObject>();

        // 生成计时与配置验证状态
        private float _timer;
        private bool _configurationValidated;
        private bool _configurationErrorReported;

        private float EffectiveCorpseLifetime =>
            corpseLifetime > 0f ? corpseLifetime : 3f;
        #endregion

        #region 编辑器配置更新 (Editor Configuration Refresh)
        /// <summary>清理配置验证缓存，让下次刷怪重新检查 Inspector 参数。</summary>
        private void OnValidate()
        {
            _configurationValidated = false;
            _configurationErrorReported = false;
            _prefabsByTid.Clear();
        }
        #endregion

        #region 刷怪循环与配置校验 (Spawn Loop & Validation)
        /// <summary>每帧回收尸体并尝试生成下一只怪物。</summary>
        private void Update()
        {
            ReleaseExpiredEnemies();
            PruneDestroyedEnemies();

            if (!ValidateConfiguration() || maxEnemies <= 0) return;
            if (_activeEnemies.Count >= maxEnemies) return;

            _timer += Time.deltaTime;
            if (_timer < spawnInterval) return;

            _timer = 0f;
            SpawnEnemy();
        }

        /// <summary>验证当前刷怪模式需要的 TID 和 Prefab。</summary>
        private bool ValidateConfiguration()
        {
            if (_configurationValidated) return true;

            if (spawnMode == EnemySpawnSelectionMode.FixedTid)
            {
                if (!ValidateTid(spawnTid)) return false;
            }
            else
            {
                if (randomTids == null || randomTids.Count == 0)
                {
                    ReportConfigurationError(
                        "RandomTid mode requires at least one TID in randomTids.");
                    return false;
                }

                for (int i = 0; i < randomTids.Count; i++)
                {
                    if (!ValidateTid(randomTids[i])) return false;
                }
            }

            _configurationValidated = true;
            AppLog.Info("Enemy",
                spawnMode == EnemySpawnSelectionMode.FixedTid
                    ? $"spawner ready mode=FixedTid TID={spawnTid}"
                    : $"spawner ready mode=RandomTid count={randomTids.Count}");
            return true;
        }

        /// <summary>验证 TID 并缓存对应的怪物 Prefab。</summary>
        private bool ValidateTid(int tid)
        {
            if (!TryResolvePrefab(tid, out GameObject prefab, out string error))
            {
                ReportConfigurationError(error);
                return false;
            }

            _prefabsByTid[tid] = prefab;
            return true;
        }

        /// <summary>通过 CharacterDefine 的资源路径解析怪物 Prefab。</summary>
        private static bool TryResolvePrefab(
            int tid,
            out GameObject prefab,
            out string error)
        {
            prefab = null;
            error = null;
            if (tid <= 0)
            {
                error = $"Spawn TID must be greater than 0 (received {tid}).";
                return false;
            }

            var characters = DataManager.Instance.Characters;
            if (characters == null || !characters.TryGetValue(tid, out var define))
            {
                error = $"Monster TID {tid} was not found in CharacterDefine.";
                return false;
            }

            prefab = Resources.Load<GameObject>(define.Resource);
            if (prefab == null)
            {
                error = $"Resource '{define.Resource}' for TID {tid} was not found.";
                return false;
            }

            if (prefab.GetComponent<EnemyBrain>() == null)
            {
                error = $"Prefab '{prefab.name}' for TID {tid} has no EnemyBrain component.";
                prefab = null;
                return false;
            }
            return true;
        }
        #endregion

        #region 怪物生成与回收操作 (Spawn & Recycle Operations)
        /// <summary>选择 TID、从对象池生成怪物并完成出生初始化。</summary>
        private void SpawnEnemy()
        {
            int tid = SelectTid();
            if (!_prefabsByTid.TryGetValue(tid, out GameObject prefab))
            {
                if (!TryResolvePrefab(tid, out prefab, out string error))
                {
                    ReportConfigurationError(error);
                    return;
                }
                _prefabsByTid[tid] = prefab;
            }

            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPos = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

            GameObject instance = ObjectPoolManager.Instance.Spawn(
                prefab,
                spawnPos,
                Quaternion.identity);
            if (instance == null) return;

            EnemyBrain brain = instance.GetComponent<EnemyBrain>();
            if (brain == null)
            {
                AppLog.Error("Enemy",
                    $"Prefab '{prefab.name}' has no EnemyBrain component.");
                ObjectPoolManager.Instance.Despawn(instance);
                return;
            }

            if (!brain.PrepareForSpawn(tid))
            {
                AppLog.Error("Enemy",
                    $"Failed to prepare pooled monster TID={tid}. The instance was returned to the pool.");
                ObjectPoolManager.Instance.Despawn(instance);
                return;
            }

            _activeEnemies.Add(new ActiveEnemy
            {
                Instance = instance,
                Brain = brain
            });

            AppLog.Info("Enemy",
                $"spawner produced TID={tid} activeSlots={_activeEnemies.Count}/{maxEnemies}");
        }

        /// <summary>按当前刷怪模式选择一个 TID。</summary>
        private int SelectTid()
        {
            if (spawnMode == EnemySpawnSelectionMode.FixedTid)
                return spawnTid;

            return randomTids[Random.Range(0, randomTids.Count)];
        }

        /// <summary>回收超过尸体保留时间的怪物实例。</summary>
        private void ReleaseExpiredEnemies()
        {
            for (int i = _activeEnemies.Count - 1; i >= 0; i--)
            {
                ActiveEnemy entry = _activeEnemies[i];
                if (entry.ReleaseAt < 0f || Time.time < entry.ReleaseAt) continue;

                if (entry.Instance != null)
                    ObjectPoolManager.Instance.Despawn(entry.Instance);

                _activeEnemies.RemoveAt(i);
                AppLog.Info("Enemy",
                    $"spawner recycled a corpse; activeSlots={_activeEnemies.Count}/{maxEnemies}");
            }
        }

        /// <summary>移除被外部销毁的怪物活动槽位。</summary>
        private void PruneDestroyedEnemies()
        {
            for (int i = _activeEnemies.Count - 1; i >= 0; i--)
            {
                if (_activeEnemies[i].Instance != null) continue;
                AppLog.Warning("Enemy",
                    "A spawned monster was destroyed outside the pool. Removing its active slot.");
                _activeEnemies.RemoveAt(i);
            }
        }

        /// <summary>只输出一次刷怪配置错误，避免每帧重复打印。</summary>
        private void ReportConfigurationError(string error)
        {
            if (_configurationErrorReported) return;
            _configurationErrorReported = true;
            AppLog.Error("Enemy", $"Spawner configuration error: {error}");
        }
        #endregion

        #region 事件响应与编辑器显示 (Events & Gizmos)
        /// <summary>订阅怪物死亡事件。</summary>
        private void OnEnable()
        {
            EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
        }

        /// <summary>取消怪物死亡事件订阅。</summary>
        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
        }

        /// <summary>记录死亡怪物的尸体回池时间。</summary>
        private void OnEnemyDied(EnemyDiedEvent evt)
        {
            for (int i = 0; i < _activeEnemies.Count; i++)
            {
                ActiveEnemy entry = _activeEnemies[i];
                if (entry.Brain == null || entry.Brain.Entity == null) continue;
                if (!ReferenceEquals(entry.Brain.Entity, evt.Victim) || entry.ReleaseAt >= 0f)
                    continue;

                float lifetime = EffectiveCorpseLifetime;
                entry.ReleaseAt = Time.time + lifetime;
                AppLog.Info("Enemy",
                    $"{entry.Instance.name} died; returning to pool in {lifetime:F2}s.");
                return;
            }
        }

        /// <summary>在 Scene 视图中绘制刷怪半径。</summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, spawnRadius);
        }
        #endregion
    }
}
