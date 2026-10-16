using Emberwild.Character;
using Emberwild.Combat;
using UnityEngine;

namespace Emberwild.Core.Events
{
    /// <summary>
    /// 角色攻击命中事件 — 伤害判定系统接收。
    /// 订阅方:伤害判定、特效、音效。
    /// </summary>
    public readonly struct CharacterAttackHitEvent : IEvent
    {
        /// <summary>
        /// 攻击者（玩家或者怪物，统一走ICombatEntity）
        /// </summary>
        public readonly ICombatEntity Attacker;

        /// <summary>
        /// 攻击段位（0 - 3 怪物目前只用0）
        /// </summary>
        public readonly int ComboIndex;

        /// <summary>
        /// 玩家攻击事件
        /// </summary>
        /// <param name="attacker">攻击者是谁</param>
        /// <param name="comboIndex">是第几段攻击</param>
       public CharacterAttackHitEvent(ICombatEntity attacker, int comboIndex)
        {
            Attacker = attacker;
            ComboIndex = comboIndex;
        }
    }
}
