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
    /// <summary>
    /// 玩家战斗组件 — 攻击连段 + 攻击事件发送。
    ///
    /// 职责:
    /// - 维护 _comboIndex / _comboWindow,听 InputReader.AttackTriggered 触发连段。
    /// - 驱动 Animator(ComboIndex + Attack Trigger)。
    /// - 发送 PlayerAttackedEvent。
    /// - 提供 InterruptCombo() 给受击系统调用。
    ///
    /// 严格不做:
    /// - 移动(Locomotion 在 CharacterMovementController)。
    /// - HP / 受击 / 死亡(Step 7.8 在 PlayerHealth / PlayerFeedback)。
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private InputReader _inputReader;
        [SerializeField] private Animator _animator;

        // TODO：以后需要抽离到配置表中
        [SerializeField] private float[] _attackDamages = { 10f, 12f, 15f, 20f, 25f };

        [SerializeField] private string _attackTrigger = "Attack";

        [SerializeField] private string _attackIndexParameter = "ComboIndex";

        [Tooltip("连段衔接窗口(秒)")]
        [SerializeField] private float _comboWindow = 0.6f;


        private int _comboIndex;
        private float _lastAttackTime;
        private bool _isAttacking;

        private int _attackTriggerHash;
        private int _attackIndexHash;

        public int ComboIndex => _comboIndex;
        public bool IsAttacking => _isAttacking;


        private void Start()
        {
            _attackTriggerHash = Animator.StringToHash(_attackTrigger);
            _attackIndexHash = Animator.StringToHash(_attackIndexParameter);
        }


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
            if (Time.time - _lastAttackTime > _comboWindow) _comboIndex = 0;
            if (_comboIndex > 4) _comboIndex = 0;

            _isAttacking = true;
            _lastAttackTime = Time.time;

            // 计算伤害
            float damage = _attackDamages[_comboIndex];

            // 计算攻击点
            Vector3 attackPoint = transform.position + transform.forward * 1.5f;

            if (_animator != null)
            {
                // 1.设置参数
                _animator.SetInteger(_attackIndexHash, _comboIndex);
                // 2.重置
                _animator.ResetTrigger(_attackTriggerHash);
                // 3.触发
                _animator.SetTrigger(_attackTriggerHash);
            }

            EventBus.Publish(new PlayerAttackedEvent(_comboIndex, attackPoint, transform.forward, damage));

            _comboIndex++;

            AppLog.Info("Combat", $"Attack combo={_comboIndex} dmg={damage:F1}");

        }

        /// <summary>
        /// 受伤时连击会被打断
        /// </summary>
        public void InterrunptCombo()
        {
            if (_isAttacking || _comboIndex != 0)
            {
                _isAttacking = false;
                _comboIndex = 0;
                if(_animator != null)
                {
                    _animator.ResetTrigger(_attackTriggerHash); 
                }

                AppLog.Info("Combat", "Combo interrupted");
            }
        }

    }
}
