using UnityEngine;
using Emberwild.Core.AppLog;

namespace Emberwild.Resource
{
    /// <summary>
    /// 基于 Unity Resources API 的资源加载实现。
    /// 路径以 "Resources/" 根目录为起点,不含扩展名。
    /// 例:Resources/Prefabs/Characters/Samurai.prefab → path = "Prefabs/Characters/Samurai"
    /// </summary>
    public class ResourcesLoader : IResourceLoader
    {
        public T Load<T>(string path) where T : Object
        {
            var asset = Resources.Load<T>(path);
            if (asset == null)
            {
                AppLog.Warning("Resource", $"Resource not found: {path}");
            }
            return asset;
        }
    }
}