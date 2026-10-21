using UnityEngine;

namespace Emberwild.AI.Enemy.Ability
{
    public class PlayAndWaitAbilityExecutor : IAbilityExecutor
    {
        #region 执行器运行时状态 (Executor Runtime State)
        private float _endsAt;
        #endregion

        #region 能力进度 (Ability Progress)
        /// <summary>计时结束后返回完成。</summary>
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
        /// <summary>播放动作、锁定移动并开始计时。</summary>
        public void Start(AbilityDefinition definition, IEnemyAbilityContext context)
        {
            if (definition.lockMovement) context.Agent.SetMotionLocked(true);
            context.Animator.SetMoveSpeed(0f);
            if (!string.IsNullOrWhiteSpace(definition.animationAction))
                context.Animator.PlaySemanticAction(definition.animationAction);
            _endsAt = Time.time + Mathf.Max(0f, definition.duration);
        }

        /// <summary>解除动作期间的移动锁定。</summary>
        public void Stop(AbilityDefinition definition, IEnemyAbilityContext context)
        {
            if (definition.lockMovement) context.Agent.SetMotionLocked(false);
        }
        #endregion
    }
}
