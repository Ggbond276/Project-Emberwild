using Emberwild.Core.Singleton;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Emberwild.Combat
{
    /// <summary>
    /// 全局伤害计算器 - 纯静态数学工具类
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>
        /// 计算最终伤害值
        /// </summary>
        /// <param name="attacker">攻击方的运行时面板</param>
        /// <param name="defender">受击方的运行时面板</param>
        /// <param name="skillDmgRate">招式伤害倍率（来自 AttackDefine）</param>
        /// <returns>经过护甲减免后的最终伤害</returns>
        public static float Calculate(CombatAttribute attacker, CombatAttribute defender, float skillDmgRate)
        {
            float baseDamage = attacker.CurrentAD * skillDmgRate;
            float finalDamage = baseDamage - defender.CurrentDEF;
            return Mathf.Max(1.0f, finalDamage);
        }
    }
}
