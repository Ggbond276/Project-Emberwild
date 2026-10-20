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

    /// <summary>
    /// 校验怪物定义中的能力、行为、动画映射和必要反应槽。
    /// </summary>
    public static class MonsterAIDefinitionValidator
    {
        /// <summary>校验怪物定义的结构完整性。</summary>
        public static bool Validate(MonsterAIDefinition definition, out string error)
        {
            error = string.Empty;
            if (definition == null)
            {
                error = "Monster AI definition is null.";
                return false;
            }
            if (definition.abilities == null || definition.abilities.Count == 0)
            {
                error = $"TID {definition.TID}: no abilities configured.";
                return false;
            }
            if (definition.behavior == null || definition.behavior.Count == 0)
            {
                error = $"TID {definition.TID}: no behavior rules configured.";
                return false;
            }
            if (definition.animation == null
                || string.IsNullOrWhiteSpace(definition.animation.speedParameter)
                || string.IsNullOrWhiteSpace(definition.animation.deadParameter))
            {
                error = $"TID {definition.TID}: animation profile must define speedParameter and deadParameter.";
                return false;
            }

            var abilityIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var slots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool hasDeathExecutor = false;
            foreach (var ability in definition.abilities)
            {
                if (ability == null || string.IsNullOrWhiteSpace(ability.id))
                {
                    error = $"TID {definition.TID}: ability with empty id.";
                    return false;
                }
                if (!abilityIds.Add(ability.id))
                {
                    error = $"TID {definition.TID}: duplicate ability id '{ability.id}'.";
                    return false;
                }
                if (!string.IsNullOrWhiteSpace(ability.slot)) slots.Add(ability.slot);
                if (ability.executor == AbilityExecutorIds.Dead) hasDeathExecutor = true;
                if (!AbilityExecutorRegistry.Contains(ability.executor))
                {
                    error = $"TID {definition.TID}: ability '{ability.id}' uses unknown executor '{ability.executor}'.";
                    return false;
                }
                if (ability.maxRange < ability.minRange)
                {
                    error = $"TID {definition.TID}: ability '{ability.id}' has maxRange below minRange.";
                    return false;
                }
                if (ability.duration < 0f || ability.cooldown < 0f || ability.speed < 0f)
                {
                    error = $"TID {definition.TID}: ability '{ability.id}' has a negative timing or speed.";
                    return false;
                }
            }
            if (!slots.Contains(AbilitySlotIds.HitReaction))
            {
                error = $"TID {definition.TID}: missing '{AbilitySlotIds.HitReaction}' ability.";
                return false;
            }
            if (!hasDeathExecutor)
            {
                error = $"TID {definition.TID}: missing an ability using executor '{AbilityExecutorIds.Dead}'.";
                return false;
            }

            foreach (var rule in definition.behavior)
            {
                if (rule == null) continue;
                if (rule.action != BehaviorActions.Ability
                    && rule.action != BehaviorActions.Chase
                    && rule.action != BehaviorActions.Idle)
                {
                    error = $"TID {definition.TID}: unknown behavior action '{rule.action}'.";
                    return false;
                }
                if (rule.action == BehaviorActions.Ability
                    && !abilityIds.Contains(rule.abilityId ?? string.Empty)
                    && !slots.Contains(rule.slot ?? string.Empty))
                {
                    error = $"TID {definition.TID}: behavior references missing ability '{rule.abilityId}' or slot '{rule.slot}'.";
                    return false;
                }
                if (rule.conditions != null)
                {
                    foreach (var condition in rule.conditions)
                    {
                        if (condition == null || !BehaviorConditionRegistry.Contains(condition.id))
                        {
                            error = $"TID {definition.TID}: behavior uses unknown condition '{condition?.id}'.";
                            return false;
                        }
                    }
                }
            }

            foreach (var ability in definition.abilities)
            {
                if (!string.IsNullOrWhiteSpace(ability.animationAction)
                    && !HasBinding(definition.animation, ability.animationAction))
                {
                    error = $"TID {definition.TID}: missing animation binding '{ability.animationAction}' for ability '{ability.id}'.";
                    return false;
                }
                if (!string.IsNullOrWhiteSpace(ability.animationState)
                    && !HasBinding(definition.animation, ability.animationState))
                {
                    error = $"TID {definition.TID}: missing animation state binding '{ability.animationState}' for ability '{ability.id}'.";
                    return false;
                }
            }
            return true;
        }

        /// <summary>校验伤害能力引用的 CharacterDefine 和 AttackDefine 是否真实存在。</summary>
        public static bool ValidateCombatLinks(MonsterAIDefinition definition, int tid, out string error)
        {
            error = string.Empty;
            if (definition == null) return true;

            var manager = DataManager.Instance;
            if (manager.Characters == null
                || !manager.Characters.TryGetValue(tid, out var character))
            {
                error = $"TID {tid}: CharacterDefine entry is missing.";
                return false;
            }

            manager.ComboGroupDict.TryGetValue(character.SubType, out var attacks);
            foreach (var ability in definition.abilities)
            {
                if (ability == null || ability.damageComboIndex < 0) continue;
                if (attacks == null || !attacks.ContainsKey(ability.damageComboIndex))
                {
                    error = $"TID {tid}: ability '{ability.id}' references missing attack "
                        + $"'{character.SubType}:{ability.damageComboIndex}'.";
                    return false;
                }
            }
            return true;
        }

        private static bool HasBinding(AnimationProfileDefinition profile, string semantic)
        {
            if (profile == null || profile.bindings == null) return false;
            foreach (var binding in profile.bindings)
            {
                if (binding != null && string.Equals(binding.semanticKey, semantic, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
