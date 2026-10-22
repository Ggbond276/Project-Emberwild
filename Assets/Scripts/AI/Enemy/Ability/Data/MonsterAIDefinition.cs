using System;
using System.Collections.Generic;

namespace Emberwild.AI.Enemy.Ability
{
    [Serializable]
    public class MonsterAIDefinition
    {
        #region 怪物配置数据 (Monster Data)
        // 怪物身份与移动、感知参数
        public int TID;
        public string Name;

        public float viewRange = 10f;
        public float loseTargetRange = 13f;
        public float scanInterval = 0.2f;
        public float runSpeed = 4f;
        public float turnSpeed = 10f;
        public float stoppingDistance = 1.2f;
        public float hitStunDuration = 0.4f;

        // 动画、能力和行为规则
        public AnimationProfileDefinition animation = new AnimationProfileDefinition();
        // 能力表
        public List<AbilityDefinition> abilities = new List<AbilityDefinition>();
        // 行为树
        public List<BehaviorRuleDefinition> behavior = new List<BehaviorRuleDefinition>();
        #endregion
    }
}
