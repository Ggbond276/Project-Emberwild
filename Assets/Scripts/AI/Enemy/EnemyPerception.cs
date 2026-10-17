using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Emberwild.AI.Enemy
{
    public class EnemyPerception : MonoBehaviour
    {
        [Header("视野")]
        [Tooltip("能看到玩家的最大距离")]
        [SerializeField] private float _sightRange = 10f;

        [Tooltip("丢失目标距离。比 _sightRange 大一点,形成滞后区间,防止在边界反复切换")]
        [SerializeField] private float _loseTargetRange = 13f;

        [Tooltip("要搜索的目标层级(玩家所在层)")]
        [SerializeField] private LayerMask _targetLayer;

        [Tooltip("雷达扫描间隔(秒)。不需要每帧扫,省性能")]
        [SerializeField] private float _scanInterval = 0.2f;

        private Transform _currentTarget;
        private float _lastScanTime = -999f;

        public Transform CurrentTarget => _currentTarget;

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

        private void Scan()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, _sightRange, _targetLayer);

            if(hits.Length > 0)
            {
                _currentTarget = hits[0].transform;
            }
        }

        /// <summary>
        /// 外部调用可注入配置
        /// </summary>
        /// <param name="sightRange"></param>
        /// <param name="loseTargetRange"></param>
        /// <param name="scanInterval"></param>
        public void Configure(float sightRange, float loseTargetRange, float scanInterval)
        {
            _sightRange = sightRange;
            _loseTargetRange = loseTargetRange;
            _scanInterval = scanInterval;
        }

        /// <summary>
        /// 编辑器可视化 ：黄圈 = 视野;  蓝圈 = 丢失目标
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
