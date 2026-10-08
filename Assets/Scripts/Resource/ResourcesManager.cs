using UnityEngine;
using Emberwild.Core.AppLog;
using Emberwild.Core.Singleton;

namespace Emberwild.Resource
{
    /// <summary>
    /// 项目统一资源加载入口。
    /// 业务层不直接调用 Resources / AssetBundle / Addressables,
    /// 而是通过本类访问 IResourceLoader。
    /// 切换底层方案(Resources / AssetBundle / Addressables)不影响业务代码。
    /// </summary>
    public class ResourcesManager : Singleton<ResourcesManager>
    {
        private IResourceLoader _loader;

        /// <summary>
        /// 注入当前资源加载实现。
        /// 阶段内由初始化代码调用一次,后续阶段可热替换。
        /// </summary>
        public void SetLoader(IResourceLoader loader)
        {
            _loader = loader;
        }

        /// <summary>
        /// 同步加载资源。
        /// </summary>
        public T Load<T>(string path) where T : Object
        {
            if (_loader == null)
            {
                AppLog.Error("Resource", "No loader registered. Call SetLoader first.");
                return null;
            }
            return _loader.Load<T>(path);
        }
    }
}