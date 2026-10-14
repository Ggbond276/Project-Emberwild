using Emberwild.Character;
using Emberwild.Combat;
using Emberwild.Define;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Emberwild.Character
{
    /// <summary>
    /// 角色实体大管家 — 负责桥接 Define 与 Data，并向外部提供干净的数据访问接口。
    /// </summary>
    [RequireComponent(typeof(CombatAttribute))]
    public class CharacterEntity : MonoBehaviour
    {
        private CharacterDefine _define;

        [field: SerializeField]
        public CharacterData SaveData;

        public int TID => _define?.TID ?? 0;
        public string CharacterName => _define?.Name ?? "Unknown";
        public string SubType => _define?.SubType ?? "None";
        public float BaseMaxHp => _define?.HP ?? 0f;
        public float BaseAD => _define?.AD ?? 0f;
        public float BaseDEF => _define?.DEF ?? 0f;

        [Tooltip("依赖的战斗组件")]
        [SerializeField] private CombatAttribute _combatAttr;

        public void Initialize(CharacterDefine define, CharacterData saveData)
        {
            _define = define;
            SaveData = saveData;

            _combatAttr.Initialize(BaseMaxHp, saveData.CurrentHP, BaseAD, BaseDEF);
        }

        public void SyncDataBeforeSave()
        {
            if (SaveData == null || _combatAttr == null) return;

            SaveData.CurrentHP = _combatAttr.CurrentHp;
            SaveData.IsDead = _combatAttr.IsDead;
        }


    }
}
