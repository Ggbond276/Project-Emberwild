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

        public int TID => _define != null ? _define.TID : _tid;
        public string EnemyName => _define != null ? _define.Name : gameObject.name;
        public string SubType => _define != null ? _define.SubType : "None";
        public float MaxHp => _define != null ? _define.HP : 0f;
        public float BaseAD => _define != null ? _define.AD : 0f;
        public float BaseDEF => _define != null ? _define.DEF : 0f;
        public CombatAttribute Attribute => _combatAttr;



        private void Awake()
        {
            if (_combatAttr == null)
            {
                _combatAttr = GetComponent<CombatAttribute>();
            }
        }

        /// <summary>
        /// 覆盖 Prefab 上的默认 TID，供 Spawner 在 Start 前按配置表选怪。
        /// </summary>
        /// <param name="tid">CharacterDefine 中的怪物 TID。</param>
        public void SetTid(int tid)
        {
            _tid = tid;
            _define = null;
        }


        private void Start()
        {
            LoadDefine();
            if (_define == null)
            {
                AppLog.Error("Enemy", $"{name}: CharacterDefine TID={_tid} is missing.");
                enabled = false;
                return;
            }
            _combatAttr.Initialize(MaxHp, MaxHp, BaseAD, BaseDEF);
        }

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
