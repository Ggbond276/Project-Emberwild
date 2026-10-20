using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emberwild.Core.AppLog;
using UnityEngine;

namespace Emberwild.AI.Pathfinding
{
    /// <summary>
    /// 怪物使用的轻量 A* 移动代理，统一维护寻路、直线位移和移动锁定。
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CustomNavAgent : MonoBehaviour
    {
        [Header("寻路参数")]
        [HideInInspector]
        [Tooltip("实体的移动速度")]
        public float moveSpeed = 4f;
        [HideInInspector]
        [Tooltip("实体转向目标方向的平滑速度")]
        public float turnSpeed = 10f;
        [HideInInspector]
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
        private bool _noPathReported;
        private float _stalledTime;
        private float _directFallbackUntil;

        /// <summary>
        /// 当前是否持有一个需要移动前往的目的地。
        /// </summary>
        public bool HasDestination => _hasDestination;

        /// <summary>
        /// 当前移动是否被攻击、受击等状态锁定。
        /// </summary>
        public bool IsMotionLocked => _motionLocked;

        /// <summary>
        /// 当前 A* 路径中剩余节点的数量。
        /// </summary>
        public int PathNodeCount => _currentPath != null ? _currentPath.Count : 0;

        /// <summary>
        /// 当前 Rigidbody 的实际速度。
        /// </summary>
        public Vector3 CurrentVelocity => _rb != null ? _rb.velocity : Vector3.zero;

        /// <summary>
        /// 当前寻路请求指定的最终世界坐标。
        /// </summary>
        public Vector3 Destination => _destination;

        /// <summary>
        /// 移动锁：锁定期间不接收任何位移，用于攻击、受击和死亡等定身场景。
        /// </summary>
        private bool _motionLocked;

        /// <summary>
        /// 基础约束：只冻结旋转，位置可动。
        /// </summary>
        private const RigidbodyConstraints BaseConstraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationY |
            RigidbodyConstraints.FreezeRotationZ;

        /// <summary>
        /// 锁定约束：在基础约束上再冻结水平位移，Y 轴保留以维持重力。
        /// </summary>
        private const RigidbodyConstraints LockedConstraints =
            BaseConstraints |
            RigidbodyConstraints.FreezePositionX |
            RigidbodyConstraints.FreezePositionZ;

        /// <summary>
        /// 获取当前实体的实际移动速度向量长度。
        /// 通常用于传递给 Animator 动画状态机（如 animator.SetFloat("Speed", agent.VelocityMagnitude)）以驱动跑动动画。
        /// </summary>
        public float VelocityMagnitude => _rb.velocity.magnitude;


        /// <summary>
        /// 缓存 Rigidbody 并应用基础物理约束。
        /// </summary>
        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.constraints = BaseConstraints;
        }

        /// <summary>
        /// 锁定/解锁移动。锁定后完全定在原地，不受自己的速度也不受外力推挤。
        /// 攻击、受击、施法、姿态这类"必须站住"的状态在 OnEnter 锁、OnExit 解锁。
        /// </summary>
        /// <param name="locked">true 表示停止并冻结水平移动；false 表示恢复正常移动。</param>
        public void SetMotionLocked(bool locked)
        {
            if (_rb == null) return;

            _motionLocked = locked;

            if (locked)
            {
                _hasDestination = false;
                _currentPath = null;
                _rb.velocity = new Vector3(0f, _rb.velocity.y, 0f);
                _rb.angularVelocity = Vector3.zero;
                _rb.constraints = LockedConstraints;
            }
            else
            {
                _rb.constraints = BaseConstraints;
            }
        }

        /// <summary>
        /// 【指令】命令实体开始向目标坐标移动。
        /// 内部会自动启动定时寻路计算，并在抵达 stoppingDistance 距离后自动停止。
        /// </summary>
        /// <param name="targetPos">最终要去的世界坐标</param>
        public void SetDestination(Vector3 targetPos)
        {
            if (_motionLocked) return;

            _destination = targetPos;
            _hasDestination = true;
            _noPathReported = false;
            _stalledTime = 0f;
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

        /// <summary>
        /// 【指令】立即面向目标世界坐标(仅绕 Y 轴瞬时转向)。
        /// 供攻击等"需要瞬间转正朝向"的场景使用;移动中的平滑转向由 FixedUpdate 的 Slerp 负责,这里是一次性硬转。
        /// 同时清零角速度,避免残留角速度引起自旋。
        /// </summary>
        /// <param name="worldTarget">要面向的世界坐标(通常是玩家位置)</param>
        public void FaceTowards(Vector3 worldTarget)
        {
            if (_rb == null) return;

            Vector3 dir = worldTarget - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;

            // 清掉可能残留的角速度,再直接锁定朝向,杜绝自旋
            _rb.angularVelocity = Vector3.zero;
            _rb.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }

        /// <summary>
        /// 绕过 A*，直接沿指定方向设置水平速度，主要用于路径失败时的追击兜底。
        /// </summary>
        /// <param name="direction">期望移动的水平方向，Y 分量会被忽略。</param>
        /// <param name="speed">沿该方向施加的水平移动速度。</param>
        public void SetDirectVelocity(Vector3 direction, float speed)
        {
            if (_rb == null || _motionLocked) return;

            Vector3 dir = direction;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            dir.Normalize();

            _rb.angularVelocity = Vector3.zero;
            _rb.rotation = Quaternion.LookRotation(dir, Vector3.up);
            _rb.velocity = new Vector3(dir.x * speed, _rb.velocity.y, dir.z * speed);
        }

        /// <summary>
        /// 清除上一条生命周期的路径、速度和移动锁，供池对象重新出生时使用。
        /// </summary>
        public void ResetForSpawn()
        {
            if (_rb == null)
                _rb = GetComponent<Rigidbody>();

            _hasDestination = false;
            _currentPath = null;
            _destination = Vector3.zero;
            _lastPathUpdateTime = 0f;
            _noPathReported = false;
            _stalledTime = 0f;
            _directFallbackUntil = 0f;

            if (_rb != null)
            {
                _rb.velocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.constraints = BaseConstraints;
            }

            SetMotionLocked(true);
        }

        /// <summary>
        /// 按固定物理帧更新寻路结果、航点切换和 Rigidbody 速度。
        /// </summary>
        void FixedUpdate()
        {
            if (_motionLocked) return;
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
                if (_currentPath == null || _currentPath.Count == 0)
                {
                    if (!_noPathReported)
                    {
                        AppLog.Warning("Pathfinding",
                            $"{name}: no A* path to {_destination}. Check grid bounds and obstacle mask.");
                        _noPathReported = true;
                    }
                    _directFallbackUntil = Time.time + pathUpdateInterval;
                }
                else
                {
                    _noPathReported = false;
                }
            }

            if (Time.time < _directFallbackUntil)
            {
                MoveDirectlyToDestination();
                return;
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

                float horizontalSpeed = new Vector2(_rb.velocity.x, _rb.velocity.z).magnitude;
                _stalledTime = horizontalSpeed < 0.05f
                    ? _stalledTime + Time.fixedDeltaTime
                    : 0f;
                if (_stalledTime > 0.5f)
                {
                    AppLog.Warning("Pathfinding",
                        $"{name}: path exists but movement is blocked. Using direct chase fallback.");
                    _currentPath = null;
                    _stalledTime = 0f;
                    _directFallbackUntil = Time.time + pathUpdateInterval;
                }
            }
        }

        /// <summary>
        /// 在没有有效 A* 路径时，沿目标方向执行直接追击。
        /// </summary>
        private void MoveDirectlyToDestination()
        {
            Vector3 direction = _destination - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;

            direction.Normalize();
            _rb.velocity = new Vector3(
                direction.x * moveSpeed,
                _rb.velocity.y,
                direction.z * moveSpeed);
            _rb.rotation = Quaternion.Slerp(
                _rb.rotation,
                Quaternion.LookRotation(direction, Vector3.up),
                Time.fixedDeltaTime * turnSpeed);
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
                    // 使用 DrawWireCube 绘制镂空的线框方块，完美解决遮挡问题
                    Gizmos.DrawWireCube(_currentPath[i].worldPos, new Vector3(0.8f, 0.2f, 0.8f));

                    if (i < _currentPath.Count - 1)
                    {
                        Gizmos.DrawLine(_currentPath[i].worldPos, _currentPath[i + 1].worldPos);
                    }
                }
            }
        }
    }
}
