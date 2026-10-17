using Emberwild.Core.AppLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Emberwild.AI.Enemy
{
    [DisallowMultipleComponent]
    public class EnemyAnimator : MonoBehaviour
    {
        [Header("引用")]
        [Tooltip("怪物 Animator(挂在怪物 GameObject 上)")]
        [SerializeField] private Animator _animator;

        [Header("Float 参数名")]
        [SerializeField] private string _speedParameter = "Speed";
        


        [Header("Bool 参数名")]
        [SerializeField] private string _isDeadParameter = "IsDead";
        [SerializeField] private string _isHiddenParameter = "IsHidden";


        [Header("Trigger 参数名")]
        [SerializeField] private string _attackTrigger = "Attack";
        [SerializeField] private string _projectileAttackTrigger = "ProjectileAttack";
        [SerializeField] private string _castSpellTrigger = "CastSpell";
        [SerializeField] private string _rollTrigger = "Roll";
        [SerializeField] private string _takeDamageTrigger = "TakeDamage";

        private int _speedHash;
        private int _isDeadHash;
        private int _isHiddenHash;
        private int _attackHash;
        private int _projectileAttackHash;
        private int _castSpellHash;
        private int _rollHash;
        private int _takeDamageHash;

        private void Awake()
        {
           if(_animator == null)
            {
                _animator = GetComponent<Animator>();
            }

           if(_animator == null)
            {
                AppLog.Error("Enemy", "EnemyAnimator: 找不到 Animator 组件, 请在 Inspector 里指定");
                
                enabled = false; // 禁用当前组件
                return;
            }

            _speedHash = Animator.StringToHash(_speedParameter);
            _isDeadHash = Animator.StringToHash(_isDeadParameter);
            _isHiddenHash = Animator.StringToHash(_isHiddenParameter);
            _attackHash = Animator.StringToHash(_attackTrigger);
            _projectileAttackHash = Animator.StringToHash(_projectileAttackTrigger);
            _castSpellHash = Animator.StringToHash(_castSpellTrigger);
            _rollHash = Animator.StringToHash(_rollTrigger);
            _takeDamageHash = Animator.StringToHash(_takeDamageTrigger);
        }

        /// <summary>
        /// 同步速度到Animator
        /// </summary>
        /// <param name="normalizedSpeed"></param>
        public void SetMoveSpeed(float normalizedSpeed)
        {
            if (_animator == null) return;
            _animator.SetFloat(_speedHash, normalizedSpeed);
        }

        /// <summary>
        /// 同步死亡状态
        /// </summary>
        /// <param name="dead"></param>
        public void SetDead(bool dead)
        {
            if (_animator == null) return;
            _animator.SetBool(_isDeadHash, dead);
        }

        /// <summary>
        /// 同步锁壳状态
        /// </summary>
        /// <param name="hidden"></param>
        public void SetHidden(bool hidden)
        {
            if (_animator == null) return;
            _animator.SetBool(_isHiddenHash, hidden);
        }

        /// <summary>
        /// 播放滚动冲刺
        /// </summary>
        public void SetRoll(bool roll)
        {
            if (_animator == null) return;
            _animator.SetBool(_rollHash, roll);
        }

        /// <summary>
        /// 播放近战动作
        /// </summary>
        public void PlayBiteAttack()
        {
            if (_animator == null) return;
            _animator.ResetTrigger(_attackHash);
            _animator.SetTrigger(_attackHash);
        }

        /// <summary>
        /// 播放远程攻击
        /// </summary>
        public void PlayProjectileAttack()
        {
            if (_animator == null) return;
            _animator.ResetTrigger(_projectileAttackHash);
            _animator.SetTrigger(_projectileAttackHash);
        }

        /// <summary>
        /// 播放施法
        /// </summary>
        public void PlayCastSpell()
        {
            if (_animator == null) return;
            _animator.ResetTrigger(_castSpellHash);
            _animator.SetTrigger(_castSpellHash);
        }

       

        /// <summary>
        /// 播放攻击
        /// </summary>
        public void PlayTakeDamage()
        {
            if (_animator == null) return;
            _animator.ResetTrigger(_takeDamageHash);
            _animator.SetTrigger(_takeDamageHash);
        }

    }
}
