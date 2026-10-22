using System;
using Emberwild.AI.Enemy.Ability;

namespace Emberwild.Editor.MonsterAI
{
    public static class MonsterAIEditorOptions
    {
        #region 下拉选项 (Dropdown Options)
        public static readonly MonsterAIEditorOption[] Actions =
        {
            new MonsterAIEditorOption(BehaviorActions.Ability, "使用技能"),
            new MonsterAIEditorOption(BehaviorActions.Chase, "追击目标"),
            new MonsterAIEditorOption(BehaviorActions.Idle, "待机")
        };

        public static readonly MonsterAIEditorOption[] Conditions =
        {
            new MonsterAIEditorOption(BehaviorConditions.HasTarget, "有目标"),
            new MonsterAIEditorOption(BehaviorConditions.HpBelow, "血量低于"),
            new MonsterAIEditorOption(BehaviorConditions.AttackCooldownReady, "攻击冷却完成"),
            new MonsterAIEditorOption(BehaviorConditions.RecentlyHit, "受击硬直中"),
            new MonsterAIEditorOption(BehaviorConditions.IsDead, "已经死亡"),
            new MonsterAIEditorOption(BehaviorConditions.Always, "始终成立")
        };

        public static readonly MonsterAIEditorOption[] Executors =
        {
            new MonsterAIEditorOption(AbilityExecutorIds.Attack, "近战攻击"),
            new MonsterAIEditorOption(AbilityExecutorIds.RangedAttack, "远程攻击"),
            new MonsterAIEditorOption(AbilityExecutorIds.Dash, "纯位移"),
            new MonsterAIEditorOption(AbilityExecutorIds.DashAttack, "位移攻击"),
            new MonsterAIEditorOption(AbilityExecutorIds.Stance, "持续姿态"),
            new MonsterAIEditorOption(AbilityExecutorIds.PlayAndWait, "播放并等待"),
            new MonsterAIEditorOption(AbilityExecutorIds.Dead, "死亡")
        };

        public static readonly MonsterAIEditorOption[] Slots =
        {
            new MonsterAIEditorOption(string.Empty, "无"),
            new MonsterAIEditorOption(AbilitySlotIds.PrimaryAttack, "主攻击"),
            new MonsterAIEditorOption(AbilitySlotIds.SecondaryAttack, "副攻击"),
            new MonsterAIEditorOption(AbilitySlotIds.HeavyAttack, "重攻击"),
            new MonsterAIEditorOption(AbilitySlotIds.GapClose, "拉近距离"),
            new MonsterAIEditorOption(AbilitySlotIds.RangedAttack, "远程攻击"),
            new MonsterAIEditorOption(AbilitySlotIds.Mobility, "位移"),
            new MonsterAIEditorOption(AbilitySlotIds.Defense, "防御"),
            new MonsterAIEditorOption(AbilitySlotIds.Alert, "警戒"),
            new MonsterAIEditorOption(AbilitySlotIds.Special, "特殊能力"),
            new MonsterAIEditorOption(AbilitySlotIds.HitReaction, "受击反应"),
            new MonsterAIEditorOption(AbilitySlotIds.DeathReaction, "死亡反应")
        };

        public static readonly MonsterAIEditorOption[] AnimationSemanticOptions =
        {
            new MonsterAIEditorOption(string.Empty, "无"),
            new MonsterAIEditorOption(AnimationSemantics.Melee, "近战攻击"),
            new MonsterAIEditorOption(AnimationSemantics.Heavy, "重攻击"),
            new MonsterAIEditorOption(AnimationSemantics.AreaMelee, "范围近战攻击"),
            new MonsterAIEditorOption(AnimationSemantics.Pounce, "扑击"),
            new MonsterAIEditorOption(AnimationSemantics.Leap, "跃击"),
            new MonsterAIEditorOption(AnimationSemantics.Ranged, "远程攻击"),
            new MonsterAIEditorOption(AnimationSemantics.Spell, "施法"),
            new MonsterAIEditorOption(AnimationSemantics.Alert, "警戒"),
            new MonsterAIEditorOption(AnimationSemantics.TakeDamage, "受击"),
            new MonsterAIEditorOption(AnimationSemantics.Roll, "滚动"),
            new MonsterAIEditorOption(AnimationSemantics.Guard, "防御姿态")
        };

        public static readonly MonsterAIEditorOption[] BindingTypes =
        {
            new MonsterAIEditorOption(nameof(AnimationBindingType.Trigger), "触发一次 (Trigger)"),
            new MonsterAIEditorOption(nameof(AnimationBindingType.Bool), "开关状态 (Bool)"),
            new MonsterAIEditorOption(nameof(AnimationBindingType.Float), "数值 (Float)"),
            new MonsterAIEditorOption(nameof(AnimationBindingType.State), "动画状态 (State)")
        };

        public static readonly MonsterAIEditorOption[] HitboxTypes =
        {
            new MonsterAIEditorOption("1", "圆形"),
            new MonsterAIEditorOption("2", "矩形"),
            new MonsterAIEditorOption("3", "扇形")
        };
        #endregion

        #region 条件参数规则 (Condition Rules)
        /// <summary>判断条件是否使用数值参数。</summary>
        public static bool UsesConditionValue(string conditionId)
        {
            return conditionId == BehaviorConditions.HpBelow
                || conditionId == BehaviorConditions.AttackCooldownReady;
        }

        /// <summary>判断是否为编辑器未知条件。</summary>
        public static bool IsKnownCondition(string conditionId)
        {
            return IndexOf(Conditions, conditionId) >= 0;
        }
        #endregion

        #region 选项工具 (Option Utilities)
        /// <summary>查找选项值对应下标。</summary>
        public static int IndexOf(MonsterAIEditorOption[] options, string value)
        {
            if (options == null) return -1;
            for (int i = 0; i < options.Length; i++)
            {
                if (string.Equals(options[i].Value, value, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        /// <summary>获取选项的显示名称。</summary>
        public static string LabelOf(MonsterAIEditorOption[] options, string value)
        {
            int index = IndexOf(options, value);
            return index >= 0 ? options[index].Label : $"未知：{value}";
        }
        #endregion
    }
}
