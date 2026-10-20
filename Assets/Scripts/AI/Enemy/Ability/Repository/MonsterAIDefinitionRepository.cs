using System;
using System.Collections.Generic;
using System.Linq;
using Emberwild.Core.AppLog;
using Emberwild.Define;
using Emberwild.Manager;

namespace Emberwild.AI.Enemy.Ability
{
    /// <summary>
    /// 从 DataManager 已加载的外联表组装单只怪物的运行时定义。
    /// </summary>
    public static class MonsterAIDefinitionRepository
    {
        private static readonly Dictionary<int, MonsterAIDefinition> Cache =
            new Dictionary<int, MonsterAIDefinition>();

        /// <summary>外联表组装是否发生异常。</summary>
        public static bool HasLoadError { get; private set; }

        /// <summary>最近一次外联表组装错误的详细描述。</summary>
        public static string LoadError { get; private set; }

        /// <summary>
        /// 按 TID 获取怪物定义；TID 不在 MonsterAI 表中时返回 null。
        /// </summary>
        public static MonsterAIDefinition Get(int tid)
        {
            if (Cache.TryGetValue(tid, out var cached)) return cached;

            var manager = DataManager.Instance;
            if (!manager.MonsterAICores.TryGetValue(tid, out var core)) return null;

            try
            {
                var definition = Build(tid, core, manager);
                Cache[tid] = definition;
                HasLoadError = false;
                LoadError = string.Empty;
                return definition;
            }
            catch (Exception exception)
            {
                HasLoadError = true;
                LoadError = $"TID {tid}: failed to assemble monster tables: {exception.Message}";
                AppLog.Error("EnemyAI", LoadError);
                return null;
            }
        }

        /// <summary>清空组装缓存并重新读取所有数据表。</summary>
        public static void Reload()
        {
            Cache.Clear();
            HasLoadError = false;
            LoadError = string.Empty;
            DataManager.Instance.Load();
        }

        private static MonsterAIDefinition Build(
            int tid,
            MonsterAICoreDefine core,
            DataManager manager)
        {
            var definition = new MonsterAIDefinition
            {
                TID = tid,
                Name = core.Name,
                viewRange = core.viewRange,
                loseTargetRange = core.loseTargetRange,
                scanInterval = core.scanInterval,
                runSpeed = core.runSpeed,
                turnSpeed = core.turnSpeed,
                stoppingDistance = core.stoppingDistance,
                hitStunDuration = core.hitStunDuration,
                animation = new AnimationProfileDefinition
                {
                    speedParameter = core.speedParameter,
                    deadParameter = core.deadParameter
                }
            };

            if (manager.MonsterAnimationBindingsByTid.TryGetValue(tid, out var bindings))
            {
                foreach (var row in bindings.OrderBy(item => item.BindingIndex))
                {
                    if (!Enum.TryParse(row.BindingType, true, out AnimationBindingType bindingType))
                    {
                        throw new InvalidOperationException(
                            $"Unknown animation binding type '{row.BindingType}' for '{row.SemanticKey}'.");
                    }

                    definition.animation.bindings.Add(new AnimationBinding
                    {
                        semanticKey = row.SemanticKey,
                        bindingType = bindingType,
                        parameterName = row.ParameterName
                    });
                }
            }

            if (manager.MonsterAbilitiesByTid.TryGetValue(tid, out var abilities))
            {
                foreach (var row in abilities)
                {
                    definition.abilities.Add(new AbilityDefinition
                    {
                        id = row.AbilityId,
                        slot = row.Slot,
                        executor = row.Executor,
                        animationAction = row.AnimationAction,
                        animationState = row.AnimationState,
                        duration = row.Duration,
                        cooldown = row.Cooldown,
                        damageComboIndex = row.DamageComboIndex,
                        ranged = row.Ranged,
                        minRange = row.MinRange,
                        maxRange = row.MaxRange,
                        speed = row.Speed,
                        hitDelay = row.HitDelay,
                        lockMovement = row.LockMovement,
                        stopMovementAtEnd = row.StopMovementAtEnd,
                        oncePerCombat = row.OncePerCombat,
                        damageTakenMultiplier = row.DamageTakenMultiplier,
                        countsAsAttack = row.CountsAsAttack,
                        priority = row.Priority
                    });
                }
            }

            var conditionsByRule = new Dictionary<int, List<MonsterBehaviorConditionDefine>>();
            if (manager.MonsterBehaviorConditionsByTid.TryGetValue(tid, out var conditions))
            {
                conditionsByRule = conditions
                    .GroupBy(item => item.RuleIndex)
                    .ToDictionary(group => group.Key,
                        group => group.OrderBy(item => item.ConditionIndex).ToList());
            }

            if (manager.MonsterBehaviorsByTid.TryGetValue(tid, out var rules))
            {
                foreach (var row in rules.OrderBy(item => item.RuleIndex))
                {
                    var rule = new BehaviorRuleDefinition
                    {
                        action = row.Action,
                        abilityId = row.AbilityId,
                        slot = row.Slot
                    };

                    if (conditionsByRule.TryGetValue(row.RuleIndex, out var ruleConditions))
                    {
                        foreach (var condition in ruleConditions)
                        {
                            rule.conditions.Add(new BehaviorConditionDefinition(condition.ConditionId)
                            {
                                text1 = condition.Text1,
                                text2 = condition.Text2,
                                p1 = condition.P1,
                                p2 = condition.P2
                            });
                        }
                    }
                    definition.behavior.Add(rule);
                }
            }

            return definition;
        }
    }
}
