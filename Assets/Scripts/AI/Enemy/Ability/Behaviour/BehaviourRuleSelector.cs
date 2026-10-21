using Emberwild.Combat;

namespace Emberwild.AI.Enemy.Ability
{
    public sealed class BehaviourRuleSelector
    {
        #region 规则配置 (Rule Configuration)
        private readonly MonsterAIDefinition _definition;
        #endregion

        #region 构造方法 (Constructor)
        /// <summary>使用怪物行为规则创建选择器。</summary>
        public BehaviourRuleSelector(MonsterAIDefinition definition)
        {
            _definition = definition;
        }
        #endregion

        #region 规则匹配与决策 (Rule Matching & Decision)
        /// <summary>返回第一条全部条件成立的行为规则。</summary>
        public BehaviourDecision Select(
            EnemyBlackboard blackboard,
            EnemyAbilityRuntime abilities,
            CombatAttribute attribute,
            float now)
        {
            if (_definition?.behavior == null) return Idle();

            foreach (var rule in _definition.behavior)
            {
                if (rule == null
                    || !MatchesAll(rule, blackboard, abilities, attribute, now))
                {
                    continue;
                }
                return new BehaviourDecision(rule.action, rule.abilityId, rule.slot);
            }
            return Idle();
        }

        /// <summary>判断规则中的条件是否全部成立。</summary>
        private bool MatchesAll(
            BehaviorRuleDefinition rule,
            EnemyBlackboard blackboard,
            EnemyAbilityRuntime abilities,
            CombatAttribute attribute,
            float now)
        {
            if (rule.conditions == null) return true;
            foreach (var condition in rule.conditions)
            {
                if (condition == null
                    || !BehaviorConditionRegistry.TryEvaluate(
                        condition,
                        blackboard,
                        abilities,
                        attribute,
                        _definition,
                        now))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>创建待机决策。</summary>
        private static BehaviourDecision Idle()
        {
            return new BehaviourDecision(
                BehaviorActions.Idle,
                string.Empty,
                string.Empty);
        }
        #endregion
    }
}
