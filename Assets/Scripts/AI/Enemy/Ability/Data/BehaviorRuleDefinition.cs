using System;
using System.Collections.Generic;

namespace Emberwild.AI.Enemy.Ability
{
    [Serializable]
    public class BehaviorRuleDefinition
    {
        #region 规则配置数据 (Rule Data)
        public string action = BehaviorActions.Idle;
        public string abilityId;
        public string slot;
        public List<BehaviorConditionDefinition> conditions = new List<BehaviorConditionDefinition>();
        #endregion
    }
}
