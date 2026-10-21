using System;
using System.Collections.Generic;
using System.Linq;
using Emberwild.AI.Enemy.Ability;
using UnityEditor;
using UnityEngine;

namespace Emberwild.Editor.MonsterAI
{
    public partial class MonsterAIEditorWindow
    {
        #region 技能与打击盒标签页 (Abilities & Attacks Tab)
        /// <summary>绘制技能、动画映射和攻击判定配置。</summary>
        private void DrawAbilitiesTab()
        {
            var profile = _selectedProfile;
            DrawSection("技能列表");
            EditorGUILayout.HelpBox(
                "技能定义“做什么”，动画映射定义“怎么表现”，攻击配置定义“如何判定伤害”。",
                MessageType.Info);
            if (GUILayout.Button("+ 新增技能", GUILayout.Height(26f)))
            {
                _store.AddAbility(profile);
                _store.MarkDirty();
            }
            EndSection();

            for (int index = 0; index < profile.Abilities.Count; index++)
            {
                var ability = profile.Abilities[index];
                bool delete = DrawAbilityCard(profile, ability, index);
                if (!delete) continue;

                profile.Abilities.RemoveAt(index);
                _store.MarkDirty();
                break;
            }

            DrawAnimationSection(profile);
            DrawAttackSection(profile);
        }

        private bool DrawAbilityCard(
            MonsterEditorProfile profile,
            MonsterAbilityRow ability,
            int index)
        {
            bool delete = false;
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            ability.IsExpanded = EditorGUILayout.Foldout(
                ability.IsExpanded,
                $"{index + 1}. {ability.AbilityId}",
                true);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("删除", GUILayout.Width(52f)))
                delete = true;
            EditorGUILayout.EndHorizontal();

            if (ability.IsExpanded)
            {
                EditorGUI.BeginChangeCheck();
                ability.AbilityId = EditorGUILayout.TextField(
                    new GUIContent("AbilityId", "同一只怪物内必须唯一。"),
                    ability.AbilityId);
                ability.Slot = DrawOptionPopup(
                    "技能槽位",
                    ability.Slot,
                    MonsterAIEditorOptions.Slots);
                ability.Executor = DrawOptionPopup(
                    "执行器",
                    ability.Executor,
                    MonsterAIEditorOptions.Executors);

                EditorGUILayout.Space(4f);
                ability.AnimationAction = DrawOptionPopup(
                    "一次性动画语义",
                    ability.AnimationAction,
                    MonsterAIEditorOptions.AnimationSemanticOptions);
                ability.AnimationState = DrawOptionPopup(
                    "持续状态语义",
                    ability.AnimationState,
                    MonsterAIEditorOptions.AnimationSemanticOptions);

                EditorGUILayout.Space(4f);
                ability.Duration = EditorGUILayout.FloatField(
                    new GUIContent("持续时间（秒）"),
                    ability.Duration);
                ability.Cooldown = EditorGUILayout.FloatField(
                    new GUIContent("冷却时间（秒）"),
                    ability.Cooldown);
                ability.HitDelay = EditorGUILayout.FloatField(
                    new GUIContent("命中延迟（秒）", "能力开始后多久触发攻击判定。"),
                    ability.HitDelay);
                ability.Speed = EditorGUILayout.FloatField(
                    new GUIContent("位移速度", "仅位移类执行器使用。"),
                    ability.Speed);
                ability.MinRange = EditorGUILayout.FloatField(
                    new GUIContent("最小距离"),
                    ability.MinRange);
                ability.MaxRange = EditorGUILayout.FloatField(
                    new GUIContent("最大距离"),
                    ability.MaxRange);

                EditorGUILayout.Space(4f);
                ability.LockMovement = EditorGUILayout.Toggle(
                    new GUIContent("执行时锁定移动"),
                    ability.LockMovement);
                ability.StopMovementAtEnd = EditorGUILayout.Toggle(
                    new GUIContent("结束时停止移动"),
                    ability.StopMovementAtEnd);
                ability.OncePerCombat = EditorGUILayout.Toggle(
                    new GUIContent("每场战斗只使用一次"),
                    ability.OncePerCombat);
                ability.CountsAsAttack = EditorGUILayout.Toggle(
                    new GUIContent("计入全局攻击冷却"),
                    ability.CountsAsAttack);
                ability.Ranged = EditorGUILayout.Toggle(
                    new GUIContent("远程攻击"),
                    ability.Ranged);
                ability.DamageTakenMultiplier = EditorGUILayout.FloatField(
                    new GUIContent("承伤倍率", "仅持续姿态类能力使用，1 表示不减伤。"),
                    ability.DamageTakenMultiplier);
                ability.Priority = EditorGUILayout.IntField(
                    new GUIContent("同槽位优先级", "数值越大越优先尝试。"),
                    ability.Priority);

                EditorGUILayout.Space(4f);
                ability.DamageComboIndex = DrawComboIndexPopup(
                    profile,
                    ability.DamageComboIndex);
                EditorGUILayout.HelpBox(
                    "选择攻击段后，可在页面底部编辑该段的伤害倍率、判定形状和范围参数。",
                    MessageType.None);

                if (EditorGUI.EndChangeCheck())
                    _store.MarkDirty();
            }

            EditorGUILayout.EndVertical();
            return delete;
        }

        private void DrawAnimationSection(MonsterEditorProfile profile)
        {
            DrawSection("动画映射");
            EditorGUILayout.HelpBox(
                "同一个语义键在一只怪物中只能映射一次。多个技能可以共享同一动画语义。",
                MessageType.Info);
            if (GUILayout.Button("+ 新增动画映射", GUILayout.Height(24f)))
                _store.AddAnimation(profile);
            EndSection();

            for (int index = 0; index < profile.Animations.Count; index++)
            {
                var animation = profile.Animations[index];
                bool delete = false;
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                animation.IsExpanded = EditorGUILayout.Foldout(
                    animation.IsExpanded,
                    string.IsNullOrWhiteSpace(animation.SemanticKey)
                        ? $"动画映射 {index + 1}"
                        : animation.SemanticKey,
                    true);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("删除", GUILayout.Width(52f)))
                    delete = true;
                EditorGUILayout.EndHorizontal();

                if (animation.IsExpanded)
                {
                    EditorGUI.BeginChangeCheck();
                    animation.SemanticKey = DrawOptionPopup(
                        "动画语义键",
                        animation.SemanticKey,
                        MonsterAIEditorOptions.AnimationSemanticOptions);
                    animation.BindingType = DrawOptionPopup(
                        "Animator 参数类型",
                        animation.BindingType,
                        MonsterAIEditorOptions.BindingTypes);
                    animation.ParameterName = EditorGUILayout.TextField(
                        new GUIContent(
                            "Animator 参数名",
                            "必须是该怪物 Animator Controller 中真实存在的名称。"),
                        animation.ParameterName);
                    if (EditorGUI.EndChangeCheck())
                        _store.MarkDirty();
                }

                EditorGUILayout.EndVertical();
                if (!delete) continue;

                profile.Animations.RemoveAt(index);
                _store.MarkDirty();
                break;
            }
        }

        private void DrawAttackSection(MonsterEditorProfile profile)
        {
            string comboGroup = profile.Character?.SubType ?? string.Empty;
            DrawSection("攻击判定盒");
            if (string.IsNullOrWhiteSpace(comboGroup))
            {
                EditorGUILayout.HelpBox(
                    "请先在“基础与躯体”中填写 SubType，才能关联攻击组。",
                    MessageType.Error);
                EndSection();
                return;
            }

            EditorGUILayout.LabelField("当前攻击组", comboGroup);
            EditorGUILayout.HelpBox(
                "圆形：参数1=半径。矩形：参数1=宽度，参数2=长度。"
                + "扇形：参数1=半径，参数2=角度。",
                MessageType.Info);
            if (GUILayout.Button("+ 新增攻击段", GUILayout.Height(24f)))
                _store.AddAttack(profile);
            EndSection();

            var attacks = _store.GetAttacks(comboGroup).ToList();
            if (attacks.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "当前攻击组还没有攻击段。纯追逐或纯待机怪物可以暂时不配置。",
                    MessageType.Warning);
                return;
            }

            foreach (var attack in attacks)
            {
                bool delete = DrawAttackCard(attack);
                if (!delete) continue;

                _store.Attacks.Remove(attack);
                _store.MarkDirty();
                break;
            }
        }

        private bool DrawAttackCard(MonsterAttackRow attack)
        {
            bool delete = false;
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            attack.IsExpanded = EditorGUILayout.Foldout(
                attack.IsExpanded,
                $"攻击段 {attack.ComboIndex}",
                true);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("删除", GUILayout.Width(52f)))
                delete = true;
            EditorGUILayout.EndHorizontal();

            if (attack.IsExpanded)
            {
                EditorGUI.BeginChangeCheck();
                attack.ComboGroup = EditorGUILayout.TextField(
                    new GUIContent("攻击组", "必须与角色 SubType 一致。"),
                    attack.ComboGroup);
                attack.ComboIndex = EditorGUILayout.IntField(
                    new GUIContent("攻击段编号", "同一攻击组内必须唯一。"),
                    attack.ComboIndex);
                attack.DmgRate = EditorGUILayout.FloatField(
                    new GUIContent("伤害倍率", "最终伤害会乘以角色 AD/DEF 计算结果。"),
                    attack.DmgRate);
                attack.HitboxType = DrawIntOptionPopup(
                    "判定形状",
                    attack.HitboxType,
                    MonsterAIEditorOptions.HitboxTypes);
                attack.OffsetZ = EditorGUILayout.FloatField(
                    new GUIContent("判定中心前移距离"),
                    attack.OffsetZ);
                if (attack.HitboxType == 1)
                {
                    attack.Param1 = EditorGUILayout.FloatField(
                        new GUIContent("圆形半径"),
                        attack.Param1);
                }
                else if (attack.HitboxType == 2)
                {
                    attack.Param1 = EditorGUILayout.FloatField(
                        new GUIContent("矩形宽度"),
                        attack.Param1);
                    attack.Param2 = EditorGUILayout.FloatField(
                        new GUIContent("矩形长度"),
                        attack.Param2);
                }
                else
                {
                    attack.Param1 = EditorGUILayout.FloatField(
                        new GUIContent("扇形半径"),
                        attack.Param1);
                    attack.Param2 = EditorGUILayout.FloatField(
                        new GUIContent("扇形角度"),
                        attack.Param2);
                }
                attack.Knockback = EditorGUILayout.FloatField(
                    new GUIContent("击退强度"),
                    attack.Knockback);
                if (EditorGUI.EndChangeCheck())
                    _store.MarkDirty();
            }

            EditorGUILayout.EndVertical();
            return delete;
        }
        #endregion

        #region 技能辅助控件 (Ability Helpers)
        private int DrawComboIndexPopup(
            MonsterEditorProfile profile,
            int currentIndex)
        {
            var attacks = _store.GetAttacks(profile.Character.SubType).ToList();
            var values = new List<int> { -1 };
            var labels = new List<string> { "不产生直接伤害" };
            values.AddRange(attacks.Select(row => row.ComboIndex));
            labels.AddRange(attacks.Select(row =>
                $"攻击段 {row.ComboIndex} (伤害倍率 {row.DmgRate:0.##})"));

            int index = values.IndexOf(currentIndex);
            if (index < 0)
            {
                values.Add(currentIndex);
                labels.Add($"缺失的攻击段：{currentIndex}");
                index = values.Count - 1;
            }

            int selected = EditorGUILayout.Popup(
                new GUIContent("关联攻击段"),
                index,
                labels.ToArray());
            return values[selected];
        }

        private static int DrawIntOptionPopup(
            string label,
            int value,
            MonsterAIEditorOption[] options)
        {
            int index = -1;
            for (int i = 0; i < options.Length; i++)
            {
                if (int.TryParse(options[i].Value, out int parsed)
                    && parsed == value)
                {
                    index = i;
                    break;
                }
            }

            if (index < 0) index = 0;
            int selected = EditorGUILayout.Popup(
                label,
                index,
                options.Select(option => option.Label).ToArray());
            return int.TryParse(options[selected].Value, out int result) ? result : value;
        }
        #endregion
    }
}
