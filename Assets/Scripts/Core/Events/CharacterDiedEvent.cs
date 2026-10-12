namespace Emberwild.Core.Events
{
    /// <summary>
    /// 角色 HP 归零时发布(仅一次)。
    /// 订阅方:游戏状态机(切到死亡流程)、UI 系统、关卡系统。
    /// </summary>
    public readonly struct CharacterDiedEvent : IEvent
    {
    }
}
