using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Emberwild.AI.Pathfinding
{
    public class CustomNavAgent : MonoBehaviour
    {
        [Header("寻路参数")]
        [Tooltip("实体的移动速度")]
        public float moveSpeed = 4f;
        [Tooltip("实体转向目标方向的平滑速度")]
        public float turnSpeed = 10f;
        [Tooltip("到达目标多近的距离时自动刹车")]
        public float stoppingDistance = 1.5f;
        [Tooltip("每隔多少秒重新向 A* 引擎请求一次最新路线 (值越大性能越好，但反应越迟钝)")]
        public float pathUpdateInterval = 0.2f;

        // 新增这个参数
        [Tooltip("【平滑优化】航点切换距离：离当前格子多近时提前看向下一个格子(值越大转弯越圆滑)")]
        public float nextWaypointDistance = 0.5f;


        private Rigidbody _rb;
        private List<Node> _currentPath;
        private Vector3 _destination;
        private bool _hasDestination = false;
        private float _lastPathUpdateTime;

        /// <summary>
        /// 获取当前实体的实际移动速度向量长度。
        /// 通常用于传递给 Animator 动画状态机（如 animator.SetFloat("Speed", agent.VelocityMagnitude)）以驱动跑动动画。
        /// </summary>
        public float VelocityMagnitude => _rb.velocity.magnitude;


        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        /// <summary>
        /// 【指令】命令实体开始向目标坐标移动。
        /// 内部会自动启动定时寻路计算，并在抵达 stoppingDistance 距离后自动停止。
        /// </summary>
        /// <param name="targetPos">最终要去的世界坐标</param>
        public void SetDestination(Vector3 targetPos)
        {
            _destination = targetPos;
            _hasDestination = true;
        }

        /// <summary>
        /// 【指令】命令实体立即紧急刹车。
        /// 会清空当前正在追踪的路线，并消除水平面的移动惯性，但保留重力下落表现。
        /// </summary>
        public void Stop()
        {
            _hasDestination = false;
            _currentPath = null;
            _rb.velocity = new Vector3(0, _rb.velocity.y, 0);
        }

        void FixedUpdate()
        {
            if (!_hasDestination) return;

            float distanceToTarget = Vector3.Distance(transform.position, _destination);
            if (distanceToTarget <= stoppingDistance)
            {
                Stop();
                return;
            }

            if (Time.time - _lastPathUpdateTime > pathUpdateInterval)
            {
                _currentPath = AStarAlgorithm.FindPath(transform.position, _destination);
                _lastPathUpdateTime = Time.time;
            }

            // 3. 按照算好的路进行物理移动
            if (_currentPath != null && _currentPath.Count > 0)
            {
                // 【核心优化：吃航点机制】
                // 只计算水平面 (X和Z轴) 的距离，忽略身高导致的 Y 轴误差
                float distanceToWaypoint = Vector2.Distance(
                    new Vector2(transform.position.x, transform.position.z),
                    new Vector2(_currentPath[0].worldPos.x, _currentPath[0].worldPos.z)
                );

                // 如果离当前目标格子已经足够近了，直接把它从路径里踢掉，准备去下一个点！
                if (distanceToWaypoint < nextWaypointDistance)
                {
                    _currentPath.RemoveAt(0);
                    // 剔除后如果发现已经没路了（到终点了），直接返回
                    if (_currentPath.Count == 0) return;
                }

                // 获取(更新后的)下一个目标点
                Vector3 nextWaypoint = _currentPath[0].worldPos;
                Vector3 moveDir = (nextWaypoint - transform.position).normalized;
                moveDir.y = 0; // 锁死 Y 轴，防止上天入地

                _rb.velocity = new Vector3(moveDir.x * moveSpeed, _rb.velocity.y, moveDir.z * moveSpeed);

                if (moveDir != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                    _rb.rotation = Quaternion.Slerp(_rb.rotation, targetRot, Time.fixedDeltaTime * turnSpeed);
                }
            }
        }

        /// <summary>
        /// 编辑器可视化：让带有此 Agent 的实体，能在 Scene 窗口中自动画出属于自己的独立路线。
        /// </summary>
        void OnDrawGizmos()
        {
            if (_currentPath != null && _currentPath.Count > 0)
            {
                Gizmos.color = Color.black;
                for (int i = 0; i < _currentPath.Count; i++)
                {
                    Gizmos.DrawCube(_currentPath[i].worldPos, new Vector3(0.8f, 0.2f, 0.8f));
                    if (i < _currentPath.Count - 1)
                    {
                        Gizmos.DrawLine(_currentPath[i].worldPos, _currentPath[i + 1].worldPos);
                    }
                }
            }
        }
    }
}
