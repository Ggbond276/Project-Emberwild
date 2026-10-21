namespace Emberwild.AI.Enemy.Ability
{
    public interface IAbilityExecutor
    {
        #region 能力进度 (Ability Progress)
        /// <summary>推进能力并返回是否已经结束。</summary>
        bool Tick(
            AbilityDefinition definition,
            IEnemyAbilityContext context,
            float deltaTime,
            float elapsed);
        #endregion

        #region 能力开始与结束 (Ability Start & End)
        /// <summary>初始化能力的动画、移动和命中状态。</summary>
        void Start(AbilityDefinition definition, IEnemyAbilityContext context);

        /// <summary>清理能力造成的移动锁和临时属性。</summary>
        void Stop(AbilityDefinition definition, IEnemyAbilityContext context);
        #endregion
    }
}
