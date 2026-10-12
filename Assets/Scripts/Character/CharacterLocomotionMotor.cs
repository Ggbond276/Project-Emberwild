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

        [Header("动画Animator")]
        [Tooltip("角色动画播放层(挂在角色 GameObject 上)")]
        [SerializeField] private CharacterAnimator _animator;


        // 地面接触次
        private int _groundContactCount;
        // 是否落地
        public bool IsGrounded { get; private set; }
        // 最后一次落地时间
        private float _lastGroundedTime = -999f;


        /// <summary>
        /// 期望水平速度 这个是一个信箱
        /// </summary>
        public Vector3 DesiredHorizontalVelocity { get; set; }

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
                AppLog.Warning("Player", "CharacterLocomotionMotor: CharacterAnimator is null, IsGrounded will not sync to Animator");
            }
        }

        /// <summary>
        /// 统一物理出口:唯一允许直接写 _rb.velocity 的地方。
        /// 外部一律通过 SetHoriziontalVelocity / SetVerticalJumpVelocity 进入。
        /// </summary>
        private void ApplyVelocity(Vector3 v)
        {
            if (_rb == null) return;
            _rb.velocity = v;
        }

        /// <summary>
        /// 设置期望水平速度(信箱)。MovementController 写入,Motor.FixedUpdate 合并到 Rigidbody.xz。
        /// 保留 Rigidbody.y,由物理引擎(重力 / 跳跃冲量)管理。
        /// </summary>
        public void SetHoriziontalVelocity(Vector2 horizontal)
        {
            DesiredHorizontalVelocity = new Vector3(horizontal.x, 0f, horizontal.y);
        }

        /// <summary>
        /// 设置期望垂直速度
        /// </summary>
        /// <param name="height"></param>   
        public void SetVerticalVelocity(float height)
        {
            if (_rb == null) return;
            // 跳跃冲量:直接覆盖 v.y,水平速度保留当前 Rigidbody 值(尊重玩家输入)。
            var v = _rb.velocity;
            v.y = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * Mathf.Max(0.01f, height));
            ApplyVelocity(v);
            _groundContactCount = 0;
            IsGrounded = false;
            if (_animator != null) _animator.SetGrounded(false);
        }

        private void FixedUpdate()
        {

            // 任何时候只要当前 _groundContactCount > 0 就视为着地。
            if (_groundContactCount > 0)
            {
                _lastGroundedTime = Time.time;
                if (!IsGrounded)
                {
                    IsGrounded = true;
                    // 通知订阅者(跳跃系统):落地,清残留 Trigger + 重置可跳次数。
                    EventBus.Publish(new LandedEvent(transform.position));
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
            if (_animator != null)
            {
                _animator.SetGrounded(IsGrounded);
            }

            _groundContactCount = 0;


            if (_rb == null) return;
            // 合并水平速度,保留 Y 轴由物理引擎控制
            var v = _rb.velocity;
            v.x = DesiredHorizontalVelocity.x;
            v.z = DesiredHorizontalVelocity.z;
            ApplyVelocity(v);
        }

        /// <summary>
        /// 碰撞持续阶段 每一帧都会触发
        /// </summary>
        /// <param name="collision"></param>
        private void OnCollisionStay(Collision collision)
        {
            // 只统计"下方"接触:接触点法线 Y > 0.5 视为地面,避免侧墙也判着地。
            foreach (var contact in collision.contacts)
            {
                // contact.normal.y > 0.5 判定为是接触地面的
                if (contact.normal.y > 0.5f)
                {
                    // 接触地面的计数增加
                    _groundContactCount++;
                    return;
                }
            }
        }

        private void Update()
        {
            // 只负责在渲染帧向 Animator 同步最新的状态，不再参与逻辑计算
            if (_animator != null)
            {
                _animator.SetGrounded(IsGrounded);
            }
        }

    }
}