namespace Emberwild.Core.Events
{
    /// <summary>
    /// 事件标记接口。所有通过 EventBus 传递的事件 struct 必须实现此接口。
    /// 约束为 struct 以保证零分配。
    /// </summary>
    public interface IEvent
    {
    }
}
