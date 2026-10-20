using System;
using System.Collections.Generic;
using Emberwild.Combat;
using UnityEngine;

namespace Emberwild.AI.Enemy.Ability
{
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
}
