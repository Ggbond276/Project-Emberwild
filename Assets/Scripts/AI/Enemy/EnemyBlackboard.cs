using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Emberwild.AI.Enemy
{
    /// <summary>
    /// 怪物黑板 —— 状态之间共享的"内存"。
    ///
    /// 职责:
    /// - 存放所有状态都要读的数据(目标/距离/感知结果/血量比例)。
    /// - 由感知层写,由状态层读。
    ///
    /// 严格不做:
    /// - 不持有任何组件引用(它只是一个数据袋子)。
    /// - 不写任何逻辑(没有方法)。
    /// </summary>
    public class EnemyBlackboard
    {
        /// <summary>
        /// 当前锁定的目标
        /// </summary>
        public Transform Target;

        /// <summary>
        /// 到目标的距离
        /// </summary>
        public float DistanceToTarget;

        /// <summary>
        /// 感知结果：当前是否看得到目标
        /// </summary>
        public bool CanSeeTarget;

        /// <summary>
        /// 血量比例 0 - 1 (用于判断缩壳状态)
        /// </summary>
        public float HpRatio = 1f;

        /// <summary>
        /// 上次攻击时间
        /// </summary>
        public float LastAttackTime;

        /// <summary>
        /// 上次滚动冲刺时间 (计算冷却)
        /// </summary>
        public float LastRollTime;

        /// <summary>
        /// 上次受击时间戳
        /// </summary>
        public float LastHitTime = -999f;
    }
}
