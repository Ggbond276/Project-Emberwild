using UnityEngine;
using Emberwild.Core.AppLog;
using Emberwild.Core.Events;

namespace Emberwild.Character
{
    [DisallowMultipleComponent]
    public class CharacterLocomotionMotor : MonoBehaviour
    {
        [Header("物理")]
        [Tooltip("角色 Rigidbody(挂在自身根节点)")]
        [SerializeField] private Rigidbody _rb;

        [Header("动画")]
        [Tooltip("角色 Animator,用于同步 IsGrounded 参数")]
        [SerializeField] private Animator _animator;

        [Tooltip("Animator 上的 IsGrounded bool 参数名")]
        [SerializeField] private string _isGroundedParameter = "IsGrounded";

        [Header("调试")]
        [SerializeField] private bool _logGrounded = false;

        public bool IsGrounded { get; private set; }

        /// <summary>
        /// 水平方向速度分量 由CharacterMovementController 每帧写入 FixedUpdate 合并到 Rigidbody。
        /// </summary>
        public Vector3 DesiredHorizontalVelocity { get; set; }

        // 内部状态。
        private int _isGroundedHash;
        private int _groundContactCount;
        private float _lastGroundedTime = -999f;

        private void Awake()
        {
            if (_rb == null)
            {
                AppLog.Error("Player", "CharacterLocomotionMotor: Rigidbody is null, please assign in Inspector");
                enabled = false;
                return;
            }
            if (_animator == null)
            {
                AppLog.Warning("Player", "CharacterLocomotionMotor: Animator is null, IsGrounded will not sync to Animator");
            }
            else
            {
                _isGroundedHash = Animator.StringToHash(_isGroundedParameter);
            }
        }

        public void DoJump(float height)
        {
            if (_rb == null) return;
            var v = _rb.velocity;
            v.y = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * Mathf.Max(0.01f, height));
            _rb.velocity = v;
            // 跳起瞬间立刻标记离地,避免 DoJump 后一帧内 CapsuleCollider 还在地面
            // OnCollisionStay 又把它判回 grounded(尤其在坡上)。
            _groundContactCount = 0;
            IsGrounded = false;
            if (_animator != null && _isGroundedHash != 0) _animator.SetBool(_isGroundedHash, false);
        }

        private void FixedUpdate()
        {
            if (_rb == null) return;
            // 合并水平速度,保留 Y 轴由物理引擎控制(重力 / 跳跃冲量)。
            var v = _rb.velocity;
            v.x = DesiredHorizontalVelocity.x;
            v.z = DesiredHorizontalVelocity.z;
            _rb.velocity = v;

            // 滞回落地:碰撞回调触发了,但 DoJump 又把标志清掉了,
            // 需要等 _lastGroundedTime 之后才允许 IsGrounded = true。
        }

        private void Update()
        {
            // 任何时候只要当前 _groundContactCount > 0 就视为着地。
            if (_groundContactCount > 0)
            {
                _lastGroundedTime = Time.time;
                if (!IsGrounded)
                {
                    IsGrounded = true;
                    if (_logGrounded) AppLog.Info("Player", "Landed (collision count > 0)");
                }
            }
            else
            {
                // 没碰撞触点就离地。
                if (IsGrounded && Time.time - _lastGroundedTime > 0.05f)
                {
                    IsGrounded = false;
                }
            }
            if (_animator != null && _isGroundedHash != 0)
            {
                _animator.SetBool(_isGroundedHash, IsGrounded);
            }
        }

        private void OnCollisionStay(Collision collision)
        {
            // 只统计"下方"接触:接触点法线 Y > 0.5 视为地面,避免侧墙也判着地。
            foreach (var contact in collision.contacts)
            {
                if (contact.normal.y > 0.5f)
                {
                    _groundContactCount++;
                    return;
                }
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            // 减一可能减到负数,做下限保护。
            if (_groundContactCount > 0) _groundContactCount--;
        }

        private void LateUpdate()
        {
            // 每帧末尾清零 _groundContactCount,因为 OnCollisionStay 在下一物理 tick 之前
            // 不会重新调用,需要主动重置。
            _groundContactCount = 0;
        }
    }
}