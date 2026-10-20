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
    /// <summary>
    /// 定义刷怪器选择怪物 TID 的方式。
    /// </summary>
    public enum EnemySpawnSelectionMode
    {
        /// <summary>
        /// 始终生成 spawnTid 指定的怪物。
        /// </summary>
        FixedTid,

        /// <summary>
        /// 每次生成时从 randomTids 列表中随机选择一只怪物。
        /// </summary>
        RandomTid
    }

    /// <summary>
    /// 定时刷怪组件。负责选择 TID、向对象池申请实例，并在怪物死亡后延迟回收。
    /// 具体怪物状态重置由 EnemyBrain 负责，通用对象池不依赖怪物代码。
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        /// <summary>
        /// 刷怪器当前管理的一只怪物实例及其死亡回收时间。
        /// </summary>
        private sealed class ActiveEnemy
        {
            /// <summary>
            /// 场景中的怪物 GameObject。
            /// </summary>
            public GameObject Instance;

            /// <summary>
            /// 怪物实例上的 AI 主控制器。
            /// </summary>
            public EnemyBrain Brain;

            /// <summary>
            /// 允许回池的时间点；小于零表示怪物尚未死亡。
            /// </summary>
            public float ReleaseAt = -1f;
        }

        [Header("生成设置")]
        /// <summary>
        /// 指定刷怪器使用固定 TID 还是随机 TID 列表。
        /// </summary>
        [Tooltip("指定 TID 或从随机列表中生成。")]
        public EnemySpawnSelectionMode spawnMode = EnemySpawnSelectionMode.FixedTid;

        /// <summary>
        /// FixedTid 模式下唯一使用的怪物 TID。
        /// </summary>
        [Tooltip("FixedTid 模式下要生成的怪物 TID。")]
        public int spawnTid = 2001;

        /// <summary>
        /// RandomTid 模式下可以被随机选中的怪物 TID 列表。
        /// </summary>
        [Tooltip("RandomTid 模式下参与随机选择的怪物 TID 列表。")]
        public List<int> randomTids = new List<int>();

        /// <summary>
        /// 两次成功生成之间需要等待的秒数。
        /// </summary>
        [Tooltip("生成时间间隔(秒)")]
        public float spawnInterval = 3f;

        /// <summary>
        /// 刷怪器允许占用的最大活动槽位数量，尸体等待回池期间同样占用槽位。
        /// </summary>
        [Tooltip("最大同时存在的怪物数量，包含等待回收的尸体")]
        public int maxEnemies = 10;

        /// <summary>
        /// 在刷怪器周围随机选择出生点时使用的水平半径。
        /// </summary>
        [Tooltip("怪物生成的随机半径范围")]
        public float spawnRadius = 5f;

        /// <summary>
        /// 怪物死亡后保留尸体表现、等待对象池回收的秒数。
        /// </summary>
        [Tooltip("怪物死亡后保留尸体并等待回池的时间(秒)")]
        public float corpseLifetime = 3f;

        /// <summary>
        /// 当前由本刷怪器生成且尚未回池的怪物记录。
        /// </summary>
        private readonly List<ActiveEnemy> _activeEnemies = new List<ActiveEnemy>();

        /// <summary>
        /// 按 TID 缓存已经验证过的怪物 Prefab，避免重复解析资源路径。
        /// </summary>
        private readonly Dictionary<int, GameObject> _prefabsByTid =
            new Dictionary<int, GameObject>();

        /// <summary>
        /// 距离下一次生成还需要累计的时间。
        /// </summary>
        private float _timer;

        /// <summary>
        /// 标记当前 TID 和 Prefab 配置是否已经通过验证。
        /// </summary>
        private bool _configurationValidated;

        /// <summary>
        /// 标记配置错误是否已经打印，避免每帧重复报错。
        /// </summary>
        private bool _configurationErrorReported;

        /// <summary>
        /// 获取有效的尸体保留时间；旧场景没有序列化该字段时自动回退为三秒。
        /// </summary>
        private float EffectiveCorpseLifetime =>
            corpseLifetime > 0f ? corpseLifetime : 3f;

        /// <summary>
        /// 启用刷怪器时订阅怪物死亡事件，用于启动尸体回收计时。
        /// </summary>
        private void OnEnable()
        {
            EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
        }

        /// <summary>
        /// 停用刷怪器时取消死亡事件订阅，避免残留回调。
        /// </summary>
        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
        }

        /// <summary>
        /// 每帧处理死亡回池、无效实例清理和下一次怪物生成。
        /// </summary>
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

        /// <summary>
        /// 生成一只怪物，并由对象池负责创建或复用实例。
        /// </summary>
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

        /// <summary>
        /// 根据刷怪模式选择一个本次要生成的 TID。
        /// </summary>
        /// <returns>固定模式返回 spawnTid，随机模式返回随机列表中的一项。</returns>
        private int SelectTid()
        {
            if (spawnMode == EnemySpawnSelectionMode.FixedTid)
                return spawnTid;

            return randomTids[Random.Range(0, randomTids.Count)];
        }

        /// <summary>
        /// 记录所属怪物的死亡时间，让尸体在场景中保留指定时间后再回池。
        /// </summary>
        /// <param name="evt">包含死亡怪物战斗实体引用的死亡事件。</param>
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

        /// <summary>
        /// 回收已经超过尸体保留时间的怪物实例。
        /// </summary>
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

        /// <summary>
        /// 清理被外部错误销毁的实例，避免活动槽位永久占用。
        /// </summary>
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

        /// <summary>
        /// 首次刷怪前验证全部 TID、Prefab 和 EnemyBrain，配置错误只报告一次。
        /// </summary>
        /// <returns>当前刷怪配置完全有效时返回 true。</returns>
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

        /// <summary>
        /// 验证单个 TID 能否解析到带 EnemyBrain 的怪物 Prefab。
        /// </summary>
        /// <param name="tid">要验证并缓存的怪物 TID。</param>
        /// <returns>该 TID 的数据和 Prefab 均有效时返回 true。</returns>
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

        /// <summary>
        /// 通过 CharacterDefine 的 Resource 路径解析 TID 对应的 Prefab。
        /// </summary>
        /// <param name="tid">要解析的怪物 TID。</param>
        /// <param name="prefab">解析成功时输出的怪物 Prefab。</param>
        /// <param name="error">解析失败时输出的可读错误信息。</param>
        /// <returns>能够找到有效数据、资源路径和 EnemyBrain 时返回 true。</returns>
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

        /// <summary>
        /// 打印一次刷怪配置错误，后续帧不再重复输出同一类错误。
        /// </summary>
        /// <param name="error">需要写入控制台的错误原因。</param>
        private void ReportConfigurationError(string error)
        {
            if (_configurationErrorReported) return;
            _configurationErrorReported = true;
            AppLog.Error("Enemy", $"Spawner configuration error: {error}");
        }

        /// <summary>
        /// Inspector 参数被修改时清除配置验证缓存，使下一次刷怪重新检查。
        /// </summary>
        private void OnValidate()
        {
            _configurationValidated = false;
            _configurationErrorReported = false;
            _prefabsByTid.Clear();
        }

        /// <summary>
        /// 在 Scene 视图中绘制刷怪器的随机出生范围。
        /// </summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, spawnRadius);
        }
    }
}
