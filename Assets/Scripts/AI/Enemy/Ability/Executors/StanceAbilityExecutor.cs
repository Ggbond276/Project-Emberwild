using UnityEngine;

namespace Emberwild.AI.Enemy.Ability
{
    /// <summary>
    /// 持续姿态执行器。用于缩壳等能力，统一处理移动锁和承伤倍率。
    /// </summary>
    public class StanceAbilityExecutor : IAbilityExecutor
    {
        private float _endsAt;
        private float _previousDamageMultiplier = 1f;

        /// <summary>锁定移动、开启姿态动画并设置承伤倍率。</summary>
        public void Start(AbilityDefinition definition, IEnemyAbilityContext context)
        {
            context.Agent.SetMotionLocked(true);
            context.Animator.SetMoveSpeed(0f);
            context.Animator.SetSemanticState(definition.animationState, true);
            _endsAt = Time.time + Mathf.Max(0f, definition.duration);

            if (context.Attribute != null)
            {
                _previousDamageMultiplier = context.Attribute.DamageTakenMultiplier;
                context.Attribute.DamageTakenMultiplier = definition.damageTakenMultiplier;
            }
        }

        /// <summary>姿态时间结束后返回完成。</summary>
        public bool Tick(
            AbilityDefinition definition,
            IEnemyAbilityContext context,
            float deltaTime,
            float elapsed)
        {
            return Time.time >= _endsAt;
        }

        /// <summary>关闭姿态并恢复进入前的承伤倍率。</summary>
        public void Stop(AbilityDefinition definition, IEnemyAbilityContext context)
        {
            context.Agent.SetMotionLocked(false);
            context.Animator.SetSemanticState(definition.animationState, false);
            if (context.Attribute != null)
                context.Attribute.DamageTakenMultiplier = _previousDamageMultiplier;
        }
    }
}
