using Emberwild.Combat;
using Emberwild.Core.AppLog;
using Emberwild.Define;
using Emberwild.Manager;
using UnityEngine;

namespace Emberwild.AI.Enemy
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatAttribute))]
    public class EnemyEntity : MonoBehaviour, ICombatEntity
    {
        #region 本地核心组件 (Local Components)
        private CombatAttribute _combatAttr;
        #endregion

        #region 实体配置与状态 (Entity Configuration & State)
        // 当前怪物身份与初始化状态
        [Header("数据")]
        [Tooltip("对应 Data/Data/CharacterDefine.txt 里的 TID")]
        [SerializeField] private int _tid = 2001;

        // 根据Tid初始化出来的数据
        private CharacterDefine _define;
        private bool _runtimeInitialized;
        #endregion

        #region 公共属性与接口 (Public API)
        public int TID => _define != null ? _define.TID : _tid;
        public string EnemyName => _define != null ? _define.Name : gameObject.name;
        public string SubType => _define != null ? _define.SubType : "None";
        public float MaxHp => _define != null ? _define.HP : 0f;
        public float BaseAD => _define != null ? _define.AD : 0f;
        public float BaseDEF => _define != null ? _define.DEF : 0f;
        public CombatAttribute Attribute => _combatAttr;
        public FactionType Faction => FactionType.Monster;
        public Transform Transform => transform;
        #endregion

        #region 实体初始化 (Entity Initialization)
        /// <summary>获取怪物身上的 CombatAttribute。</summary>
        private void Awake()
        {
            if (_combatAttr == null)
            {
                _combatAttr = GetComponent<CombatAttribute>();
            }
        }

        /// <summary>按 TID 加载怪物定义并恢复满血状态。</summary>
        public bool PrepareForSpawn(int tid)
        {
            _tid = tid;
            _define = null;
            enabled = true;

            if (_combatAttr == null)
                _combatAttr = GetComponent<CombatAttribute>();

            if (_combatAttr == null)
            {
                AppLog.Error("Enemy", $"{name}: CombatAttribute is missing.");
                return false;
            }

            LoadDefine();
            if (_define == null)
            {
                AppLog.Error("Enemy", $"{name}: CharacterDefine TID={_tid} is missing.");
                return false;
            }

            _combatAttr.enabled = true;
            _combatAttr.Initialize(MaxHp, MaxHp, BaseAD, BaseDEF);
            _runtimeInitialized = true;
            return true;
        }

        /// <summary>为直接放在场景中的怪物执行默认初始化。</summary>
        private void Start()
        {
            if (_runtimeInitialized) return;
            if (!PrepareForSpawn(_tid))
                enabled = false;
        }

        /// <summary>从 DataManager 读取当前 TID 的角色配置。</summary>
        private void LoadDefine()
        {
            var dm = DataManager.Instance;
            if (dm != null && dm.Characters != null && dm.Characters.TryGetValue(_tid, out var define))
            {
                _define = define;
                AppLog.Info("Enemy",
                    $"{name} 读取数据表成功: TID={define.TID} HP={define.HP} SubType={define.SubType}");
            }
            else
            {
                AppLog.Error("Enemy", $"{name}: CharacterDefine TID={_tid} was not found.");
            }
        }
        #endregion
    }
}
