using Emberwild.AI.Enemy.EnemyAnimator;
using Emberwild.Core.AppLog;
using Emberwild.Core.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Emberwild.AI.Enemy
{
    /// <summary>
    /// 怪物攻击执行层 - AI驱动怪物的手
    /// 
    /// 职责:
    /// - 发起一次近战攻击的时候：播放BiteAttack动画 + 发布攻击事件
    /// - 维护攻击硬直窗口，保证攻击不重叠
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyCombat : MonoBehaviour
    {
        [Header("组件引用")]
        [SerializeField] private MonoBehaviour _animator; // 挂 ShellAnimator(实现 IEnemyAnimator)
        [SerializeField] private EnemyEntity _entity;

        [Header("攻击参数")]
        [Tooltip("单段攻击硬直时长(默认值,运行时会被 EnemyAIConfig.attackDuration 覆盖)")]
        [SerializeField] private float _attackDuration = 0.6f;

        private float _attackEndTime;
        public bool IsAttacking => Time.time < _attackEndTime;

        private IEnemyAnimator Animator => _animator as IEnemyAnimator;

        // TODO: 本阶段暂时限制ComboIndex为0 因为配置表还没有配置别的攻击数据
        public int ComboIndex => 0;

        private void Awake()
        {
            if (_animator == null) _animator = FindAnimator();
            if (_entity == null) _entity = GetComponent<EnemyEntity>();
        }

        /// <summary>
        /// 注入攻击硬直时长(由 EnemyBrain 从 EnemyAIConfig 读取后调用)。
        /// </summary>
        public void Configure(float attackDuration)
        {
            _attackDuration = attackDuration;
        }

        /// <summary>
        /// AI 决策层调用：发动一次近战攻击
        /// </summary>
        public void DoAttack()
        {
            if (IsAttacking) return;
            _attackEndTime = Time.time + _attackDuration;
            Animator?.PlayAttack();
            EventBus.Publish(new CharacterAttackHitEvent(_entity, ComboIndex));
            AppLog.Info("Combat", $"{name} 发起近战攻击 combo={ComboIndex}");
        }

        /// <summary>
        /// AI 决策层调用：发动一次远程攻击
        /// </summary>
        /// <param name="targetPos"></param>
        public void DoRangedAttack(Vector3 targetPos)
        {
            if (IsAttacking) return;

            _attackEndTime = Time.time + _attackDuration;
            Animator?.PlayRangedAttack();
            EventBus.Publish(new CharacterAttackHitEvent(_entity, 1, targetPos));
            AppLog.Info("Combat", $"{name} 发起远程攻击 target={targetPos}");
        }

        /// <summary>
        /// 在自身组件中查找第一个实现 IEnemyAnimator 的组件(接口类型无法用 GetComponent 泛型直接查询)。
        /// </summary>
        private MonoBehaviour FindAnimator()
        {
            foreach (var mb in GetComponents<MonoBehaviour>())
            {
                if (mb is IEnemyAnimator) return mb;
            }
            return null;
        }
    }
}
