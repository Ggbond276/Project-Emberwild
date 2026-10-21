using System;
using System.Collections.Generic;
using System.Linq;
using Emberwild.Core.AppLog;
using Emberwild.Define;
using Emberwild.Manager;

namespace Emberwild.AI.Enemy.Ability
{
    public static class MonsterAIDefinitionRepository
    {
        #region 定义缓存 (Definition Cache)
        private static readonly Dictionary<int, MonsterAIDefinition> Cache = new Dictionary<int, MonsterAIDefinition>();
        #endregion

        #region 加载状态 (Load Status)
        public static bool HasLoadError { get; private set; }
        public static string LoadError { get; private set; }
        #endregion

        #region 定义加载与组装 (Definition Loading & Assembly)
        /// <summary>
        /// 按 TID 获取并缓存怪物运行时定义。
        /// </summary>
        public static MonsterAIDefinition Get(int tid)
        {
            TryGet(tid, out var definition, out _);
            return definition;
        }

        /// <summary>尝试获取指定 TID 的怪物定义并返回本次调用的错误。</summary>
        public static bool TryGet(
            int tid,
            out MonsterAIDefinition definition,
            out string error)
        {
            definition = null;
            error = string.Empty;
            HasLoadError = false;
            LoadError = string.Empty;

            if (Cache.TryGetValue(tid, out var cached))
            {
                definition = cached;
                return true;
            }

            var manager = DataManager.Instance;
            if (!manager.MonsterAICores.TryGetValue(tid, out var core))
            {
                error = $"TID {tid}: MonsterAI entry is missing.";
                return false;
            }

            try
            {
                definition = Build(tid, core, manager);
                Cache[tid] = definition;
                return true;
            }
            catch (Exception exception)
            {
                HasLoadError = true;
                error = $"TID {tid}: failed to assemble monster tables: {exception.Message}";
                LoadError = error;
                AppLog.Error("EnemyAI", error);
                return false;
            }
        }

        /// <summary>
        /// 清空缓存并重新读取全部配置表。
        /// </summary>
        public static void Reload()
        {
            Cache.Clear();
            HasLoadError = false;
            LoadError = string.Empty;
            DataManager.Instance.Load();
        }

        /// <summary>
        /// 把五张外联表组装成一只怪物的运行时定义。
        /// </summary>
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
                            $"Unknown animation binding type '{row.BindingType}' "
                            + $"for '{row.SemanticKey}'.");
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
                    .ToDictionary(
                        group => group.Key,
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
                            rule.conditions.Add(
                                new BehaviorConditionDefinition(condition.ConditionId)
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
        #endregion
    }
}
