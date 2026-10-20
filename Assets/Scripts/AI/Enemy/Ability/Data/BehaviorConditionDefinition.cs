using System;

namespace Emberwild.AI.Enemy.Ability
{
    /// <summary>
    /// 行为规则中的单个条件及其参数。
    /// </summary>
    [Serializable]
    public class BehaviorConditionDefinition
    {
        public string id;
        public string text1;
        public string text2;
        public float p1;
        public float p2;

        public BehaviorConditionDefinition() { }

        /// <summary>创建一个无参数或数值参数条件。</summary>
        public BehaviorConditionDefinition(string id, float p1 = 0f, float p2 = 0f)
        {
            this.id = id;
            this.p1 = p1;
            this.p2 = p2;
        }

        /// <summary>创建一个使用字符串参数的条件，例如能力 ID 或槽 ID。</summary>
        public BehaviorConditionDefinition(string id, string text1)
        {
            this.id = id;
            this.text1 = text1;
        }
    }
}
