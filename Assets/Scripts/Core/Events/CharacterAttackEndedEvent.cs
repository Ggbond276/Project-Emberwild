namespace Emberwild.Core.Events
{
    /// <summary>
    /// 角色攻击硬直结束事件 — 在 <c>CharacterCombat</c> 中 _attackEndTime 到期时发布。
    /// </summary>
    public readonly struct CharacterAttackEndedEvent : IEvent
    {
        /// <summary>攻击段位(0~3)。</summary>
        public readonly int ComboIndex;

        public CharacterAttackEndedEvent(int comboIndex)
        {
            ComboIndex = comboIndex;
        }
    }
}
