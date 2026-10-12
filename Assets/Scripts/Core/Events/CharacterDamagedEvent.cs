namespace Emberwild.Core.Events
{
    /// <summary>
    /// 角色受到伤害时发布(HP 组件扣血后发布)。
    /// 订阅方:UI 系统(血条)、动画系统(触发 GetHit State)、状态机、音效系统。
    /// </summary>
    public readonly struct CharacterDamagedEvent : IEvent
    {
        /// <summary>实际扣血量。</summary>
        public readonly float Damage;
        /// <summary>受到伤害后剩余 HP。</summary>
        public readonly float CurrentHp;
        /// <summary>受击方向(用于 GetHit 动画方向选择)。</summary>
        public readonly UnityEngine.Vector3 FromDirection;

        public CharacterDamagedEvent(float damage, float currentHp, UnityEngine.Vector3 fromDirection)
        {
            Damage = damage;
            CurrentHp = currentHp;
            FromDirection = fromDirection;
        }
    }
}
