namespace Emberwild.AI.Enemy.Ability
{
    /// <summary>
    /// 死亡执行器。锁定移动、播放死亡并关闭寻路组件。
    /// </summary>
    public class DeadAbilityExecutor : IAbilityExecutor
    {
        /// <summary>进入不可逆死亡表现。</summary>
        public void Start(AbilityDefinition definition, IEnemyAbilityContext context)
        {
            context.Agent.SetMotionLocked(true);
            context.Animator.SetMoveSpeed(0f);
            context.Animator.SetDead(true);
            context.Agent.enabled = false;
        }

        /// <summary>死亡能力不会正常结束。</summary>
        public bool Tick(
            AbilityDefinition definition,
            IEnemyAbilityContext context,
            float deltaTime,
            float elapsed)
        {
            return false;
        }

        /// <summary>死亡状态不恢复移动。</summary>
        public void Stop(AbilityDefinition definition, IEnemyAbilityContext context)
        {
        }
    }
}
