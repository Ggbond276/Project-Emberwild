using Emberwild.Core.AppLog;
using Emberwild.Core.Events;
using Emberwild.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Emberwild.Character
{
    public class CharacterJumpController : MonoBehaviour
    {
        [Header("输入")]
        [Tooltip("场景中的 InputReader(挂在 InputManager GameObject 上)")]
        [SerializeField] private InputReader _inputReader;

        [Header("物理")]
        [Tooltip("角色物理驱动层(用于 IsGrounded 读取与 SetVerticalJumpVelocity 调用)")]
        [SerializeField] private CharacterLocomotionMotor _motor;

        [Header("动画")]
        [Tooltip("角色动画播放层(挂在角色 GameObject 上)")]
        [SerializeField] private CharacterAnimator _animator;

        [Header("状态")]
        [Tooltip("角色状态协调器(用于读 CanJump 决定是否锁跳跃)")]
        [SerializeField] private CharacterStateController _state;

        [Header("跳跃参数")]
        [Tooltip("一段跳高度(米)")]
        [SerializeField] private float _jumpHeight = 2f;

        // TODO:二段跳 — 暂不启用,手感调试结果不理想,后续再开放
        [Tooltip("二段跳高度(米) — TODO:暂不启用")]
        [SerializeField] private float _doubleJumpHeight = 2f;

        // TODO:二段跳 — 暂不启用,手感调试结果不理想,后续再开放
        [Tooltip("空中可再跳次数 — TODO:暂不启用")]
        [SerializeField] private int _maxAirJumps = 0;

        private int _airJumpsRemaining;     // 空中剩余可跳次数(TODO:二段跳恢复时启用)

        private void Start()
        {
            if (_inputReader == null)
            {
                AppLog.Error("Player", "CharacterJumpController: InputReader is null, please assign in Inspector");
                enabled = false;
                return;
            }
            if (_motor == null)
            {
                AppLog.Error("Player", "CharacterJumpController: Motor is null, please assign in Inspector");
                enabled = false;
                return;
            }
            if (_animator == null)
            {
                AppLog.Warning("Player", "CharacterJumpController: CharacterAnimator is null, jump animation will not play");
            }
            _airJumpsRemaining = _maxAirJumps;
            _inputReader.OnJump += HandleJump;
            EventBus.Subscribe<LandedEvent>(OnLanded);
            AppLog.Info("Player", "CharacterJumpController initialized");
        }

        private void OnDestroy()
        {
            if (_inputReader != null)
            {
                _inputReader.OnJump -= HandleJump;
            }
            EventBus.Unsubscribe<LandedEvent>(OnLanded);
        }

        private void HandleJump()
        {
            if (_state != null && !_state.CanJump) return;
            if (_motor == null) return;
            if (_motor.IsGrounded)
            {
                // 地面跳。
                if (_animator != null) _animator.PlayJumpStart();
                _motor.SetVerticalVelocity(_jumpHeight);
                _airJumpsRemaining = _maxAirJumps;
                EventBus.Publish(new JumpStartedEvent(transform.position));
            }
            // TODO:二段跳 — 暂不启用,手感调试结果不理想,后续再开放
            // else if (_airJumpsRemaining > 0)
            // {
            //     // 空中跳(二段跳)。
            //     if (_animator != null) _animator.PlayDoubleJump();
            //     _motor.SetVerticalVelocity(_doubleJumpHeight);
            //     _airJumpsRemaining--;
            //     EventBus.Publish(new DoubleJumpStartedEvent(transform.position));
            // }
            // 现状:空中按 jump 键不响应(_maxAirJumps 已为 0,if/else 链落到此处直接退出)。
        }
        private void OnLanded(LandedEvent _)
        {
            // 落地归零,允许再次一段跳。
            _airJumpsRemaining = _maxAirJumps;
            // TODO:二段跳 — 暂不启用,手感调试结果不理想,后续再开放
            // 清理残留的 DoubleJump Trigger,避免落地后下一帧被动画机误消费。
            // if (_animator != null) _animator.ResetDoubleJump();
            // 清理残留的 Jump Trigger,避免落地瞬间 JumpAir 状态误切回 Locomotion。
            if (_animator != null) _animator.ResetJump();
        }



    }
}
