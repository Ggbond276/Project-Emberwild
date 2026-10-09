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



        private PlayerInputActions _actions;


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
        }

        private void OnDisable()
        {
            _actions.Player.Attack.started -= HandleAttack;
            _actions.Player.Dodge.started -= HandleDodge;
            _actions.Player.Disable();
        }

        private void OnDestroy()
        {
            _actions?.Dispose();
        }


        /// <summary>
        /// 抛出事件给订阅OnAttack的处理
        /// </summary>
        /// <param name="_"></param>
        private void HandleAttack(InputAction.CallbackContext _) => OnAttack?.Invoke();
        /// <summary>
        /// 抛出事件给订阅OnDodge的处理
        /// </summary>
        /// <param name="_"></param>
        private void HandleDodge(InputAction.CallbackContext _) => OnDodge?.Invoke();

        private void Update()
        {
            OnMove?.Invoke(_actions.Player.Move.ReadValue<Vector2>());
            OnLook?.Invoke(_actions.Player.Look.ReadValue<Vector2>());
        }
    }
}
