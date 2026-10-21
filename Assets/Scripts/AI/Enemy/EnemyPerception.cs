using Emberwild.Combat;
using UnityEngine;

namespace Emberwild.AI.Enemy
{
    public class EnemyPerception : MonoBehaviour
    {
        #region 感知配置与状态 (Perception Configuration & State)
        // 目标层、感知范围与扫描计时
        [Tooltip("要搜索的目标层，留空自动使用 Player 层")]
        [SerializeField] private LayerMask _targetLayer;

        // 1.静态数据
        private float _sightRange = 10f;
        private float _loseTargetRange = 13f;
        private float _scanInterval = 0.2f;

        // 2.运行时状态
        private Transform _currentTarget;
        private float _lastScanTime = -999f;
        #endregion

        #region 感知查询 (Perception Queries)
        public Transform CurrentTarget => _currentTarget;
        #endregion

        #region 感知初始化与配置 (Perception Setup & Configuration)
        /// <summary>
        /// 初始化感知组件使用的目标层。
        /// </summary>
        private void Awake()
        {
            EnsureTargetLayer();
        }

        /// <summary>
        /// 在未配置目标层时自动使用 Player 层。
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
        /// 注入感知距离和扫描间隔。
        /// </summary>
        public void Configure(float sightRange, float loseTargetRange, float scanInterval)
        {
            _sightRange = sightRange;
            _loseTargetRange = loseTargetRange;
            _scanInterval = scanInterval;
        }

        /// <summary>
        /// 清空目标锁定时和扫描时间。
        /// </summary>
        public void ResetForSpawn()
        {
            EnsureTargetLayer();
            _currentTarget = null;
            _lastScanTime = -999f;
        }
        #endregion

        #region 感知更新与扫描 (Perception Update & Scanning)
        /// <summary>
        /// 按扫描间隔寻找目标并把感知结果写入黑板。
        /// </summary>
        public void Tick(EnemyBlackboard bb)
        {
            if (Time.time - _lastScanTime >= _scanInterval)
            {
                _lastScanTime = Time.time;
                Scan();
            }

            if (_currentTarget != null)
            {
                float dist = Vector3.Distance(transform.position, CurrentTarget.position);
                if (dist > _loseTargetRange)
                {
                    _currentTarget = null;
                }
                else
                {
                    bb.DistanceToTarget = dist;
                }
            }

            bb.Target = _currentTarget;
            bb.CanSeeTarget = _currentTarget != null;
            if (_currentTarget == null)
            {
                bb.DistanceToTarget = float.MaxValue;
            }
        }

        /// <summary>
        /// 扫描视野并锁定最近的有效战斗目标。
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
        #endregion

        #region 编辑器可视化 (Gizmos)
        /// <summary>
        /// 在 Scene 视图中绘制视野和丢失目标范围。
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _sightRange);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, _loseTargetRange);
        }
        #endregion
    }
}
