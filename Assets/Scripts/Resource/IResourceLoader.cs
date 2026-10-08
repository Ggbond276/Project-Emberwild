using UnityEngine;

namespace Emberwild.Resource
{
    /// <summary>
    /// 资源加载统一接口。
    /// 业务层不直接调用 Resources / AssetBundle / Addressables,
    /// 而是通过 ResourcesManager 间接访问本接口。
    /// 切换底层方案(Resources / AssetBundle / Addressables)不影响业务代码。
    /// 当前阶段只实现同步 Load,异步加载在后续阶段按需扩展。
    /// </summary>
    public interface IResourceLoader
    {
        T Load<T>(string path) where T : Object;
    }
}