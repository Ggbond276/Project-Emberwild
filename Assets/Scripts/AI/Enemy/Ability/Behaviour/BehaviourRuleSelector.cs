using Emberwild.Combat;

namespace Emberwild.AI.Enemy.Ability
{
    /// <summary>
    /// 按配置顺序扫描行为规则，返回第一条全部条件成立的动作。
    /// 配置数组的顺序就是行为优先级，从上到下依次降低。
    /// </summary>
    public sealed class BehaviourRuleSelector
    {
        private readonly MonsterAIDefinition _definition;

        /// <summary>使用一只怪物的行为规则创建选择器。</summary>
        public BehaviourRuleSelector(MonsterAIDefinition definition)
        {
            _definition = definition;
        }

        /// <summary>
        /// 根据当前黑板和能力状态选择下一条行为。
        /// </summary>
        /// <param name="blackboard"></param>
        /// <param name="abilities"></param>
        /// <param name="attribute"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        public BehaviourDecision Select(
            EnemyBlackboard blackboard,
            EnemyAbilityRuntime abilities,
            CombatAttribute attribute,
            float now)
        {
            if (_definition?.behavior == null) return Idle();

            foreach (var rule in _definition.behavior)
            {
                if (rule == null || !MatchesAll(rule, blackboard, abilities, attribute, now)) continue;
                return new BehaviourDecision(rule.action, rule.abilityId, rule.slot);
            }
            return Idle();
        }

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
                        condition, blackboard, abilities, attribute, _definition, now))
                    return false;
            }
            return true;
        }

        private static BehaviourDecision Idle()
        {
            return new BehaviourDecision(BehaviorActions.Idle, string.Empty, string.Empty);
        }
    }
}
