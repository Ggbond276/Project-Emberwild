using System;
using System.Collections.Generic;
using Emberwild.Combat;
using UnityEngine;

namespace Emberwild.AI.Enemy.Ability
{
    /// <summary>
    /// 行为规则选择结果，描述接下来应执行的动作或能力。
    /// </summary>
    public readonly struct BehaviourDecision
    {
        public readonly string Action;
        public readonly string AbilityId;
        public readonly string Slot;

        /// <summary>创建一次行为决策。</summary>
        /// <param name="action">动作类型，例如 ability、chase 或 idle。</param>
        /// <param name="abilityId">直接指定的能力 ID，可为空。</param>
        /// <param name="slot">要激活的能力槽，可为空。</param>
        public BehaviourDecision(string action, string abilityId, string slot)
        {
            Action = action;
            AbilityId = abilityId;
            Slot = slot;
        }
    }

    /// <summary>
    /// 行为条件注册表。内置条件无需注册，Boss 或特殊怪可追加自己的条件求值器。
    /// </summary>
    public static class BehaviorConditionRegistry
    {
        /// <summary>
        /// 条件求值函数签名。
        /// </summary>
        public delegate bool Evaluator(
            BehaviorConditionDefinition condition,
            EnemyBlackboard blackboard,
            EnemyAbilityRuntime abilities,
            CombatAttribute attribute,
            MonsterAIDefinition definition,
            float now);

        private static readonly Dictionary<string, Evaluator> Evaluators = new Dictionary<string, Evaluator>(StringComparer.OrdinalIgnoreCase)
            {
                [BehaviorConditions.Always] = (c, bb, a, attr, def, now) => true,
                [BehaviorConditions.HasTarget] = (c, bb, a, attr, def, now) => bb.Target != null,
                [BehaviorConditions.IsDead] = (c, bb, a, attr, def, now) => attr != null && attr.IsDead,
                [BehaviorConditions.RecentlyHit] = (c, bb, a, attr, def, now) => now - bb.LastHitTime < def.hitStunDuration,
                [BehaviorConditions.HpBelow] = (c, bb, a, attr, def, now) => bb.HpRatio < c.p1,
                [BehaviorConditions.InRange] = (c, bb, a, attr, def, now) => bb.DistanceToTarget >= c.p1 && bb.DistanceToTarget <= c.p2,
                [BehaviorConditions.AbilityReady] = (c, bb, a, attr, def, now) => a.IsAbilityReady(c.text1, c.p1),
                [BehaviorConditions.SlotReady] = (c, bb, a, attr, def, now) => a.IsSlotReady(c.text1, c.p1),
                [BehaviorConditions.SlotUnused] = (c, bb, a, attr, def, now) => a.IsSlotUnused(c.text1),
                [BehaviorConditions.AttackCooldownReady] = (c, bb, a, attr, def, now) => a.TimeSinceLastAttack >= c.p1
            };

        /// <summary>
        /// 注册或替换一个条件求值器。
        /// </summary>
        public static void Register(string conditionId, Evaluator evaluator)
        {
            if (string.IsNullOrWhiteSpace(conditionId) || evaluator == null) return;
            Evaluators[conditionId] = evaluator;
        }

        /// <summary>
        /// 判断指定条件 ID 是否已经注册。
        /// </summary>
        public static bool Contains(string conditionId)
        {
            return !string.IsNullOrWhiteSpace(conditionId) && Evaluators.ContainsKey(conditionId);
        }

        /// <summary>
        /// 执行指定条件；未知条件返回 false 并输出错误日志。
        /// </summary>
        public static bool TryEvaluate(
            BehaviorConditionDefinition condition,
            EnemyBlackboard blackboard,
            EnemyAbilityRuntime abilities,
            CombatAttribute attribute,
            MonsterAIDefinition definition,
            float now)
        {
            if (condition == null || string.IsNullOrWhiteSpace(condition.id)) return false;
            if (!Evaluators.TryGetValue(condition.id, out var evaluator))
            {
                Debug.LogError($"[EnemyAI] Unknown behavior condition '{condition.id}'.");
                return false;
            }
            return evaluator(condition, blackboard, abilities, attribute, definition, now);
        }
    }

    /// <summary>
    /// 按配置顺序扫描行为规则，返回第一条全部条件成立的动作。
    /// 配置数组的顺序就是行为优先级，从上到下依次降低。
    /// </summary>
    public sealed class BehaviourRuleSelector
    {
        private readonly MonsterAIDefinition _definition;

        /// <summary>
        /// 使用一只怪物的行为规则创建选择器。
        /// </summary>
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
