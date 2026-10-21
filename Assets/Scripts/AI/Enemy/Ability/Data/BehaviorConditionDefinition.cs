using System;

namespace Emberwild.AI.Enemy.Ability
{
    [Serializable]
    public class BehaviorConditionDefinition
    {
        #region 条件配置数据 (Condition Data)
        public string id;
        public string text1;
        public string text2;
        public float p1;
        public float p2;
        #endregion

        #region 构造方法 (Constructors)
        /// <summary>
        /// 创建空行为条件。
        /// </summary>
        public BehaviorConditionDefinition() { }

        /// <summary>创建使用数值参数的行为条件。</summary>
        public BehaviorConditionDefinition(string id, float p1 = 0f, float p2 = 0f)
        {
            this.id = id;
            this.p1 = p1;
            this.p2 = p2;
        }

        /// <summary>
        /// 创建使用字符串参数的行为条件。
        /// </summary>
        public BehaviorConditionDefinition(string id, string text1)
        {
            this.id = id;
            this.text1 = text1;
        }
        #endregion
    }
}
