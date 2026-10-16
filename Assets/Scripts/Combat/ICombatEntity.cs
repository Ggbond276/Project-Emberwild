using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Emberwild.Combat
{
    /// <summary>
    /// 战斗实体统一契约 玩家CharacterEntity
    /// </summary>
    public interface ICombatEntity
    {
        /// <summary>
        /// 类型标识
        /// </summary>
       string SubType { get; }

        /// <summary>
        /// 阵营标识
        /// </summary>
        FactionType Faction { get; }

        /// <summary>
        /// 世界坐标变换
        /// </summary>
        Transform Transform { get; }

        /// <summary>
        /// 攻击者属性面板
        /// </summary>
        CombatAttribute Attribute { get; }
    }
}
