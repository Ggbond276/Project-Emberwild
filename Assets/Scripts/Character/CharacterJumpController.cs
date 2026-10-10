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
        [Tooltip("角色物理驱动层(用于 IsGrounded 读取与 DoJump 调用)")]
        [SerializeField] private CharacterLocomotionMotor _motor;

        [Header("动画")]
        [Tooltip("角色 Animator(用于 SetTrigger Jump / DoubleJump)")]
        [SerializeField] private Animator _animator;

        [Tooltip("Animator 上的 Jump Trigger 参数名")]
        [SerializeField] private string _jumpTrigger = "Jump";

        [Tooltip("Animator 上的 DoubleJump Trigger 参数名")]
        [SerializeField] private string _doubleJumpTrigger = "DoubleJump";

        [Header("跳跃参数")]
        [Tooltip("一段跳高度(米)")]
        [SerializeField] private float _jumpHeight = 2f;

        [Tooltip("二段跳高度(米)")]
        [SerializeField] private float _doubleJumpHeight = 2f;

        [Tooltip("空中可再跳次数(默认 1,即允许二段跳)")]
        [SerializeField] private int _maxAirJumps = 1;

        private int _airJumpsRemaining;     // 空中剩余可跳次数
        private int _jumpHash;
        private int _doubleJumpHash;

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
                AppLog.Warning("Player", "CharacterJumpController: Animator is null, Jump trigger will not play");
            }
            else
            {
                _jumpHash = Animator.StringToHash(_jumpTrigger);
                _doubleJumpHash = Animator.StringToHash(_doubleJumpTrigger);
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
            if (_motor == null) return;
            if (_motor.IsGrounded)
            {
                // 地面跳。
                if (_animator != null) _animator.ResetTrigger(_doubleJumpHash);
                if (_animator != null) _animator.SetTrigger(_jumpHash);
                _motor.DoJump(_jumpHeight);
                _airJumpsRemaining = _maxAirJumps;
                EventBus.Publish(new JumpStartedEvent(transform.position));
            }
            else if (_airJumpsRemaining > 0)
            {
                // 空中跳(二段跳)。
                if (_animator != null) _animator.ResetTrigger(_jumpHash);
                if (_animator != null) _animator.SetTrigger(_doubleJumpHash);
                _motor.DoJump(_doubleJumpHeight);
                _airJumpsRemaining--;
                EventBus.Publish(new DoubleJumpStartedEvent(transform.position));
            }
            // else: 空中且剩余次数为 0,忽略输入(避免三段以上跳)。
        }
        private void OnLanded(LandedEvent _)
        {
            // 落地归零,允许再次一段跳。
            _airJumpsRemaining = _maxAirJumps;
        }



    }
}
