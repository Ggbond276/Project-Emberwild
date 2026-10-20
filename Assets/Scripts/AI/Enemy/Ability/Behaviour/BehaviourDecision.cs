namespace Emberwild.AI.Enemy.Ability
{
    /// <summary>
    /// 行为规则选择结果，描述接下来应执行的动作或能力。
    /// </summary>
    public readonly struct BehaviourDecision
    {
        public readonly string Action;
        public readonly string AbilityId;
        public readonly string Slot;

        /// <summary>创建一次行为决策。</summary>
        /// <param name="action">动作类型，例如 ability、chase 或 idle。</param>
        /// <param name="abilityId">直接指定的能力 ID，可为空。</param>
        /// <param name="slot">要激活的能力槽，可为空。</param>
        public BehaviourDecision(string action, string abilityId, string slot)
        {
            Action = action;
            AbilityId = abilityId;
            Slot = slot;
        }
    }
}
