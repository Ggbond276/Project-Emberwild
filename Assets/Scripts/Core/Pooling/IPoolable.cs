namespace Emberwild.Core.Pooling
{
    /// <summary>
    /// 由池对象上的组件实现，用于在取出和回收时清理自身运行时状态。
    /// 对象池本身不依赖具体业务类型，因此怪物、特效和投射物可以共用同一套接口。
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// 对象从池中被激活并完成位置设置后调用。
        /// </summary>
        void OnSpawnedFromPool();

        /// <summary>
        /// 对象即将被停用并放回池时调用。
        /// </summary>
        void OnReturnedToPool();
    }
}
