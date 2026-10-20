using Emberwild.Combat;
using Emberwild.Core.AppLog;
using Emberwild.Define;
using Emberwild.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Emberwild.AI.Enemy
{
    /// <summary>
    /// 怪物实体 - 桥接CharacterDefine 数据表与 CombatAttribute
    /// 
    /// 职责：
    /// - 按TID从DataManager读取配置
    /// - 把 HP/AD/DEF 等战斗信息注入CombatAttribute
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatAttribute))]
    public class EnemyEntity : MonoBehaviour, ICombatEntity
    {
        [Header("数据")]
        [Tooltip("对应 Data/Data/CharacterDefine.txt 里的 TID")]
        [SerializeField] private int _tid = 2001;

        private CombatAttribute _combatAttr;


        private CharacterDefine _define;
        private bool _runtimeInitialized;

        public int TID => _define != null ? _define.TID : _tid;
        public string EnemyName => _define != null ? _define.Name : gameObject.name;
        public string SubType => _define != null ? _define.SubType : "None";
        public float MaxHp => _define != null ? _define.HP : 0f;
        public float BaseAD => _define != null ? _define.AD : 0f;
        public float BaseDEF => _define != null ? _define.DEF : 0f;
        public CombatAttribute Attribute => _combatAttr;



        /// <summary>
        /// 缓存当前 GameObject 上的战斗属性组件。
        /// </summary>
        private void Awake()
        {
            if (_combatAttr == null)
            {
                _combatAttr = GetComponent<CombatAttribute>();
            }
        }

        /// <summary>
        /// 按 TID 重新加载实体定义并恢复满血状态。
        /// 池对象每次出生都必须调用，不能依赖只执行一次的 Start。
        /// </summary>
        /// <param name="tid">要加载的 CharacterDefine 怪物 TID。</param>
        /// <returns>数据加载并初始化成功后返回 true。</returns>
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

        /// <summary>
        /// 处理未经过刷怪器、直接放在场景中的怪物初始化。
        /// </summary>
        private void Start()
        {
            if (_runtimeInitialized) return;
            if (!PrepareForSpawn(_tid))
                enabled = false;
        }

        /// <summary>
        /// 从 DataManager 读取当前 TID 对应的角色配置。
        /// </summary>
        private void LoadDefine()
        {
            var dm = DataManager.Instance;
            if (dm != null && dm.Characters != null && dm.Characters.TryGetValue(_tid, out var define))
            {
                _define = define;
                AppLog.Info("Enemy", $"{name} 读取数据表成功: TID={define.TID} HP={define.HP} SubType={define.SubType}");
            }
            else
            {
                AppLog.Error("Enemy", $"{name}: CharacterDefine TID={_tid} was not found.");
            }
        }


        public FactionType Faction => FactionType.Monster;
        public Transform Transform => transform;

    }
}
