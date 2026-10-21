using System.Collections.Generic;
using Emberwild.Core.AppLog;
using UnityEngine;

namespace Emberwild.AI.Pathfinding
{
    [RequireComponent(typeof(Rigidbody))]
    public class CustomNavAgent : MonoBehaviour
    {
        #region 移动约束常量 (Movement Constraints)
        private const RigidbodyConstraints BaseConstraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationY |
            RigidbodyConstraints.FreezeRotationZ;

        private const RigidbodyConstraints LockedConstraints =
            BaseConstraints |
            RigidbodyConstraints.FreezePositionX |
            RigidbodyConstraints.FreezePositionZ;
        #endregion

        #region 本地核心组件 (Local Components)
        private Rigidbody _rb;
        #endregion

        #region 寻路配置 (Navigation Configuration)
        // 寻路参数
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

        [Tooltip("每隔多少秒重新向 A* 引擎请求一次最新路线")]
        public float pathUpdateInterval = 0.2f;

        [Tooltip("离当前格子多近时提前看向下一个格子")]
        public float nextWaypointDistance = 0.5f;
        #endregion

        #region 寻路运行时状态 (Navigation Runtime State)
        // 当前路径与移动状态
        private List<Node> _currentPath;
        private Vector3 _destination;
        private bool _hasDestination;
        private float _lastPathUpdateTime;
        private bool _noPathReported;
        private float _stalledTime;
        private float _directFallbackUntil;
        private bool _motionLocked;
        #endregion

        #region 移动状态查询 (Movement Queries)
        public bool HasDestination => _hasDestination;
        public bool IsMotionLocked => _motionLocked;
        public int PathNodeCount => _currentPath != null ? _currentPath.Count : 0;
        public Vector3 CurrentVelocity => _rb != null ? _rb.velocity : Vector3.zero;
        public Vector3 Destination => _destination;
        public float VelocityMagnitude => _rb.velocity.magnitude;
        #endregion

        #region Unity 生命周期与出生重置 (Lifecycle & Spawn Reset)
        /// <summary>获取 Rigidbody 并应用基础物理约束。</summary>
        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.constraints = BaseConstraints;
        }

        /// <summary>清除路径、速度和移动锁，供池对象重新出生。</summary>
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
        #endregion

        #region 物理寻路更新 (Physics Path Update)
        /// <summary>按固定物理帧更新寻路和 Rigidbody 速度。</summary>
        private void FixedUpdate()
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

            if (_currentPath == null || _currentPath.Count == 0) return;

            float distanceToWaypoint = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.z),
                new Vector2(_currentPath[0].worldPos.x, _currentPath[0].worldPos.z));

            if (distanceToWaypoint < nextWaypointDistance)
            {
                _currentPath.RemoveAt(0);
                if (_currentPath.Count == 0) return;
            }

            Vector3 nextWaypoint = _currentPath[0].worldPos;
            Vector3 moveDir = (nextWaypoint - transform.position).normalized;
            moveDir.y = 0;

            _rb.velocity = new Vector3(
                moveDir.x * moveSpeed,
                _rb.velocity.y,
                moveDir.z * moveSpeed);

            if (moveDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                _rb.rotation = Quaternion.Slerp(
                    _rb.rotation,
                    targetRot,
                    Time.fixedDeltaTime * turnSpeed);
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

        /// <summary>在没有 A* 路径时沿目标方向直接移动。</summary>
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
        #endregion

        #region 移动指令 (Movement Commands)
        /// <summary>锁定或解锁移动。</summary>
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

        /// <summary>设置下一个寻路目的地。</summary>
        public void SetDestination(Vector3 targetPos)
        {
            if (_motionLocked) return;

            _destination = targetPos;
            _hasDestination = true;
            _noPathReported = false;
            _stalledTime = 0f;
        }

        /// <summary>立即停止移动并清空路径。</summary>
        public void Stop()
        {
            _hasDestination = false;
            _currentPath = null;
            _rb.velocity = new Vector3(0, _rb.velocity.y, 0);
        }

        /// <summary>立即面向指定世界坐标。</summary>
        public void FaceTowards(Vector3 worldTarget)
        {
            if (_rb == null) return;

            Vector3 dir = worldTarget - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;

            _rb.angularVelocity = Vector3.zero;
            _rb.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }

        /// <summary>沿指定方向直接设置水平速度。</summary>
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
        #endregion

        #region 编辑器路径显示 (Gizmos)
        /// <summary>在 Scene 视图中绘制当前 A* 路径。</summary>
        private void OnDrawGizmos()
        {
            if (_currentPath == null || _currentPath.Count == 0) return;

            Gizmos.color = Color.black;
            for (int i = 0; i < _currentPath.Count; i++)
            {
                Gizmos.DrawWireCube(
                    _currentPath[i].worldPos,
                    new Vector3(0.8f, 0.2f, 0.8f));

                if (i < _currentPath.Count - 1)
                {
                    Gizmos.DrawLine(_currentPath[i].worldPos, _currentPath[i + 1].worldPos);
                }
            }
        }
        #endregion
    }
}
