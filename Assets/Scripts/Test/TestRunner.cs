using Emberwild.CameraControl;
using Emberwild.Character;
using Emberwild.Combat;
using Emberwild.Data;
using Emberwild.Define;
using Emberwild.Input;
using Emberwild.Manager;
using UnityEngine;
// 解决命名空间冲突:Emberwild.Input.InputReader 与 UnityEngine.Input 重名
using UnityInput = UnityEngine.Input;

namespace Emberwild.Core.Test
{
    public class TestRunner : MonoBehaviour
    {

        // 1. 新增：用于缓存生成的玩家的动画机
        private CharacterAnimator _characterAnimator;

        private void Start()
        {
            Debug.Log("<color=yellow>启动核心模块</color>");

            // 1. 静态数据管线
            DataManager.Instance.Init();
            DataManager.Instance.Load();

            // 2. 动态存档管线
            PlayerDataManager.Instance.Init();
            PlayerDataManager.Instance.LoadGame();

            // 3. 战斗判定管线
            CombatDetector.Instance.Init();


            Debug.Log("<color=yellow>构建玩家实体</color>");

            // 4. 提取当前存档，并根据存档中的 TID 去拿配置表
            CharacterData saveData = PlayerDataManager.Instance.CurrentSaveData;
            if (saveData == null)
            {
                Debug.LogError("初始化失败：无法获取本地存档数据！");
                return;
            }

            if (!DataManager.Instance.Characters.TryGetValue(saveData.TID, out CharacterDefine define))
            {
                Debug.LogError($"初始化失败：数据表中找不到 TID 为 {saveData.TID} 的角色配置！");
                return;
            }

            // 5. 动态加载并生成角色预制体
            GameObject prefab = Resources.Load<GameObject>(define.Resource);
            if (prefab == null)
            {
                Debug.LogError($"预制体加载失败！请检查 Resources/{define.Resource} 路径是否正确。");
                return;
            }

            GameObject playerObj = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            playerObj.name = define.Name;

            _characterAnimator = playerObj.GetComponent<CharacterAnimator>();

            CharacterEntity entity = playerObj.GetComponent<CharacterEntity>();
            if (entity != null)
            {
                entity.Initialize(define, saveData);
            }
            else
            {
                Debug.LogError($"配置错误：预制体 {playerObj.name} 上未挂载 CharacterEntity 组件！");
                return;
            }


            Debug.Log("<color=yellow>建立系统依赖关联</color>");
            BindDependencies(playerObj);
        }

        /// <summary>
        /// 串联摄像机、输入系统与动作系统
        /// </summary>
        private void BindDependencies(GameObject playerObj)
        {
            CameraController camController = FindObjectOfType<CameraController>();
            CharacterMovementController moveController = playerObj.GetComponent<CharacterMovementController>();
            InputReader playerInput = playerObj.GetComponent<InputReader>();

            if (camController != null && moveController != null && playerInput != null)
            {
                // A. 移动系统 -> 摄像机
                moveController.cameraController = camController;

                // B. 摄像机系统 -> 认识玩家坐标与手柄输入
                camController.BindTarget(playerObj.transform, playerInput);

                Debug.Log($"<color=green>角色 [{playerObj.name}] 实例化与依赖绑定 测试战斗</color>");
            }
            else
            {
                Debug.LogError("依赖绑定失败：未能找齐 CameraController、MovementController 或 InputReader！");
            }
        }

        private void Update()
        {
            if (UnityInput.GetKeyDown(KeyCode.Space))
            {
                if (_characterAnimator != null)
                {
                    _characterAnimator.PlayGetHit();
                    Debug.Log("按下空格，触发受击动画！");
                }
            }
        }

    }
}
