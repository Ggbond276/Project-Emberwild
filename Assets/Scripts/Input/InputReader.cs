using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Emberwild.Core.AppLog;

namespace Emberwild.Input
{
    /// <summary>
    /// 输入读取器 — 纯输入层。
    ///
    /// 职责:
    /// - 持有 <see cref="PlayerInputActions"/> 实例,生命周期与组件一致。
    /// - 每帧从 <c>Player.Move</c> / <c>Player.Look</c> 读取摇杆值,通过
    ///   <see cref="OnMove"/> / <see cref="OnLook"/> 事件暴露给外部系统。
    ///
    /// 严格不做:
    /// - 角色行为判断(是否允许移动、是否冲刺、是否攻击 ...)
    /// - 修改 Transform / Rigidbody
    /// - 访问 Animator
    /// - 广播 EventBus
    ///
    /// 后续 MovementController / CameraController 通过订阅本组件的事件取得输入。
    /// </summary>
    [DisallowMultipleComponent]
    public class InputReader : MonoBehaviour
    {
        /// <summary>
        /// 移动事件
        /// </summary>
        public event Action<Vector2> OnMove;
        
        /// <summary>
        /// 视角移动事件
        /// </summary>
        public event Action<Vector2> OnLook;

        /// <summary>
        /// 攻击事件
        /// </summary>
        public event Action OnAttack;

        /// <summary>
        /// 闪避事件
        /// </summary>
        public event Action OnDodge;

        /// <summary>
        /// 跳跃事件(按下瞬间触发一次)
        /// </summary>
        public event Action OnJump;

        /// <summary>
        /// 输入动作实例
        /// </summary>
        private PlayerInputActions _actions;

        /// <summary>
        /// 当前移动输入值(由外部按需读取,如 CharacterLocomotionMotor 落地后才读)
        /// </summary>
        public Vector2 CurrentMove => _actions.Player.Move.ReadValue<Vector2>();


        private void Awake()
        {
            _actions = new PlayerInputActions();
            AppLog.Info("Input", "InputReader initialized");
        }

        private void OnEnable()
        {
            _actions.Player.Enable();
            _actions.Player.Attack.started += HandleAttack;
            _actions.Player.Dodge.started += HandleDodge;
            _actions.Player.Jump.started += HandleJump;
        }

        private void OnDisable()
        {
            _actions.Player.Attack.started -= HandleAttack;
            _actions.Player.Dodge.started -= HandleDodge;
            _actions.Player.Jump.started -= HandleJump;
            _actions.Player.Disable();
        }

        private void OnDestroy()
        {
            _actions?.Dispose();
        }



        private void HandleAttack(InputAction.CallbackContext _) => OnAttack?.Invoke();
        private void HandleDodge(InputAction.CallbackContext _) => OnDodge?.Invoke();
        private void HandleJump(InputAction.CallbackContext _) => OnJump?.Invoke();

        

        private void Update()
        {
            OnMove?.Invoke(_actions.Player.Move.ReadValue<Vector2>());
            OnLook?.Invoke(_actions.Player.Look.ReadValue<Vector2>());
        }
    }
}
