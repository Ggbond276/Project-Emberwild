using Emberwild.AI.Pathfinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Emberwild.AI
{
    public class EnemyAI : MonoBehaviour
    {
        // 定义怪物的极简状态机（未来可基于此扩展攻击、巡逻状态）
        public enum State { Idle, Chase }

        [Header("索敌设置")]
        [Tooltip("怪物多远能看到玩家")]
        public float sightRange = 10f;
        [Tooltip("玩家所在的物理层级")]
        public LayerMask playerLayer;
        [Tooltip("雷达扫描频率(秒)。不需要每帧扫描，节省性能")]
        public float scanInterval = 0.5f;

        private CustomNavAgent _agent;
        private State _currentState = State.Idle;
        private Transform _targetPlayer;
        private float _lastScanTime;

        void Awake()
        {
            // 自动获取挂在自己身上的那双“寻路的腿”
            _agent = GetComponent<CustomNavAgent>();
        }

        void Update()
        {
            // 定时雷达扫描（性能优化：拒绝在 Update 里每帧做物理 Overlap）
            if (Time.time - _lastScanTime > scanInterval)
            {
                ScanForPlayer();
                _lastScanTime = Time.time;
            }

            // FSM 状态机逻辑流转
            switch (_currentState)
            {
                case State.Idle:
                    // 发呆状态下，如果雷达锁定了玩家，立刻切入追击状态
                    if (_targetPlayer != null)
                    {
                        _currentState = State.Chase;
                    }
                    break;

                case State.Chase:
                    // 如果玩家跑出了视野，丢失仇恨，变回发呆
                    if (_targetPlayer == null || Vector3.Distance(transform.position, _targetPlayer.position) > sightRange)
                    {
                        _targetPlayer = null;
                        _currentState = State.Idle;
                        _agent.Stop(); // 命令底层系统刹车
                    }
                    else
                    {
                        // 持续把玩家的最新坐标下达给寻路组件
                        _agent.SetDestination(_targetPlayer.position);
                    }
                    break;
            }

        }

        /// <summary>
        /// 雷达扫描核心逻辑
        /// </summary>
        private void ScanForPlayer()
        {
            // 以怪物自身为圆心，画一个球形雷达，只过滤 playerLayer
            Collider[] hits = Physics.OverlapSphere(transform.position, sightRange, playerLayer);

            if (hits.Length > 0)
            {
                // 假设扫到的第一个就是玩家
                _targetPlayer = hits[0].transform;
            }
        }

        /// <summary>
        /// 编辑器可视化：画出怪物的视野范围，方便你调节平衡性
        /// </summary>
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, sightRange);
        }

    }
}
