namespace Emberwild.AI.Enemy.Ability
{
    public readonly struct BehaviourDecision
    {
        #region 决策数据 (Decision Data)
        public readonly string Action;
        public readonly string AbilityId;
        public readonly string Slot;
        public readonly AbilityDefinition Ability;
        #endregion

        #region 构造方法 (Constructor)
        /// <summary>创建一次行为决策结果。</summary>
        public BehaviourDecision(string action, string abilityId, string slot)
            : this(action, abilityId, slot, null)
        {
        }

        /// <summary>创建包含已解析能力的决策结果。</summary>
        public BehaviourDecision(
            string action,
            string abilityId,
            string slot,
            AbilityDefinition ability)
        {
            Action = action;
            AbilityId = abilityId;
            Slot = slot;
            Ability = ability;
        }
        #endregion
    }
}
