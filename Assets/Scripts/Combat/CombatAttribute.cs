using Emberwild.Core.AppLog;
using Emberwild.Core.Events;
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

        /// <summary>
        /// 缩壳减伤标志
        /// </summary>
        public bool IsHidden { get; set; }

        private ICombatEntity _entity;

        private void Awake()
        {
            _entity = GetComponent<ICombatEntity>();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="maxHp"></param>
        /// <param name="currentHp"></param>
        /// <param name="baseAD"></param>
        /// <param name="baseDEF"></param>
        public void Initialize(float maxHp, float currentHp, float baseAD, float baseDEF)
        {
            MaxHp = maxHp;
            CurrentHp = currentHp;
            CurrentAD = baseAD;   
            CurrentDEF = baseDEF;
            IsDead = currentHp <= 0;
        }

        /// <summary>
        /// 受到伤害
        /// </summary>
        /// <param name="damageAmount">受到的伤害是多少</param>
        /// <param name="hitPoint">攻击发起点</param>
        /// <param name="fromDirection">伤害来自哪个方向</param>
        public void TakeDamage(float damageAmount, Vector3 hitPoint, Vector3 fromDirection)
        {
            if (IsDead) return;

            if (IsHidden) damageAmount *= 0.5f; // 缩壳减伤0.5f

            CurrentHp = Mathf.Max(0, CurrentHp - damageAmount);
            AppLog.Info("Combat", $"{gameObject.name} 受到 {damageAmount:F1} 伤害, 剩余 HP: {CurrentHp:F1}");

            if (CurrentHp <= 0)
            {
                IsDead = true;
                PublishDiedEvent();
                AppLog.Info("Combat", $"{gameObject.name} 死亡");
            } else
            {
                PublishDamagedEvent(damageAmount, hitPoint, fromDirection);
            }
        }

        /// <summary>
        /// 发布死亡事件
        /// </summary>
        private void PublishDiedEvent()
        {
            if (_entity == null) return;
            if (_entity.Faction == FactionType.Player)
                EventBus.Publish(new CharacterDiedEvent());
            else
                EventBus.Publish(new EnemyDiedEvent(_entity));
        }

        /// <summary>
        /// 发布受伤事件
        /// </summary>
        /// <param name="damage"></param>
        private void PublishDamagedEvent(float damage, Vector3 hitPoint, Vector3 fromDirection)
        {
            if (_entity == null) return;
            if (_entity.Faction == FactionType.Player)
                EventBus.Publish(new CharacterDamagedEvent(damage, CurrentHp, fromDirection, hitPoint));
            else
                EventBus.Publish(new EnemyDamagedEvent(_entity, damage, CurrentHp, fromDirection, hitPoint));
        }



    }
}
