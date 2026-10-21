using System;
using System.Collections.Generic;
using System.Linq;
using Emberwild.AI.Enemy.Ability;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Emberwild.Editor.MonsterAI
{
    public partial class MonsterAIEditorWindow
    {
        #region 大脑决策树标签页 (Behavior Tab)
        /// <summary>绘制行为规则排序和条件编辑区。</summary>
        private void DrawBehaviorTab()
        {
            var profile = _selectedProfile;
            EditorGUILayout.HelpBox(
                "规则从上到下判断，上方优先级更高。每条规则的条件必须全部满足，"
                + "才会执行该规则的动作。",
                MessageType.Info);

            EnsureRuleList(profile);
            _ruleList.DoLayoutList();

            if (profile.Rules.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "至少需要一条规则。建议最后保留一条“待机”规则。",
                    MessageType.Warning);
                return;
            }

            _selectedRuleIndex = Mathf.Clamp(
                _selectedRuleIndex,
                0,
                profile.Rules.Count - 1);
            var selectedRule = profile.Rules[_selectedRuleIndex];
            DrawRuleDetails(profile, selectedRule, _selectedRuleIndex);
        }

        private void EnsureRuleList(MonsterEditorProfile profile)
        {
            if (_ruleList != null && ReferenceEquals(_ruleList.list, profile.Rules))
                return;

            _ruleList = new ReorderableList(
                profile.Rules,
                typeof(MonsterBehaviorEditorRule),
                true,
                true,
                true,
                true)
            {
                drawHeaderCallback = rect =>
                    EditorGUI.LabelField(rect, "行为优先级（越靠上越先判断）"),
                elementHeight = 34f,
                drawElementCallback = (rect, index, active, focused) =>
                {
                    if (index < 0 || index >= profile.Rules.Count) return;
                    var rule = profile.Rules[index];
                    string action = RuleSummary(profile, rule);
                    EditorGUI.LabelField(
                        new Rect(rect.x + 8f, rect.y + 7f, rect.width - 20f, 20f),
                        $"{index + 1}. {action}    ({rule.Conditions.Count} 个条件)");
                },
                onSelectCallback = list =>
                {
                    _selectedRuleIndex = list.index;
                    _conditionList = null;
                    _conditionListRule = null;
                },
                onReorderCallback = list =>
                {
                    _selectedRuleIndex = list.index;
                    _conditionList = null;
                    _conditionListRule = null;
                    _store.MarkDirty();
                    Repaint();
                },
                onAddCallback = list =>
                {
                    _store.AddRule(profile);
                    _selectedRuleIndex = profile.Rules.Count - 1;
                    list.index = _selectedRuleIndex;
                    _conditionList = null;
                    _conditionListRule = null;
                },
                onRemoveCallback = list =>
                {
                    if (list.index < 0 || list.index >= profile.Rules.Count) return;
                    profile.Rules.RemoveAt(list.index);
                    _selectedRuleIndex = profile.Rules.Count == 0
                        ? -1
                        : Mathf.Clamp(list.index, 0, profile.Rules.Count - 1);
                    _conditionList = null;
                    _conditionListRule = null;
                    _store.MarkDirty();
                }
            };
        }

        private void DrawRuleDetails(
            MonsterEditorProfile profile,
            MonsterBehaviorEditorRule rule,
            int ruleIndex)
        {
            EditorGUILayout.Space(8f);
            DrawSection($"规则 {ruleIndex + 1}");

            EditorGUI.BeginChangeCheck();
            string action = DrawOptionPopup(
                "执行动作",
                rule.Rule.Action,
                MonsterAIEditorOptions.Actions);
            if (EditorGUI.EndChangeCheck())
            {
                rule.Rule.Action = action;
                if (action != BehaviorActions.Ability)
                {
                    rule.Rule.AbilityId = string.Empty;
                    rule.Rule.Slot = string.Empty;
                }
                _store.MarkDirty();
            }

            if (rule.Rule.Action == BehaviorActions.Ability)
            {
                bool useSlot = string.IsNullOrWhiteSpace(rule.Rule.AbilityId)
                    && !string.IsNullOrWhiteSpace(rule.Rule.Slot);
                int mode = EditorGUILayout.Popup(
                    "技能选择方式",
                    useSlot ? 1 : 0,
                    new[] { "选择具体技能", "选择技能槽" });
                if ((mode == 1) != useSlot)
                {
                    useSlot = mode == 1;
                    rule.Rule.AbilityId = string.Empty;
                    rule.Rule.Slot = string.Empty;
                    _store.MarkDirty();
                }

                EditorGUI.BeginChangeCheck();
                if (useSlot)
                {
                    rule.Rule.Slot = DrawSlotPopup(
                        "技能槽",
                        rule.Rule.Slot,
                        profile);
                }
                else
                {
                    rule.Rule.AbilityId = DrawAbilityPopup(
                        "技能",
                        rule.Rule.AbilityId,
                        profile,
                        true);
                }
                if (EditorGUI.EndChangeCheck())
                    _store.MarkDirty();
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("触发条件（全部满足）", EditorStyles.boldLabel);
            EnsureConditionList(profile, rule);
            _conditionList.DoLayoutList();
            EditorGUILayout.HelpBox(
                "不需要任何条件时，可以保留空列表。空列表代表该规则始终成立。",
                MessageType.None);
            EndSection();
        }

        private void EnsureConditionList(
            MonsterEditorProfile profile,
            MonsterBehaviorEditorRule rule)
        {
            if (_conditionList != null && _conditionListRule == rule) return;

            _conditionListRule = rule;
            _conditionList = new ReorderableList(
                rule.Conditions,
                typeof(MonsterBehaviorConditionRow),
                true,
                true,
                true,
                true)
            {
                drawHeaderCallback = rect =>
                    EditorGUI.LabelField(rect, "条件（可拖动排序）"),
                elementHeightCallback = index => index >= 0
                    && index < rule.Conditions.Count
                    ? ConditionHeight(rule.Conditions[index])
                    : 52f,
                drawElementCallback = (rect, index, active, focused) =>
                {
                    if (index < 0 || index >= rule.Conditions.Count) return;
                    DrawConditionElement(rect, profile, rule.Conditions[index]);
                },
                onReorderCallback = list => _store.MarkDirty(),
                onAddCallback = list =>
                {
                    _store.AddCondition(profile, rule);
                    list.index = rule.Conditions.Count - 1;
                },
                onRemoveCallback = list =>
                {
                    if (list.index < 0 || list.index >= rule.Conditions.Count) return;
                    rule.Conditions.RemoveAt(list.index);
                    _store.MarkDirty();
                }
            };
        }

        private float ConditionHeight(MonsterBehaviorConditionRow condition)
        {
            return MonsterAIEditorOptions.UsesAbility(condition.ConditionId)
                || MonsterAIEditorOptions.UsesSlot(condition.ConditionId)
                || MonsterAIEditorOptions.UsesP1(condition.ConditionId)
                || MonsterAIEditorOptions.UsesP2(condition.ConditionId)
                ? 56f
                : 30f;
        }

        private void DrawConditionElement(
            Rect rect,
            MonsterEditorProfile profile,
            MonsterBehaviorConditionRow condition)
        {
            EditorGUI.BeginChangeCheck();
            float width = rect.width - 18f;
            Rect typeRect = new Rect(rect.x + 4f, rect.y + 3f, width, 20f);
            int conditionIndex = MonsterAIEditorOptions.IndexOf(
                MonsterAIEditorOptions.Conditions,
                condition.ConditionId);
            var conditionLabels = MonsterAIEditorOptions.Conditions
                .Select(option => option.Label)
                .ToArray();

            if (conditionIndex >= 0)
            {
                conditionIndex = EditorGUI.Popup(
                    typeRect,
                    conditionIndex,
                    conditionLabels);
                condition.ConditionId =
                    MonsterAIEditorOptions.Conditions[conditionIndex].Value;
            }
            else
            {
                var values = new List<string> { condition.ConditionId ?? string.Empty };
                var labels = new List<string>
                {
                    $"未知条件：{condition.ConditionId}"
                };
                values.AddRange(MonsterAIEditorOptions.Conditions
                    .Select(option => option.Value));
                labels.AddRange(conditionLabels);
                int selected = EditorGUI.Popup(typeRect, 0, labels.ToArray());
                condition.ConditionId = values[selected];
            }

            Rect fieldRect = new Rect(
                rect.x + 4f,
                rect.y + 27f,
                width,
                20f);
            DrawConditionFields(fieldRect, profile, condition);
            if (EditorGUI.EndChangeCheck())
                _store.MarkDirty();
        }

        private void DrawConditionFields(
            Rect rect,
            MonsterEditorProfile profile,
            MonsterBehaviorConditionRow condition)
        {
            if (MonsterAIEditorOptions.UsesAbility(condition.ConditionId))
            {
                Rect abilityRect = new Rect(rect.x, rect.y, rect.width * 0.55f, rect.height);
                Rect cooldownRect = new Rect(
                    rect.x + rect.width * 0.57f,
                    rect.y,
                    rect.width * 0.43f,
                    rect.height);
                condition.Text1 = DrawAbilityPopupRect(
                    abilityRect,
                    condition.Text1,
                    profile,
                    false);
                condition.P1 = EditorGUI.FloatField(
                    cooldownRect,
                    new GUIContent("冷却秒数"),
                    condition.P1);
                return;
            }

            if (MonsterAIEditorOptions.UsesSlot(condition.ConditionId))
            {
                float cooldownWidth = condition.ConditionId == BehaviorConditions.SlotReady
                    ? rect.width * 0.32f
                    : 0f;
                Rect slotRect = new Rect(
                    rect.x,
                    rect.y,
                    rect.width - cooldownWidth,
                    rect.height);
                condition.Text1 = DrawSlotPopupRect(
                    slotRect,
                    condition.Text1,
                    profile);
                if (cooldownWidth > 0f)
                {
                    condition.P1 = EditorGUI.FloatField(
                        new Rect(
                            rect.x + slotRect.width + 4f,
                            rect.y,
                            cooldownWidth - 4f,
                            rect.height),
                        new GUIContent("冷却秒数"),
                        condition.P1);
                }
                return;
            }

            if (condition.ConditionId == BehaviorConditions.HpBelow)
            {
                condition.P1 = EditorGUI.Slider(
                    rect,
                    new GUIContent("血量低于比例"),
                    condition.P1,
                    0f,
                    1f);
                return;
            }

            if (condition.ConditionId == BehaviorConditions.InRange)
            {
                float half = rect.width * 0.5f;
                condition.P1 = EditorGUI.FloatField(
                    new Rect(rect.x, rect.y, half - 2f, rect.height),
                    new GUIContent("最小距离"),
                    condition.P1);
                condition.P2 = EditorGUI.FloatField(
                    new Rect(rect.x + half + 2f, rect.y, half - 2f, rect.height),
                    new GUIContent("最大距离"),
                    condition.P2);
                return;
            }

            if (condition.ConditionId == BehaviorConditions.AttackCooldownReady)
            {
                condition.P1 = EditorGUI.FloatField(
                    rect,
                    new GUIContent("攻击冷却秒数"),
                    condition.P1);
            }
        }

        private string RuleSummary(
            MonsterEditorProfile profile,
            MonsterBehaviorEditorRule rule)
        {
            string action = MonsterAIEditorOptions.LabelOf(
                MonsterAIEditorOptions.Actions,
                rule.Rule.Action);
            if (rule.Rule.Action != BehaviorActions.Ability)
                return action;

            if (!string.IsNullOrWhiteSpace(rule.Rule.AbilityId))
                return $"{action}：{rule.Rule.AbilityId}";
            if (!string.IsNullOrWhiteSpace(rule.Rule.Slot))
            {
                string slotLabel = MonsterAIEditorOptions.LabelOf(
                    MonsterAIEditorOptions.Slots,
                    rule.Rule.Slot);
                return $"{action}：{slotLabel}";
            }
            return $"{action}：未选择";
        }
        #endregion

        #region 条件下拉工具 (Condition Dropdown Helpers)
        private static string DrawAbilityPopupRect(
            Rect rect,
            string value,
            MonsterEditorProfile profile,
            bool allowEmpty)
        {
            var values = new List<string> { string.Empty };
            var labels = new List<string> { allowEmpty ? "无" : "请选择" };
            values.AddRange(profile.Abilities.Select(row => row.AbilityId));
            labels.AddRange(profile.Abilities.Select(row => row.AbilityId));

            int index = values.FindIndex(item => string.Equals(
                item,
                value,
                StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                values.Add(value ?? string.Empty);
                labels.Add($"缺失：{value}");
                index = values.Count - 1;
            }

            int next = EditorGUI.Popup(rect, index, labels.ToArray());
            return values[next];
        }

        private static string DrawSlotPopupRect(
            Rect rect,
            string value,
            MonsterEditorProfile profile)
        {
            var values = new List<string> { string.Empty };
            var labels = new List<string> { "无" };
            var slots = profile.Abilities
                .Where(row => !string.IsNullOrWhiteSpace(row.Slot))
                .Select(row => row.Slot)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            values.AddRange(slots);
            labels.AddRange(slots.Select(slot =>
                MonsterAIEditorOptions.LabelOf(MonsterAIEditorOptions.Slots, slot)));

            int index = values.FindIndex(item => string.Equals(
                item,
                value,
                StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                values.Add(value ?? string.Empty);
                labels.Add($"缺失：{value}");
                index = values.Count - 1;
            }

            int next = EditorGUI.Popup(rect, index, labels.ToArray());
            return values[next];
        }
        #endregion
    }
}
