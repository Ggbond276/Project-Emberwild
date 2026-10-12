using Emberwild.CameraControl;
using Emberwild.Character;
using Emberwild.Data;
using Emberwild.Define;
using Emberwild.Input;
using Emberwild.Manager;
using UnityEngine;
// 解决命名空间冲突:Emberwild.Input.InputReader 与 UnityEngine.Input 重名
using UnityInput = UnityEngine.Input;

namespace Emberwild.Core.Test
{
    /// <summary>
    /// 运行时手动测试入口 — 触发伤害/重置,验证 PlayerDataManager + 事件流。
    /// 通过 Inspector 启用后,使用以下键盘按键:
    /// - T: 受伤 10
    /// - Y: 受伤 30
    /// - U: 受伤 999 (致命)
    /// - R: 重置血量
    /// </summary>
    public class TestRunner : MonoBehaviour
    {
        private void Start()
        {
            // 1. 全局数据初始化（加载配置表）
            DataManager.Instance.Load();

            // 2. 模拟获取玩家配置数据
            int playerTID = 19;
            if (!DataManager.Instance.Character.ContainsKey(playerTID))
            {
                Debug.LogError($"找不到 TID 为 {playerTID} 的角色配置！");
                return;
            }
            CharacterDefine playerDefine = DataManager.Instance.Character[playerTID];

            // 3. 动态加载预制体
            GameObject prefab = Resources.Load<GameObject>(playerDefine.Resource);
            if (prefab == null)
            {
                Debug.LogError($"预制体加载失败，路径: {playerDefine.Resource}");
                return;
            }

            // 4. 实例化预制体到场景中
            GameObject playerObj = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            playerObj.name = playerDefine.Name;

            // 5. 注入角色核心数据
            CharacterEntity entity = playerObj.GetComponent<CharacterEntity>();
            if (entity != null)
            {
                entity.Initialize(playerDefine);
            }

            // ==========================================
            // 6. 核心新增：动态绑定相机与控制器
            // ==========================================

            // 第一步：在当前场景中寻找 CameraController
            // 假设它挂载在 Main Camera 上
            CameraController camController = FindObjectOfType<CameraController>();

            // 获取角色身上的控制器和输入源
            CharacterMovementController moveController = playerObj.GetComponent<CharacterMovementController>();
            InputReader playerInput = playerObj.GetComponent<InputReader>();

            if (camController != null && moveController != null && playerInput != null)
            {
                // 1. 角色 -> 认识相机（用于计算移动方向）
                moveController.cameraController = camController;
                // 注意：如果这行报错，请把 CharacterMovementController 里的 cameraController 变量改为 public。

                // 2. 相机 -> 认识角色 Transform 和 输入源 (执行我们刚写的绑定方法)
                camController.BindTarget(playerObj.transform, playerInput);

                Debug.Log("<color=green>角色与摄像机的所有依赖绑定完成！</color>");
            }
            else
            {
                Debug.LogError("绑定失败：未能找齐 CameraController、MovementController 或 InputReader！");
            }
        }

    }
}
