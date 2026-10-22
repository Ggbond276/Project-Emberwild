using System;

namespace Emberwild.Define
{
    /// <summary>行为规则中的一个条件，通过 TID 和 RuleIndex 关联规则。</summary>
    [Serializable]
    public class MonsterBehaviorConditionDefine
    {
        public int TID;
        public int RuleIndex;
        public int ConditionIndex;
        public string ConditionId;
        public float ConditionValue;
    }
}
