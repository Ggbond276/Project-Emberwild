namespace Emberwild.Core.Events
{
    /// <summary>
    /// 玩家血量重置事件 — 由 PlayerDataManager.ResetHealth() 发布。
    /// 供 PlayerFeedback 等模块重置内部状态(死亡标志等)。
    /// </summary>
    public readonly struct PlayerHealthResetEvent : IEvent
    {
    }
}
