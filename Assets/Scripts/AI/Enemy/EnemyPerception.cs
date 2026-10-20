using Emberwild.Combat;
using UnityEngine;

namespace Emberwild.AI.Enemy
{
    /// <summary>
    /// 感知：定时扫描玩家并写入黑板。
    /// </summary>
    public class EnemyPerception : MonoBehaviour
    {
        [Tooltip("要搜索的目标层，留空自动使用 Player 层")]
        [SerializeField] private LayerMask _targetLayer;

        private float _sightRange = 10f;
        private float _loseTargetRange = 13f;
        private float _scanInterval = 0.2f;

        private Transform _currentTarget;
        private float _lastScanTime = -999f;

        public Transform CurrentTarget => _currentTarget;

        /// <summary>
        /// 未配置目标层时自动取 Player，避免 OverlapSphere 扫 0 层永远返回空。
        /// </summary>
        private void EnsureTargetLayer()
        {
            if (_targetLayer.value != 0) return;

            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
            {
                _targetLayer = 1 << playerLayer;
            }
            else
            {
                Debug.LogWarning($"[EnemyPerception] {name}：找不到 Player 层，请在 Inspector 手动指定目标层");
            }
        }

        /// <summary>
        /// 初始化感知组件需要的目标层。
        /// </summary>
        private void Awake()
        {
            EnsureTargetLayer();
        }

        /// <summary>
        /// 按扫描间隔更新目标，并把感知结果写入怪物黑板。
        /// </summary>
        /// <param name="bb">当前怪物共享的黑板数据。</param>
        public void Tick(EnemyBlackboard bb)
        {
            if(Time.time - _lastScanTime >=  _scanInterval)
            {
                _lastScanTime = Time.time;
                Scan();
            }

            if(_currentTarget != null)
            {
                float dist = Vector3.Distance(transform.position, CurrentTarget.position);
                if(dist > _loseTargetRange)
                {
                    _currentTarget = null;
                } else
                {
                    bb.DistanceToTarget = dist;
                }
            }

            bb.Target = _currentTarget;
            bb.CanSeeTarget = _currentTarget != null;
            if(_currentTarget == null)
            {
                bb.DistanceToTarget = float.MaxValue;
            }
        }

        /// <summary>
        /// 球形扫描，取最近的有效战斗实体。
        /// </summary>
        private void Scan()
        {
            EnsureTargetLayer();
            if (_targetLayer.value == 0) return;

            Collider[] hits = Physics.OverlapSphere(transform.position, _sightRange, _targetLayer);

            Transform best = null;
            float bestSqr = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider col = hits[i];
                if (col == null) continue;

                // 只认带 CombatAttribute 的，避免选中装饰物
                CombatAttribute attr = col.GetComponentInParent<CombatAttribute>();
                if (attr == null || attr.IsDead) continue;

                float sqr = (col.transform.position - transform.position).sqrMagnitude;
                if (sqr >= bestSqr) continue;

                best = col.transform;
                bestSqr = sqr;
            }

            if (best != null)
            {
                bool first = _currentTarget == null;
                _currentTarget = best;

                if (first)
                    Debug.Log($"[Perception] {name} 锁定目标 {best.name}");
            }
        }

        /// <summary>
        /// 从怪物外联表注入感知距离和扫描频率。
        /// </summary>
        /// <param name="sightRange">首次发现目标时使用的视野半径。</param>
        /// <param name="loseTargetRange">已经锁定目标后允许保持锁定的距离。</param>
        /// <param name="scanInterval">两次主动扫描之间等待的秒数。</param>
        public void Configure(float sightRange, float loseTargetRange, float scanInterval)
        {
            _sightRange = sightRange;
            _loseTargetRange = loseTargetRange;
            _scanInterval = scanInterval;
        }

        /// <summary>
        /// 清除上一条生命周期的目标和扫描时间，确保池对象从空感知状态开始。
        /// </summary>
        public void ResetForSpawn()
        {
            EnsureTargetLayer();
            _currentTarget = null;
            _lastScanTime = -999f;
        }

        /// <summary>
        /// 编辑器可视化：黄圈表示视野，蓝圈表示丢失目标距离。
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _sightRange);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, _loseTargetRange);
        }
    }
}
