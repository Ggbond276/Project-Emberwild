using UnityEngine;
using Emberwild.CameraControl;
using Emberwild.Core.AppLog;
using Emberwild.Input;

namespace Emberwild.Character
{
    /// <summary>
    /// 角色移动控制器 — Camera Relative 移动 + Animator Locomotion 驱动。
    ///
    /// 职责:
    /// - 订阅 <see cref="InputReader.OnMove"/>,获得 LS 二维向量。
    /// - 从 <see cref="CameraController"/>.transform 拿 forward / right,投影到 XZ 平面。
    /// - 算相机相对移动方向,写入 <see cref="CharacterLocomotionMotor.DesiredHorizontalVelocity"/>。
    /// - 让 transform.forward 始终朝向移动方向(静止时保留当前朝向)。
    /// - 把 <c>_moveInput.magnitude</c>(0~1) 喂给 CharacterAnimator,驱动 Locomotion Blend Tree。
    /// - 读 CharacterStateController.CanMove 决定是否锁定移动。
    ///
    /// 严格不做:
    /// - 不广播 EventBus。
    /// - 不做碰撞检测 / 物理 / 地面检测 / 重力。
    /// - 不引入 Rigidbody / CharacterController。
    /// - 不处理 Sprint / Crouch / Jump / Attack 等其他动画参数。
    /// - 不持有 CharacterCombat / CharacterJumpController 引用。
    /// </summary>
    [DisallowMultipleComponent]
    public class CharacterMovementController : MonoBehaviour
    {
        [Header("输入")]
        [Tooltip("场景中的 InputReader(挂在 InputManager GameObject 上)")]
        [SerializeField] private InputReader _inputReader;

        [Header("摄像机")]
        [Tooltip("Main Camera 上的 CameraController(用于取相机方向)")]
        [SerializeField] private CameraController _cameraController;

        [Header("动画")]
        [Tooltip("角色动画播放层(挂在角色 GameObject 上)")]
        [SerializeField] private CharacterAnimator _animator;

        [Header("状态")]
        [Tooltip("角色状态协调器(用于读 CanMove 决定是否锁移动)")]
        [SerializeField] private CharacterStateController _state;

        [Header("移动")]
        [Tooltip("角色移动速度(米/秒)")]
        [SerializeField] private float _moveSpeed = 5f;

        [Header("物理")]
        [Tooltip("角色物理驱动层 CharacterLocomotionMotor(挂在自身根节点)")]
        [SerializeField] private CharacterLocomotionMotor _motor;

        // 缓存 OnMove 回调值,Update 中读取。
        private Vector2 _moveInput;

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
            AppLog.Info("Player", "CharacterMovementController initialized");
        }

        private void OnDestroy()
        {
            if (_inputReader != null)
            {
                _inputReader.OnMove -= HandleMove;
            }
        }

        private void HandleMove(Vector2 move)
        {
            _moveInput = move;
        }

        private void Update()
        {
            // 状态机锁定时:停止移动与朝向,但不缓存(下一帧自然恢复)。
            if (_state != null && !_state.CanMove)
            {
                LockMovementOutput();
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

            // 强制主角的正脸对准移动方向
            transform.rotation = Quaternion.LookRotation(moveDir, Vector3.up);
        }

        /// <summary>
        /// 锁定移动输出:清零 Animator Speed 与物理信箱速度,角色原地停留。
        /// </summary>
        private void LockMovementOutput()
        {
            // 攻击硬直时:清 Animator Speed(仅在地面避免污染空中 JumpAir)。
            if (_animator != null && _motor != null && _motor.IsGrounded)
            {
                _animator.SetMoveSpeed(0f);
            }
            // 强制把物理信箱清零！否则角色在攻击时会继续按照上一帧的速度溜冰。
            if (_motor != null)
            {
                _motor.DesiredHorizontalVelocity = Vector3.zero;
            }
        }
    }
}
