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
    /// - 发起一次近战攻击的时候：播放BIteAttack动画 + 发布攻击事件
    /// - 维护攻击硬直窗口，保证攻击不重叠
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyCombat : MonoBehaviour
    {
        [Header("组件引用")]
        [SerializeField] private EnemyAnimator _animator;
        [SerializeField] private EnemyEntity _entity;

        [Header("攻击参数")]
        [Tooltip("单段近战攻击硬直时长")]
        [SerializeField] private float _attackDuration = 0.6f;

        private float _attackEndTime;
        public bool IsAttacking => Time.time < _attackEndTime;

        // TODO: 本阶段暂时限制ComboIndex为0 因为配置表还没有配置别的攻击数据
        public int ComboIndex => 0;

        private void Awake()
        {
            if (_animator == null) _animator = GetComponent<EnemyAnimator>();
            if (_entity == null) _entity = GetComponent<EnemyEntity>();
        }

        public void DoAttack()
        {
            if (IsAttacking) return;
            _attackEndTime = Time.time + _attackDuration;
            _animator?.PlayBiteAttack();
            EventBus.Publish(new CharacterAttackHitEvent(_entity, ComboIndex));
            AppLog.Info("Combat", $"{name} 发起近战攻击 combo={ComboIndex}");

        }

    }
}
