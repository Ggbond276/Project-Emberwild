using Emberwild.Core.AppLog;
using Emberwild.Core.Events;
using Emberwild.Input;
using UnityEngine;

namespace Emberwild.Character
{
    /// <summary>
    /// 角色战斗组件 — 攻击连段 + 攻击事件发送。
    ///
    /// 职责:
    /// - 维护 _comboIndex / _comboWindow,听 InputReader.OnAttack 触发连段。
    /// - 调用 CharacterAnimator.PlayAttack 播放连段动画。
    /// - 发布 CharacterAttackStartedEvent / CharacterAttackHitEvent / CharacterAttackEndedEvent。
    /// - 提供 InterruptCombo() 给受击系统调用。
    ///
    /// 严格不做:
    /// - 移动(Locomotion 在 CharacterMovementController)。
    /// - 移动锁定(锁定由 CharacterStateController 根据事件自动管理)。
    /// - HP / 受击 / 死亡(由 CharacterDataManager 单点产生事件)。
    /// </summary>
    [DisallowMultipleComponent]
    public class CharacterCombat : MonoBehaviour
    {
        [Header("输入")]
        [SerializeField] private InputReader _inputReader;

        [Header("动画")]
        [SerializeField] private CharacterAnimator _animator;

        [Header("状态")]
        [SerializeField] private CharacterStateController _state;

        [Tooltip("单段攻击动画时长(秒)")]
        [SerializeField] private float _attackDuration = 0.5f;

        [Tooltip("段间衔接窗口(秒)")]
        [SerializeField] private float _comboWindow = 0.6f;

        [Tooltip("移动锁时长(秒,必须 >= 攻击动画时长,确保动画期间完全锁住)")]
        [SerializeField] private float _lockDuration = 0.5f;

        [Tooltip("实体数据")]
        [SerializeField]  private CharacterEntity _entity;


        private int _comboIndex;
        private float _lastAttackTime;
        private float _attackEndTime;
        private bool _isAttacking;
        // 硬直期间按攻击,标记待推进段位;硬直结束消费。
        private bool _pendingComboAdvance;


        public int ComboIndex => _comboIndex;
        public bool IsAttacking => _isAttacking;

        private void OnEnable()
        {
            if (_inputReader != null) _inputReader.OnAttack += HandleAttack;
        }

        private void OnDisable()
        {
            if (_inputReader != null) _inputReader.OnAttack -= HandleAttack;
        }

        private void HandleAttack()
        {
            // 攻击禁用守卫。
            if (_state != null && !_state.CanAttack) return;

            // 硬直中:不重置 _attackEndTime(保护当前动画),只缓存推进意图。
            if (_isAttacking)
            {
                _pendingComboAdvance = true;
                AppLog.Info("Combat", $"Attack buffered, current combo={_comboIndex}");
                return;
            }

            // 衔接窗口外(距上次硬直结束 > _comboWindow):从段 0 开始。
            if (Time.time - _lastAttackTime > _comboWindow)
            {
                _comboIndex = 0;
            }

            // 如果连击段数超过上限，重置为第一段
            if (_comboIndex > 3) _comboIndex = 0;

            _isAttacking = true;
            _attackEndTime = Time.time + _attackDuration;
            _lastAttackTime = _attackEndTime;

            if (_animator != null) _animator.PlayAttack(_comboIndex);

            EventBus.Publish(new CharacterAttackHitEvent(_entity, _comboIndex));
            EventBus.Publish(new CharacterAttackStartedEvent(_comboIndex, _attackDuration, _lockDuration));

            // 修改日志：因为这里不再知道具体伤害，所以只打印触发了第几段攻击
            AppLog.Info("Combat", $"Attack combo={_comboIndex} triggered");

            _comboIndex++;
        }

        private void Update()
        {
            if (_isAttacking && Time.time >= _attackEndTime)
            {
                int endedCombo = _comboIndex - 1;
                _isAttacking = false;

                // 消费硬直期间缓存的连段推进意图。
                if (_pendingComboAdvance)
                {
                    _pendingComboAdvance = false;
                    // 立即推进到下一段。
                    HandleAttack();
                    return;
                }

                EventBus.Publish(new CharacterAttackEndedEvent(endedCombo));
                AppLog.Info("Combat", $"Attack ended, last combo={endedCombo}");
            }
        }

        /// <summary>
        /// 受伤时连击会被打断。
        /// </summary>
        public void InterruptCombo()
        {
            if (_isAttacking || _comboIndex != 0)
            {
                _isAttacking = false;
                _comboIndex = 0;
                if (_animator != null)
                {
                    _animator.ResetAttack();
                }
                AppLog.Info("Combat", "Combo interrupted");
            }
        }
    }
}
