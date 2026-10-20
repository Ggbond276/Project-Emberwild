using System;
using System.Collections.Generic;

namespace Emberwild.AI.Enemy.Ability
{
    /// <summary>
    /// 一只怪物的统一运行时定义，包含感知、移动、动画、能力和行为规则。
    /// </summary>
    [Serializable]
    public class MonsterAIDefinition
    {
        public int TID;
        public string Name;

        public float viewRange = 10f;
        public float loseTargetRange = 13f;
        public float scanInterval = 0.2f;
        public float runSpeed = 4f;
        public float turnSpeed = 10f;
        public float stoppingDistance = 1.2f;
        public float hitStunDuration = 0.4f;

        public AnimationProfileDefinition animation = new AnimationProfileDefinition();
        public List<AbilityDefinition> abilities = new List<AbilityDefinition>();
        public List<BehaviorRuleDefinition> behavior = new List<BehaviorRuleDefinition>();
    }
}
