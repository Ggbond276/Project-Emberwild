using UnityEngine;

namespace Emberwild.Core.Events
{
    /// <summary>
    /// 角色攻击命中事件 — 伤害判定系统接收。
    /// 订阅方:伤害判定、特效、音效。
    /// </summary>
    public readonly struct CharacterAttackHitEvent : IEvent
    {
        /// <summary>攻击段位(0~3)。</summary>
        public readonly int ComboIndex;
        /// <summary>命中框中心(世界坐标)。</summary>
        public readonly Vector3 HitPoint;
        /// <summary>命中框朝向(单位向量)。</summary>
        public readonly Vector3 HitDirection;
        /// <summary>伤害值。</summary>
        public readonly float Damage;

        public CharacterAttackHitEvent(int comboIndex, Vector3 hitPoint, Vector3 hitDirection, float damage)
        {
            ComboIndex = comboIndex;
            HitPoint = hitPoint;
            HitDirection = hitDirection;
            Damage = damage;
        }
    }
}
