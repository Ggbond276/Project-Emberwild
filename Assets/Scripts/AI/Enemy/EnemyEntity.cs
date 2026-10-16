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

        [Header("兜底数值(数据表读不到时使用)")]
        [SerializeField] private float _fallbackHp = 100f;
        [SerializeField] private float _fallbackAD = 10f;
        [SerializeField] private float _fallbackDEF = 10f;

        [Header("依赖组件")]
        [SerializeField] private CombatAttribute _combatAttr;


        private CharacterDefine _define;

        public int TID => _define != null ? _define.TID : _tid;
        public string EnemyName => _define != null ? _define.Name : gameObject.name;
        public string SubType => _define != null ? _define.SubType : "None";
        public float MaxHp => _define != null ? _define.HP : _fallbackHp;
        public float BaseAD => _define != null ? _define.AD : _fallbackAD;
        public float BaseDEF => _define != null ? _define.DEF : _fallbackDEF;
        public CombatAttribute Attribute => _combatAttr;



        private void Awake()
        {
            if (_combatAttr == null)
            {
                _combatAttr = GetComponent<CombatAttribute>();
            }
        }


        private void Start()
        {
            LoadDefine();
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
                AppLog.Warning("Enemy", $"{name} 数据表里找不到 TID={_tid}, 使用兜底数值");
            }
        }


        public FactionType Faction => FactionType.Monster;
        public Transform Transform => transform;

    }
}
