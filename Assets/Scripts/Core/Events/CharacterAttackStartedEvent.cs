namespace Emberwild.Core.Events
{
    /// <summary>
    /// 角色攻击开始事件 — 在 <c>CharacterCombat.HandleAttack</c> 输入确认后立刻发布。
    /// 用于通知角色状态机锁定移动。
    /// </summary>
    public readonly struct CharacterAttackStartedEvent : IEvent
    {
        /// <summary>攻击段位(0~3)。</summary>
        public readonly int ComboIndex;
        /// <summary>动画时长(秒)。</summary>
        public readonly float Duration;
        /// <summary>移动锁时长(秒,通常 == 动画硬直时长)。</summary>
        public readonly float LockDuration;

        public CharacterAttackStartedEvent(int comboIndex, float duration, float lockDuration)
        {
            ComboIndex = comboIndex;
            Duration = duration;
            LockDuration = lockDuration;
        }
    }
}
