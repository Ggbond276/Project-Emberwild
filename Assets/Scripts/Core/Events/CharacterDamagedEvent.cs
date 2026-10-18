using UnityEngine;

namespace Emberwild.Core.Events
{
    /// <summary>
    /// 角色受到伤害时发布(HP 组件扣血后发布)。
    /// 订阅方:UI 系统(血条)、动画系统(触发 GetHit State)、状态机、音效系统。
    /// </summary>
    public readonly struct CharacterDamagedEvent : IEvent
    {
        /// <summary>
        /// 实际扣血量。
        /// </summary>
        public readonly float Damage;
        /// <summary>
        /// 受到伤害后剩余 HP。
        /// </summary>
        public readonly float CurrentHp;
        /// <summary>
        /// 受击方向(用于 GetHit 动画方向选择)。
        /// </summary>
        public readonly Vector3 FromDirection;

        /// <summary>
        /// 攻击命中点，特效生成位置
        /// </summary>
        public readonly Vector3 HitPoint;

        public CharacterDamagedEvent(float damage, float currentHp, Vector3 fromDirection, Vector3 hitPoint)
        {
            Damage = damage;
            CurrentHp = currentHp;
            FromDirection = fromDirection;
            HitPoint = hitPoint;
        }
    }
}
