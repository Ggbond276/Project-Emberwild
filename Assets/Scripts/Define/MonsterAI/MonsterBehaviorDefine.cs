using System;

namespace Emberwild.Define
{
    /// <summary>一条按 RuleIndex 排序的行为规则。</summary>
    [Serializable]
    public class MonsterBehaviorDefine
    {
        public int TID;
        public int RuleIndex;
        public string Action;
        public string AbilityId;
        public string Slot;
    }
}
