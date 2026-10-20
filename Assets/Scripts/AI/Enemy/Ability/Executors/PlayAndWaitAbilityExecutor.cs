using UnityEngine;

namespace Emberwild.AI.Enemy.Ability
{
    /// <summary>
    /// 播放并等待执行器。用于警戒、受击等没有持续逻辑的一次性动作。
    /// </summary>
    public class PlayAndWaitAbilityExecutor : IAbilityExecutor
    {
        private float _endsAt;

        /// <summary>播放语义动作，可选锁移动，并开始计时。</summary>
        public void Start(AbilityDefinition definition, IEnemyAbilityContext context)
        {
            if (definition.lockMovement) context.Agent.SetMotionLocked(true);
            context.Animator.SetMoveSpeed(0f);
            if (!string.IsNullOrWhiteSpace(definition.animationAction))
                context.Animator.PlaySemanticAction(definition.animationAction);
            _endsAt = Time.time + Mathf.Max(0f, definition.duration);
        }

        /// <summary>计时结束后返回完成。</summary>
        public bool Tick(
            AbilityDefinition definition,
            IEnemyAbilityContext context,
            float deltaTime,
            float elapsed)
        {
            return Time.time >= _endsAt;
        }

        /// <summary>解除可选的移动锁定。</summary>
        public void Stop(AbilityDefinition definition, IEnemyAbilityContext context)
        {
            if (definition.lockMovement) context.Agent.SetMotionLocked(false);
        }
    }
}
