using System;
using System.Collections.Generic;
using Emberwild.Manager;

namespace Emberwild.AI.Enemy.Ability
{
    public static class MonsterAIDefinitionValidator
    {
        #region 怪物定义校验 (Monster Definition Validation)
        /// <summary>校验怪物定义的结构、引用和动画绑定。</summary>
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
                error = $"TID {definition.TID}: animation profile must define "
                    + "speedParameter and deadParameter.";
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
                    error = $"TID {definition.TID}: ability '{ability.id}' uses "
                        + $"unknown executor '{ability.executor}'.";
                    return false;
                }
                if (ability.maxRange < ability.minRange)
                {
                    error = $"TID {definition.TID}: ability '{ability.id}' has "
                        + "maxRange below minRange.";
                    return false;
                }
                if (ability.duration < 0f || ability.cooldown < 0f || ability.speed < 0f)
                {
                    error = $"TID {definition.TID}: ability '{ability.id}' has "
                        + "a negative timing or speed.";
                    return false;
                }
            }

            if (!slots.Contains(AbilitySlotIds.HitReaction))
            {
                error = $"TID {definition.TID}: missing "
                    + $"'{AbilitySlotIds.HitReaction}' ability.";
                return false;
            }
            if (!hasDeathExecutor)
            {
                error = $"TID {definition.TID}: missing an ability using executor "
                    + $"'{AbilityExecutorIds.Dead}'.";
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
                    error = $"TID {definition.TID}: behavior references missing ability "
                        + $"'{rule.abilityId}' or slot '{rule.slot}'.";
                    return false;
                }
                if (rule.conditions != null)
                {
                    foreach (var condition in rule.conditions)
                    {
                        if (condition == null
                            || !BehaviorConditionRegistry.Contains(condition.id))
                        {
                            error = $"TID {definition.TID}: behavior uses "
                                + $"unknown condition '{condition?.id}'.";
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
                    error = $"TID {definition.TID}: missing animation binding "
                        + $"'{ability.animationAction}' for ability '{ability.id}'.";
                    return false;
                }
                if (!string.IsNullOrWhiteSpace(ability.animationState)
                    && !HasBinding(definition.animation, ability.animationState))
                {
                    error = $"TID {definition.TID}: missing animation state binding "
                        + $"'{ability.animationState}' for ability '{ability.id}'.";
                    return false;
                }
            }
            return true;
        }

        /// <summary>校验伤害能力引用的角色和攻击配置。</summary>
        public static bool ValidateCombatLinks(
            MonsterAIDefinition definition,
            int tid,
            out string error)
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

        /// <summary>判断动画配置中是否存在指定语义绑定。</summary>
        private static bool HasBinding(
            AnimationProfileDefinition profile,
            string semantic)
        {
            if (profile == null || profile.bindings == null) return false;
            foreach (var binding in profile.bindings)
            {
                if (binding != null
                    && string.Equals(
                        binding.semanticKey,
                        semantic,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
        #endregion
    }
}
