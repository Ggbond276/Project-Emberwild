using Emberwild.Character;
using UnityEngine;

namespace Emberwild.Core.Events
{
    /// <summary>
    /// 角色攻击命中事件 — 伤害判定系统接收。
    /// 订阅方:伤害判定、特效、音效。
    /// </summary>
    public readonly struct CharacterAttackHitEvent : IEvent
    {
        public readonly CharacterEntity Attacker;

        public readonly int ComboIndex;

        /// <summary>
        /// 玩家攻击事件
        /// </summary>
        /// <param name="attacker">攻击者是谁</param>
        /// <param name="comboIndex">是第几段攻击</param>
       public CharacterAttackHitEvent(CharacterEntity attacker, int comboIndex)
        {
            Attacker = attacker;
            ComboIndex = comboIndex;
        }
    }
}
