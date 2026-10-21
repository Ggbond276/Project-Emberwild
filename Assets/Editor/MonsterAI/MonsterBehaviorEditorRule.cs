using System.Collections.Generic;

namespace Emberwild.Editor.MonsterAI
{
    public class MonsterBehaviorEditorRule
    {
        #region 行为规则 (Behavior Rule)
        public MonsterBehaviorRow Rule = new MonsterBehaviorRow();
        public List<MonsterBehaviorConditionRow> Conditions =
            new List<MonsterBehaviorConditionRow>();
        #endregion
    }
}
