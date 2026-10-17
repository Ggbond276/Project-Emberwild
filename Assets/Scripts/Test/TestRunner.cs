using Emberwild.AI.Pathfinding;
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

        [Header("寻路系统配置")]
        [Tooltip("寻路网格的生成起点 (通常为地形左下角)")]
        [SerializeField] private Vector3 _gridOrigin = Vector3.zero;
        [Tooltip("寻路网格的覆盖尺寸 (X和Z宽跨度)")]
        [SerializeField] private Vector2 _gridWorldSize = new Vector2(20, 20);
        [Tooltip("单个寻路节点的检测半径")]
        [SerializeField] private float _nodeRadius = 0.5f;
        [Tooltip("障碍物图层，用于标记不可行走的区域")]
        [SerializeField] private LayerMask _obstacleMask;

        // 缓存生成的玩家对象及其组件
        private GameObject _playerObj;
        private CharacterAnimator _characterAnimator;

        private void Start()
        {
            Debug.Log("<color=yellow>--- 游戏启动流水线 开始 ---</color>");

            //1. 初始化所有无状态的全局管理器 (数据表/存档/战斗)
            InitGlobalManagers();

            // 2. 初始化底层环境数据 (如 A* 寻路网格)
            InitEnvironmentSystems();

            // 3. 根据存档数据动态实例化玩家模型
            CreatePlayerEntity();

            // 4. 将玩家与外围系统(相机/输入)进行绑定
            BindDependencies();

            Debug.Log("<color=yellow>--- 游戏启动流水线 完毕 ---</color>");
        }

        /// <summary>
        /// 模块 1：初始化全局数据与系统管线
        /// </summary>
        private void InitGlobalManagers()
        {
            Debug.Log("<color=yellow>[1/4] 初始化核心管理器...</color>");

            DataManager.Instance.Init();
            DataManager.Instance.Load();

            PlayerDataManager.Instance.Init();
            PlayerDataManager.Instance.LoadGame();

            CombatDetector.Instance.Init();
        }

        /// <summary>
        /// 模块 2：初始化环境系统（包括全局寻路网格）
        /// </summary>
        private void InitEnvironmentSystems()
        {
            Debug.Log("<color=yellow>[2/4] 初始化环境系统(寻路网格)...</color>");

            // 调度我们刚刚重构剥离 MonoBehaviour 后的 GridManager
            GridManager.Instance.Init(_gridOrigin, _gridWorldSize, _nodeRadius, _obstacleMask);
        }

        /// <summary>
        /// 模块 3：读取存档并动态构建玩家实体
        /// </summary>
        private void CreatePlayerEntity()
        {
            Debug.Log("<color=yellow>[3/4] 构建玩家实体...</color>");

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

            GameObject prefab = Resources.Load<GameObject>(define.Resource);
            if (prefab == null)
            {
                Debug.LogError($"预制体加载失败！请检查 Resources/{define.Resource} 路径是否正确。");
                return;
            }

            Vector3 location = new Vector3(18, 1, 16);
            _playerObj = Instantiate(prefab, location, Quaternion.identity);
            _playerObj.name = define.Name;

            _characterAnimator = _playerObj.GetComponent<CharacterAnimator>();

            CharacterEntity entity = _playerObj.GetComponent<CharacterEntity>();
            if (entity != null)
            {
                entity.Initialize(define, saveData);
            }
            else
            {
                Debug.LogError($"配置错误：预制体 {_playerObj.name} 上未挂载 CharacterEntity 组件！");
            }
        }

        /// <summary>
        /// 模块 4：串联摄像机、输入系统与动作系统
        /// </summary>
        private void BindDependencies()
        {
            Debug.Log("<color=yellow>[4/4] 建立系统依赖关联...</color>");

            if (_playerObj == null) return;

            CameraController camController = FindObjectOfType<CameraController>();
            CharacterMovementController moveController = _playerObj.GetComponent<CharacterMovementController>();
            InputReader playerInput = _playerObj.GetComponent<InputReader>();

            if (camController != null && moveController != null && playerInput != null)
            {
                moveController.cameraController = camController;
                camController.BindTarget(_playerObj.transform, playerInput);

                Debug.Log($"<color=green>角色 [{_playerObj.name}] 实例化与依赖绑定完成！测试阶段就绪。</color>");
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
