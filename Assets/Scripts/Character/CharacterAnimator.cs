using UnityEngine;
using Emberwild.Core.AppLog;

namespace Emberwild.Character
{
    /// <summary>
    /// 角色动画播放层 — 唯一允许接触 Animator 的类。
    ///
    /// 职责:
    /// - 持有 Animator 引用 + 所有参数 Hash。
    /// - 对外暴露语义级方法(PlayJumpStart / PlayDoubleJump / PlayAttack / ...)。
    /// - 调用方只说"播什么",不说"碰哪个 Trigger / Bool / Float"。
    ///
    /// 严格不做:
    /// - 不读 / 不写游戏状态(HP / 攻击伤害 / 段位判定 都在调用方)。
    /// - 不订阅事件总线(动画是"被推"的,不是"自拉"的)。
    /// </summary>
    [DisallowMultipleComponent]
    public class CharacterAnimator : MonoBehaviour
    {
        [Header("引用")]
        [Tooltip("角色 Animator(挂在角色 GameObject 上)")]
        [SerializeField] private Animator _animator;

        [Header("Trigger 参数名")]
        [SerializeField] private string _jumpTrigger = "Jump";
        [SerializeField] private string _doubleJumpTrigger = "DoubleJump";
        [SerializeField] private string _attackTrigger = "Attack";
        [SerializeField] private string _getHitTrigger = "GetHit";
        [SerializeField] private string _dieTrigger = "Die";

        [Header("Int 参数名")]
        [SerializeField] private string _comboIndexParameter = "ComboIndex";

        [Header("Float 参数名")]
        [SerializeField] private string _speedParameter = "Speed";

        [Header("Bool 参数名")]
        [SerializeField] private string _isGroundedParameter = "IsGrounded";

        private int _jumpHash;
        private int _doubleJumpHash;
        private int _attackHash;
        private int _getHitHash;
        private int _dieHash;
        private int _comboIndexHash;
        private int _speedHash;
        private int _isGroundedHash;

        private void Awake()
        {
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }

            if (_animator == null)
            {
                AppLog.Error("Player", "CharacterAnimator: Animator is null, please assign in Inspector");
                enabled = false;
                return;
            }

            _jumpHash = Animator.StringToHash(_jumpTrigger);
            _doubleJumpHash = Animator.StringToHash(_doubleJumpTrigger);
            _attackHash = Animator.StringToHash(_attackTrigger);
            _getHitHash = Animator.StringToHash(_getHitTrigger);
            _dieHash = Animator.StringToHash(_dieTrigger);
            _comboIndexHash = Animator.StringToHash(_comboIndexParameter);
            _speedHash = Animator.StringToHash(_speedParameter);
            _isGroundedHash = Animator.StringToHash(_isGroundedParameter);
        }

        // ---------- 跳跃 ----------

        /// <summary>播放一段跳动画(地面跳)。</summary>
        public void PlayJumpStart()
        {
            if (_animator == null) return;
            _animator.ResetTrigger(_doubleJumpHash);
            _animator.SetTrigger(_jumpHash);
        }

        /// <summary>播放二段跳动画(空中跳)。</summary>
        public void PlayDoubleJump()
        {
            if (_animator == null) return;
            _animator.ResetTrigger(_jumpHash);
            _animator.SetTrigger(_doubleJumpHash);
        }

        /// <summary>清理残留的二段跳 Trigger(落地时调用,避免下一帧被状态机误消费)。</summary>
        public void ResetDoubleJump()
        {
            if (_animator == null) return;
            _animator.ResetTrigger(_doubleJumpHash);
        }

        /// <summary>清理残留的跳跃 Trigger(落地时调用,避免落地瞬间被状态机误消费导致走路动画覆盖跳跃动画)。</summary>
        public void ResetJump()
        {
            if (_animator == null) return;
            _animator.ResetTrigger(_jumpHash);
        }

        // ---------- 攻击 ----------

        /// <summary>播放指定段位的攻击动画。</summary>
        public void PlayAttack(int comboIndex)
        {
            if (_animator == null) return;
            // 顺序保留原 PlayerCombat:先设参数,再重置,再触发,避免连招错段。
            _animator.SetInteger(_comboIndexHash, comboIndex);
            _animator.ResetTrigger(_attackHash);
            _animator.SetTrigger(_attackHash);
        }

        /// <summary>打断当前连段(受击时调用)。</summary>
        public void ResetAttack()
        {
            if (_animator == null) return;
            _animator.ResetTrigger(_attackHash);
        }

        // ---------- 受击 / 死亡 ----------

        /// <summary>播放受击动画。</summary>
        public void PlayGetHit()
        {
            if (_animator == null) return;
            _animator.SetTrigger(_getHitHash);
        }

        /// <summary>播放死亡动画。</summary>
        public void PlayDie()
        {
            if (_animator == null) return;
            _animator.SetTrigger(_dieHash);
        }

        /// <summary>重置死亡 Trigger(复活时调用)。</summary>
        public void ResetDie()
        {
            if (_animator == null) return;
            _animator.ResetTrigger(_dieHash);
        }

        // ---------- 状态广播 ----------

        /// <summary>同步落地状态到 Animator(由物理层调用)。</summary>
        public void SetGrounded(bool grounded)
        {
            if (_animator == null) return;
            _animator.SetBool(_isGroundedHash, grounded);
        }

        /// <summary>同步移动速度到 Animator,驱动 Locomotion Blend Tree。</summary>
        public void SetMoveSpeed(float speed)
        {
            if (_animator == null) return;
            _animator.SetFloat(_speedHash, speed);
        }
    }
}
