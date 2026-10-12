namespace Emberwild.Core.Events
{
    /// <summary>
    /// 角色血量重置事件 — 由 CharacterDataManager.ResetHealth() 发布。
    /// 供 CharacterFeedback 等模块重置内部状态(死亡标志等)。
    /// </summary>
    public readonly struct CharacterHealthResetEvent : IEvent
    {
    }
}
