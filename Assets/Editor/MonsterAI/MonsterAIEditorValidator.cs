using System;
using System.Collections.Generic;
using System.Linq;
using Emberwild.AI.Enemy.Ability;
using UnityEngine;

namespace Emberwild.Editor.MonsterAI
{
    public static class MonsterAIEditorValidator
    {
        #region 校验入口 (Validation Entry)
        /// <summary>检查全部怪物的引用、数值和必要能力。</summary>
        public static List<MonsterAIEditorIssue> Validate(
            MonsterAIEditorStore store)
        {
            var issues = new List<MonsterAIEditorIssue>();
            if (store == null)
            {
                AddError(issues, 0, "配置数据尚未加载。");
                return issues;
            }

            var subTypeOwners = new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var profile in store.Monsters)
            {
                ValidateProfile(store, profile, issues, subTypeOwners);
            }

            ValidateAttacks(store, issues);
            return issues;
        }
        #endregion

        #region 怪物校验 (Monster Validation)
        private static void ValidateProfile(
            MonsterAIEditorStore store,
            MonsterEditorProfile profile,
            List<MonsterAIEditorIssue> issues,
            Dictionary<string, int> subTypeOwners)
        {
            int tid = profile.TID;
            if (profile.Character == null || profile.AI == null)
            {
                AddError(issues, tid, "CharacterDefine 或 MonsterAI 数据缺失。");
                return;
            }

            if (tid <= 0)
                AddError(issues, tid, "TID 必须大于 0。");
            if (string.IsNullOrWhiteSpace(profile.Character.Name))
                AddError(issues, tid, "怪物名称不能为空。");
            if (string.IsNullOrWhiteSpace(profile.Character.SubType))
            {
                AddError(issues, tid, "SubType 不能为空，它用于关联攻击组。");
            }
            else if (subTypeOwners.TryGetValue(
                         profile.Character.SubType,
                         out int otherTid))
            {
                AddWarning(
                    issues,
                    tid,
                    $"SubType 与 TID {otherTid} 共用，攻击配置也会共享。");
            }
            else
            {
                subTypeOwners[profile.Character.SubType] = tid;
            }

            if (string.IsNullOrWhiteSpace(profile.Character.Resource))
            {
                AddError(issues, tid, "Resource 不能为空。");
            }
            else if (Resources.Load<GameObject>(profile.Character.Resource) == null)
            {
                AddError(
                    issues,
                    tid,
                    $"找不到 Resources 路径：{profile.Character.Resource}");
            }

            if (profile.AI.viewRange <= 0f)
                AddError(issues, tid, "视野范围必须大于 0。");
            if (profile.AI.loseTargetRange < profile.AI.viewRange)
                AddWarning(issues, tid, "丢失目标距离小于视野范围。");
            if (profile.AI.scanInterval <= 0f)
                AddError(issues, tid, "感知扫描间隔必须大于 0。");
            if (profile.AI.runSpeed <= 0f)
                AddError(issues, tid, "移动速度必须大于 0。");
            if (string.IsNullOrWhiteSpace(profile.AI.speedParameter)
                || string.IsNullOrWhiteSpace(profile.AI.deadParameter))
            {
                AddError(issues, tid, "Animator 的移动和死亡参数不能为空。");
            }

            ValidateAbilities(store, profile, issues);
            ValidateAnimations(profile, issues);
            ValidateRules(profile, issues);
        }

        private static void ValidateAbilities(
            MonsterAIEditorStore store,
            MonsterEditorProfile profile,
            List<MonsterAIEditorIssue> issues)
        {
            int tid = profile.TID;
            if (profile.Abilities.Count == 0)
            {
                AddError(issues, tid, "至少需要配置一个能力。");
                return;
            }

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool hasHitReaction = false;
            bool hasDeath = false;

            foreach (var ability in profile.Abilities)
            {
                if (string.IsNullOrWhiteSpace(ability.AbilityId))
                {
                    AddError(issues, tid, "存在未填写 AbilityId 的技能。");
                }
                else if (!ids.Add(ability.AbilityId))
                {
                    AddError(issues, tid, $"AbilityId 重复：{ability.AbilityId}");
                }

                if (MonsterAIEditorOptions.IndexOf(
                        MonsterAIEditorOptions.Executors,
                        ability.Executor) < 0)
                {
                    AddError(
                        issues,
                        tid,
                        $"技能 {ability.AbilityId} 的执行器无效：{ability.Executor}");
                }

                if (ability.Slot == AbilitySlotIds.HitReaction)
                    hasHitReaction = true;
                if (ability.Executor == AbilityExecutorIds.Dead)
                    hasDeath = true;

                if (ability.MaxRange < ability.MinRange)
                {
                    AddError(
                        issues,
                        tid,
                        $"技能 {ability.AbilityId} 的最大距离小于最小距离。");
                }
                if (ability.Duration < 0f || ability.Cooldown < 0f
                    || ability.HitDelay < 0f || ability.Speed < 0f)
                {
                    AddError(
                        issues,
                        tid,
                        $"技能 {ability.AbilityId} 存在负数时间或速度。");
                }

                ValidateAbilityAnimation(profile, ability, issues);

                if (ability.DamageComboIndex >= 0
                    && store.FindAttack(profile, ability.DamageComboIndex) == null)
                {
                    AddError(
                        issues,
                        tid,
                        $"技能 {ability.AbilityId} 引用的攻击段不存在："
                        + $"{profile.Character.SubType}:{ability.DamageComboIndex}");
                }
            }

            if (!hasHitReaction)
                AddError(issues, tid, "缺少 reaction.hit 受击能力。");
            if (!hasDeath)
                AddError(issues, tid, "缺少 dead 执行器死亡能力。");
        }

        private static void ValidateAbilityAnimation(
            MonsterEditorProfile profile,
            MonsterAbilityRow ability,
            List<MonsterAIEditorIssue> issues)
        {
            if (!string.IsNullOrWhiteSpace(ability.AnimationAction)
                && !HasAnimation(profile, ability.AnimationAction))
            {
                AddError(
                    issues,
                    profile.TID,
                    $"技能 {ability.AbilityId} 缺少动画映射：{ability.AnimationAction}");
            }

            if (!string.IsNullOrWhiteSpace(ability.AnimationState)
                && !HasAnimation(profile, ability.AnimationState))
            {
                AddError(
                    issues,
                    profile.TID,
                    $"技能 {ability.AbilityId} 缺少状态映射：{ability.AnimationState}");
            }
        }

        private static void ValidateAnimations(
            MonsterEditorProfile profile,
            List<MonsterAIEditorIssue> issues)
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var animation in profile.Animations)
            {
                if (string.IsNullOrWhiteSpace(animation.SemanticKey))
                {
                    AddError(issues, profile.TID, "存在未选择语义键的动画映射。");
                    continue;
                }
                if (!keys.Add(animation.SemanticKey))
                {
                    AddError(
                        issues,
                        profile.TID,
                        $"动画语义键重复：{animation.SemanticKey}");
                }
                if (string.IsNullOrWhiteSpace(animation.ParameterName))
                {
                    AddError(
                        issues,
                        profile.TID,
                        $"动画 {animation.SemanticKey} 未填写 Animator 参数名。");
                }
                if (MonsterAIEditorOptions.IndexOf(
                        MonsterAIEditorOptions.BindingTypes,
                        animation.BindingType) < 0)
                {
                    AddError(
                        issues,
                        profile.TID,
                        $"动画 {animation.SemanticKey} 的绑定类型无效。");
                }
            }
        }

        private static void ValidateRules(
            MonsterEditorProfile profile,
            List<MonsterAIEditorIssue> issues)
        {
            int tid = profile.TID;
            if (profile.Rules.Count == 0)
            {
                AddError(issues, tid, "至少需要一条行为规则。");
                return;
            }

            var abilityIds = new HashSet<string>(
                profile.Abilities.Select(row => row.AbilityId)
                    .Where(value => !string.IsNullOrWhiteSpace(value)),
                StringComparer.OrdinalIgnoreCase);
            var slots = new HashSet<string>(
                profile.Abilities.Select(row => row.Slot)
                    .Where(value => !string.IsNullOrWhiteSpace(value)),
                StringComparer.OrdinalIgnoreCase);

            for (int ruleIndex = 0; ruleIndex < profile.Rules.Count; ruleIndex++)
            {
                var rule = profile.Rules[ruleIndex];
                if (MonsterAIEditorOptions.IndexOf(
                        MonsterAIEditorOptions.Actions,
                        rule.Rule.Action) < 0)
                {
                    AddError(
                        issues,
                        tid,
                        $"第 {ruleIndex + 1} 条规则的动作无效：{rule.Rule.Action}");
                }

                if (rule.Rule.Action == BehaviorActions.Ability)
                {
                    bool hasAbility = !string.IsNullOrWhiteSpace(rule.Rule.AbilityId)
                        && abilityIds.Contains(rule.Rule.AbilityId);
                    bool hasSlot = !string.IsNullOrWhiteSpace(rule.Rule.Slot)
                        && slots.Contains(rule.Rule.Slot);
                    if (!hasAbility && !hasSlot)
                    {
                        AddError(
                            issues,
                            tid,
                            $"第 {ruleIndex + 1} 条规则没有引用有效技能或技能槽。");
                    }
                }

                for (int conditionIndex = 0;
                     conditionIndex < rule.Conditions.Count;
                     conditionIndex++)
                {
                    ValidateCondition(
                        profile,
                        rule.Conditions[conditionIndex],
                        ruleIndex,
                        conditionIndex,
                        abilityIds,
                        slots,
                        issues);
                }
            }
        }

        private static void ValidateCondition(
            MonsterEditorProfile profile,
            MonsterBehaviorConditionRow condition,
            int ruleIndex,
            int conditionIndex,
            HashSet<string> abilityIds,
            HashSet<string> slots,
            List<MonsterAIEditorIssue> issues)
        {
            int tid = profile.TID;
            string location = $"规则 {ruleIndex + 1} 条件 {conditionIndex + 1}";
            if (!MonsterAIEditorOptions.IsKnownCondition(condition.ConditionId))
            {
                AddError(
                    issues,
                    tid,
                    $"{location} 使用未知条件：{condition.ConditionId}");
                return;
            }

            if (MonsterAIEditorOptions.UsesAbility(condition.ConditionId)
                && !abilityIds.Contains(condition.Text1 ?? string.Empty))
            {
                AddError(issues, tid, $"{location} 引用了不存在的技能：{condition.Text1}");
            }
            if (MonsterAIEditorOptions.UsesSlot(condition.ConditionId)
                && !slots.Contains(condition.Text1 ?? string.Empty))
            {
                AddError(issues, tid, $"{location} 引用了不存在的技能槽：{condition.Text1}");
            }
            if (condition.ConditionId == BehaviorConditions.HpBelow
                && (condition.P1 <= 0f || condition.P1 > 1f))
            {
                AddError(issues, tid, $"{location} 的血量比例必须在 0 到 1 之间。");
            }
            if (condition.ConditionId == BehaviorConditions.InRange
                && condition.P2 < condition.P1)
            {
                AddError(issues, tid, $"{location} 的最大距离小于最小距离。");
            }
            if ((condition.ConditionId == BehaviorConditions.AbilityReady
                 || condition.ConditionId == BehaviorConditions.SlotReady
                 || condition.ConditionId == BehaviorConditions.AttackCooldownReady)
                && condition.P1 < 0f)
            {
                AddError(issues, tid, $"{location} 的冷却时间不能为负数。");
            }
        }
        #endregion

        #region 攻击配置校验 (Attack Validation)
        private static void ValidateAttacks(
            MonsterAIEditorStore store,
            List<MonsterAIEditorIssue> issues)
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var attack in store.Attacks)
            {
                if (string.IsNullOrWhiteSpace(attack.ComboGroup))
                {
                    AddError(issues, 0, "存在未填写 ComboGroup 的攻击配置。");
                    continue;
                }

                string key = $"{attack.ComboGroup}:{attack.ComboIndex}";
                if (!keys.Add(key))
                    AddError(issues, 0, $"攻击段重复：{key}");
                if (attack.HitboxType < 1 || attack.HitboxType > 3)
                    AddError(issues, 0, $"攻击段 {key} 的 HitboxType 必须为 1、2 或 3。");
                if (attack.DmgRate < 0f || attack.Param1 < 0f
                    || attack.Param2 < 0f || attack.Knockback < 0f)
                {
                    AddError(issues, 0, $"攻击段 {key} 存在负数参数。");
                }
            }
        }
        #endregion

        #region 内部工具 (Internal Helpers)
        private static bool HasAnimation(
            MonsterEditorProfile profile,
            string semanticKey)
        {
            return profile.Animations.Any(animation => string.Equals(
                animation.SemanticKey,
                semanticKey,
                StringComparison.OrdinalIgnoreCase));
        }

        private static void AddError(
            List<MonsterAIEditorIssue> issues,
            int tid,
            string message)
        {
            issues.Add(new MonsterAIEditorIssue(
                MonsterAIEditorIssueLevel.Error,
                tid,
                message));
        }

        private static void AddWarning(
            List<MonsterAIEditorIssue> issues,
            int tid,
            string message)
        {
            issues.Add(new MonsterAIEditorIssue(
                MonsterAIEditorIssueLevel.Warning,
                tid,
                message));
        }
        #endregion
    }
}
