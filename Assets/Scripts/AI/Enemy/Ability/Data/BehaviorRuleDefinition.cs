using System;
using System.Collections.Generic;

namespace Emberwild.AI.Enemy.Ability
{
    /// <summary>
    /// 一条按优先级排列的行为规则。所有条件成功时执行指定动作。
    /// </summary>
    [Serializable]
    public class BehaviorRuleDefinition
    {
        public string action = BehaviorActions.Idle;
        public string abilityId;
        public string slot;
        public List<BehaviorConditionDefinition> conditions = new List<BehaviorConditionDefinition>();
    }
}
