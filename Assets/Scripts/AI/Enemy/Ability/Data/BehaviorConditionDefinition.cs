using System;

namespace Emberwild.AI.Enemy.Ability
{
    [Serializable]
    public class BehaviorConditionDefinition
    {
        #region 条件配置数据 (Condition Data)
        public string id;
        public float conditionValue;
        #endregion

        #region 构造方法 (Constructors)
        /// <summary>
        /// 创建空行为条件。
        /// </summary>
        public BehaviorConditionDefinition() { }

        /// <summary>创建使用数值参数的行为条件。</summary>
        public BehaviorConditionDefinition(string id, float conditionValue = 0f)
        {
            this.id = id;
            this.conditionValue = conditionValue;
        }

        #endregion
    }
}
