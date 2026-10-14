using Emberwild.Core.AppLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Emberwild.Combat
{
    /// <summary>
    /// 每个参与战斗的角色身上都会挂载
    /// </summary>
    [DisallowMultipleComponent]
    public class CombatAttribute : MonoBehaviour
    {
        [field: SerializeField] public float MaxHp { get; private set; }
        [field: SerializeField] public float CurrentHp { get; private set; }
        [field: SerializeField] public float CurrentAD { get; private set; }
        [field: SerializeField] public float CurrentDEF { get; private set; }

        public bool IsDead { get; private set; }

        public void Initialize(float maxHp, float currentHp, float baseAD, float baseDEF)
        {
            MaxHp = maxHp;
            CurrentHp = currentHp;
            CurrentAD = baseAD;   
            CurrentDEF = baseDEF;
            IsDead = currentHp <= 0;
        }

        public void TakeDamage(float damageAmount)
        {
            if (IsDead) return;

            CurrentHp = Mathf.Max(0, CurrentHp - damageAmount);
            AppLog.Info("Combat", $"{gameObject.name} 受到 {damageAmount:F1} 伤害, 剩余 HP: {CurrentHp:F1}");

            if (CurrentHp <= 0)
            {
                IsDead = true;
                AppLog.Info("Combat", $"{gameObject.name} 死亡");
            }
        }
    }
}
