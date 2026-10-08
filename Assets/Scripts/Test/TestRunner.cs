using UnityEngine;
using Emberwild.Core.AppLog;
using Emberwild.Resource;

namespace Emberwild.Test
{
    /// <summary>
    /// 项目集中测试入口。
    /// 所有 Stage 的运行时验证都在此脚本中,按 Stage 顺序排列。
    /// 使用方法:挂载到 SampleScene 任意 GameObject,运行场景后查看 Console 输出。
    /// </summary>
    public class TestRunner : MonoBehaviour
    {
        private void Start()
        {
            TestStage2_AppLog();
            TestStage3_ResourceManager();
            TestStage3_ResourcesLoader();
        }

        private static void TestStage2_AppLog()
        {
            AppLog.Info("Input", "Attack Input Received");
            AppLog.Warning("Resource", "Prefab not found");
            AppLog.Error("Combat", "Attack initialization failed");
        }

        private static void TestStage3_ResourceManager()
        {
            // 1. Instance 可访问(Singleton 验证)
            var mgr = ResourcesManager.Instance;
            if (mgr == null)
            {
                AppLog.Error("Resource", "ResourcesManager.Instance is null");
                return;
            }
            AppLog.Info("Resource", "Instance accessible");

            // 2. 未注入 loader 时 Load:应返回 null 并输出 Error
            var pre = mgr.Load<GameObject>("Prefabs/Characters/Samurai");
            AppLog.Info("Resource", $"Pre-injection Load result: {(pre == null ? "null" : "value")}");

            // 3. 注入真实 ResourcesLoader
            mgr.SetLoader(new ResourcesLoader());
            AppLog.Info("Resource", "ResourcesLoader injected");
        }

        private static void TestStage3_ResourcesLoader()
        {
            // Resources 目录下当前没有测试 Prefab,因此只能验证"找不到"分支。
            // 期望:返回 null + Warning(Resource not found: ...)
            // 若日后在 Resources/Prefabs/Characters/Samurai.prefab 放置真实 Prefab,
            // 该路径 Load 将返回非 null 且不再有 Warning。
            var mgr = ResourcesManager.Instance;
            var asset = mgr.Load<GameObject>("Prefabs/Characters/Samurai");
            AppLog.Info("Resource", $"ResourcesLoader.Load result: {(asset == null ? "null" : asset.name)}");
        }
    }
}