namespace Emberwild.AI.Enemy.Ability
{
    /// <summary>
    /// 通用能力执行器契约。一次能力经历 Start、持续 Tick 和最终 Stop。
    /// </summary>
    public interface IAbilityExecutor
    {
        /// <summary>能力开始时初始化动画、移动和命中状态。</summary>
        void Start(AbilityDefinition definition, IEnemyAbilityContext context);

        /// <summary>每帧推进能力；返回 true 表示能力已经结束。</summary>
        bool Tick(AbilityDefinition definition, IEnemyAbilityContext context, float deltaTime, float elapsed);

        /// <summary>正常结束或被中断时清理锁移动、姿态和临时属性。</summary>
        void Stop(AbilityDefinition definition, IEnemyAbilityContext context);
    }
}
