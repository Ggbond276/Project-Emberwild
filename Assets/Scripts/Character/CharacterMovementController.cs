using UnityEngine;
using Emberwild.CameraControl;
using Emberwild.Core.AppLog;
using Emberwild.Input;
using Emberwild.Core.Events;

namespace Emberwild.Character
{
    /// <summary>
    /// 角色移动控制器 — Camera Relative 移动 + Animator Locomotion 驱动。
    ///
    /// 职责:
    /// - 订阅 <see cref="InputReader.OnMove"/>,获得 LS 二维向量。
    /// - 从 <see cref="CameraController"/>.transform 拿 forward / right,投影到 XZ 平面。
    /// - 算相机相对移动方向,以 <see cref="_moveSpeed"/> 修改 transform.position。
    /// - 让 transform.forward 始终朝向移动方向(静止时保留当前朝向)。
    /// - 把 <c>_moveInput.magnitude</c>(0~1) 喂给 CharacterAnimator,驱动 Locomotion Blend Tree。
    ///
    /// 严格不做:
    /// - 不广播 EventBus。
    /// - 不做碰撞检测 / 物理 / 地面检测 / 重力。
    /// - 不引入 Rigidbody / CharacterController。
    /// - 不处理 Sprint / Crouch / Jump / Attack 等其他动画参数。
    /// </summary>
    [DisallowMultipleComponent]
    public class CharacterMovementController : MonoBehaviour
    {


        [Header("攻击")]
        [Tooltip("每段伤害(段位 0~3)")]
        [SerializeField] private float[] _attackDamages = { 10f, 12f, 15f, 20f };

        [Tooltip("每段攻击动画时长(秒);命中帧 = 时长/2")]
        [SerializeField] private float _attackDuration = 0.5f;

        [Tooltip("段间衔接窗口(秒)")]
        [SerializeField] private float _comboWindow = 0.4f;



        [Header("输入")]
        [Tooltip("场景中的 InputReader(挂在 InputManager GameObject 上)")]
        [SerializeField] private InputReader _inputReader;

        [Header("摄像机")]
        [Tooltip("Main Camera 上的 CameraController(用于取相机方向)")]
        [SerializeField] private CameraController _cameraController;

        [Header("动画")]
        [Tooltip("角色动画播放层(挂在角色 GameObject 上)")]
        [SerializeField] private CharacterAnimator _animator;

        [Header("移动")]
        [Tooltip("角色移动速度(米/秒)")]
        [SerializeField] private float _moveSpeed = 5f;

        [Header("物理")]
        [Tooltip("角色物理驱动层 CharacterLocomotionMotor(挂在自身根节点)")]
        [SerializeField] private CharacterLocomotionMotor _motor;

        // 缓存 OnMove 回调值,Update 中读取。
        private Vector2 _moveInput;

        private bool _isAttacking;
        private int _comboIndex;
        private float _attackEndTime;   // 当前攻击结束时间(Time.time)
        private float _comboDeadline;   // 衔接窗口截止时间

        private void Start()
        {
            if (_inputReader == null)
            {
                AppLog.Error("Player", "CharacterMovementController: InputReader is null, please assign in Inspector");
                enabled = false;
                return;
            }

            if (_cameraController == null)
            {
                AppLog.Error("Player", "CharacterMovementController: CameraController is null, please assign in Inspector");
                enabled = false;
                return;
            }

            if (_animator == null)
            {
                AppLog.Warning("Player", "CharacterMovementController: CharacterAnimator is null, locomotion animation will not play");
            }

            if (_motor == null)
            {
                AppLog.Error("Player", "CharacterMovementController: Motor is null, please assign in Inspector");
                enabled = false;
                return;
            }

            _inputReader.OnMove += HandleMove;
            _inputReader.OnAttack += HandleAttack;
            AppLog.Info("Player", "Attack subscribed");
            AppLog.Info("Player", "CharacterMovementController initialized");
        }

        private void OnDestroy()
        {
            if (_inputReader != null)
            {
                _inputReader.OnMove -= HandleMove;
                _inputReader.OnAttack -= HandleAttack;
            }

        }

        private void HandleMove(Vector2 move)
        {
            _moveInput = move;
        }

        private void HandleAttack()
        {
            // 衔接窗口外:从段 0 开始
            // 衔接窗口内:连段(0→1→2→3,满 4 回 0)
            if (Time.time > _comboDeadline || _comboIndex >= 3)
            {
                _comboIndex = 0;
            }
            else
            {
                _comboIndex++;
            }

            _isAttacking = true;
            _attackEndTime = Time.time + _attackDuration;
            _comboDeadline = _attackEndTime + _comboWindow;

            float damage = _attackDamages[Mathf.Clamp(_comboIndex, 0, _attackDamages.Length - 1)];

            // 命中帧 = 动画中点
            var hitPoint = transform.position + transform.forward * 1.2f;
            var hitDir = transform.forward;

            EventBus.Publish(new PlayerAttackedEvent(_comboIndex, hitPoint, hitDir, damage));

            AppLog.Info("Player", $"Attack combo={_comboIndex} damage={damage}");
        }

        private void Update()
        {
            // 攻击中:跳过移动与朝向
            if (_isAttacking)
            {
                if (Time.time >= _attackEndTime)
                {
                    _isAttacking = false;
                }

                // 移动涉及两个方面 1.动画 2.物理速度
                // 这两个需要在攻击硬直状态下锁死
                if (_animator != null && _motor != null && _motor.IsGrounded)
                {
                    _animator.SetMoveSpeed(0f);
                }

                // 【修复Bug 1】：必须强制把物理信箱清零！否则角色在攻击时会继续按照上一帧的速度溜冰。
                if (_motor != null)
                {
                    _motor.DesiredHorizontalVelocity = Vector3.zero;
                }
                return;
            }

            // 拿到手柄的二维坐标输入 比如(1,1)
            var input = _moveInput;
            var magnitude = input.magnitude;

            // 1. 有动画播放器 2.有物理控制器 3.玩家在地面上
            if (_animator != null && _motor != null && _motor.IsGrounded)
            {
                _animator.SetMoveSpeed(magnitude);
            }
            // 空中:显式清零 Speed,防止上一帧残留值在落地前干扰 JumpAir 状态的 Exit 判断。
            else if (_animator != null)
            {
                _animator.SetMoveSpeed(0f);
            }

            // 静止:不移动、不朝向。
            if (magnitude < 0.0001f)
            {
                if (_motor != null) _motor.DesiredHorizontalVelocity = Vector3.zero;
                return;
            }

            // --- 阶段一：读取摄像机视角 ---
            var camForward = _cameraController.transform.forward;
            camForward.y = 0f;
            if (camForward.sqrMagnitude < 0.0001f)
            {
                if (_motor != null) _motor.DesiredHorizontalVelocity = Vector3.zero;
                return;
            }
            camForward.Normalize();

            var camRight = _cameraController.transform.right;
            camRight.y = 0f;
            camRight.Normalize();

            // --- 阶段二：计算三维移动方向 ---
            var moveDir = camRight * input.x + camForward * input.y;
            if (moveDir.sqrMagnitude < 0.0001f)
            {
                if (_motor != null) _motor.DesiredHorizontalVelocity = Vector3.zero;
                return;
            }
            moveDir.Normalize();

            // --- 阶段三：调整主角实体 ---
            if (_motor != null)
            {
                _motor.DesiredHorizontalVelocity = moveDir * _moveSpeed;
            }

            // 【修复Bug 2】：删掉了重复行。强制主角的正脸对准移动方向
            transform.rotation = Quaternion.LookRotation(moveDir, Vector3.up);
        }
    }
}
