using Emberwild.AI.Enemy.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        [InspectorName("恒真(兜底)")] Always,               // 恒真(兜底)
        [InspectorName("已死亡")] IsDead,                    // 已死亡
        [InspectorName("刚受击(硬直内)")] RecentlyHit,       // 受击硬直窗口(读 config.hitstunDuration,无参数)
        [InspectorName("血量低于阈值")] HpBelow,             // p1=血量比例阈值
        [InspectorName("距离在区间内")] InRange,             // p1=最小距离, p2=最大距离
        [InspectorName("攻击冷却就绪")] CooldownAttackReady, // p1=攻击冷却时长
        [InspectorName("滚动冷却就绪")] CooldownRollReady,   // p1=滚动冷却时长
        [InspectorName("能看到目标")] HasTarget,             // 能看到目标
        [InspectorName("尚未缩壳过")] NotHidden,             // 未缩壳过
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
}
