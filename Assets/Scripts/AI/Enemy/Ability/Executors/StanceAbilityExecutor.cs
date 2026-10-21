using UnityEngine;

namespace Emberwild.AI.Enemy.Ability
{
    public class StanceAbilityExecutor : IAbilityExecutor
    {
        #region 执行器运行时状态 (Executor Runtime State)
        private float _endsAt;
        private float _previousDamageMultiplier = 1f;
        #endregion

        #region 能力进度 (Ability Progress)
        /// <summary>姿态时间结束后返回完成。</summary>
        public bool Tick(
            AbilityDefinition definition,
            IEnemyAbilityContext context,
            float deltaTime,
            float elapsed)
        {
            return Time.time >= _endsAt;
        }
        #endregion

        #region 能力开始与结束 (Ability Start & End)
        /// <summary>锁定移动、播放姿态并修改承伤倍率。</summary>
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

        /// <summary>关闭姿态并恢复原承伤倍率。</summary>
        public void Stop(AbilityDefinition definition, IEnemyAbilityContext context)
        {
            context.Agent.SetMotionLocked(false);
            context.Animator.SetSemanticState(definition.animationState, false);
            if (context.Attribute != null)
                context.Attribute.DamageTakenMultiplier = _previousDamageMultiplier;
        }
        #endregion
    }
}
