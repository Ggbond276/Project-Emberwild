using Emberwild.AI.Enemy.State;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Emberwild.AI.BT
{
    public enum BTNodeType
    {
        [InspectorName("选择器 Selector")] Selector,
        [InspectorName("顺序 Sequence")] Sequence,
        [InspectorName("条件 Condition")] Condition,
        [InspectorName("动作 Action")] Action
    }

    public enum ConditionType
    {
        [InspectorName("恒真(兜底)")] Always,
        [InspectorName("已死亡")] IsDead,
        [InspectorName("刚受击(硬直内)")] RecentlyHit,
        [InspectorName("血量低于阈值")] HpBelow,
        [InspectorName("距离在区间内")] InRange,
        [InspectorName("攻击冷却就绪")] CooldownAttackReady,
        [InspectorName("滚动冷却就绪")] CooldownRollReady,
        [InspectorName("能看到目标")] HasTarget,
        [InspectorName("尚未缩壳过")] NotHidden,

        [InspectorName("重击冷却就绪")] CooldownHeavyReady,
        [InspectorName("扑击冷却就绪")] CooldownPounceReady,
        [InspectorName("跃击冷却就绪")] CooldownLeapReady,
        [InspectorName("远程冷却就绪")] CooldownRangedReady,
        [InspectorName("施法冷却就绪")] CooldownSpellReady,
        [InspectorName("尚未警戒过")] NotAlerted,
    }

    [Serializable]
    public class BTNodeDef
    {
        public BTNodeType type;
        public ConditionType condition;
        public float p1;
        public float p2;
        public EnemyStateType action;
        public List<BTNodeDef> children = new List<BTNodeDef>();
    }

    /// <summary>
    /// 条件参数元数据：每个条件需要几个参数、每个参数的显示名。
    /// Inspector 绘制器与 ConditionFactory 都以这里为准，
    /// 新增条件只改这一处，不会出现"工厂认了但界面不画"的漏配。
    /// </summary>
    public static class ConditionMeta
    {
        /// <summary>该条件用到的参数个数（0 / 1 / 2）。</summary>
        public static int ParamCount(ConditionType cond)
        {
            if (IsCooldown(cond)) return 1;

            switch (cond)
            {
                case ConditionType.HpBelow:
                    return 1;

                case ConditionType.InRange:
                    return 2;

                default:
                    return 0;
            }
        }

        /// <summary>第 index 个参数的显示标签（index 从 0 开始）。</summary>
        public static string ParamLabel(ConditionType cond, int index)
        {
            if (IsCooldown(cond)) return "冷却时长（秒）";

            if (cond == ConditionType.InRange)
                return index == 0 ? "最小距离（米）" : "最大距离（米）";

            if (cond == ConditionType.HpBelow)
                return "血量阈值（0~1）";

            return "参数" + (index + 1);
        }

        /// <summary>是否为「XX 冷却就绪」这一类条件，它们都只用一个冷却时长参数。</summary>
        public static bool IsCooldown(ConditionType cond)
        {
            switch (cond)
            {
                case ConditionType.CooldownAttackReady:
                case ConditionType.CooldownRollReady:
                case ConditionType.CooldownHeavyReady:
                case ConditionType.CooldownPounceReady:
                case ConditionType.CooldownLeapReady:
                case ConditionType.CooldownRangedReady:
                case ConditionType.CooldownSpellReady:
                    return true;

                default:
                    return false;
            }
        }
    }
}
