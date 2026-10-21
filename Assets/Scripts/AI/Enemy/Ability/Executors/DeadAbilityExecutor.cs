namespace Emberwild.AI.Enemy.Ability
{
    public class DeadAbilityExecutor : IAbilityExecutor
    {
        #region 能力进度 (Ability Progress)
        /// <summary>死亡能力保持执行且不自动结束。</summary>
        public bool Tick(
            AbilityDefinition definition,
            IEnemyAbilityContext context,
            float deltaTime,
            float elapsed)
        {
            return false;
        }
        #endregion

        #region 能力开始与结束 (Ability Start & End)
        /// <summary>锁定移动、播放死亡并关闭寻路组件。</summary>
        public void Start(AbilityDefinition definition, IEnemyAbilityContext context)
        {
            context.Agent.SetMotionLocked(true);
            context.Animator.SetMoveSpeed(0f);
            context.Animator.SetDead(true);
            context.Agent.enabled = false;
        }

        /// <summary>死亡状态不恢复移动。</summary>
        public void Stop(AbilityDefinition definition, IEnemyAbilityContext context)
        {
        }
        #endregion
    }
}
