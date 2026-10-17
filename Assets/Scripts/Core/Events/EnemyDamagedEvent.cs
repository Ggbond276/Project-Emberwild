using Emberwild.Combat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emberwild.Core.Events
{
    public readonly struct EnemyDamagedEvent : IEvent 
    {

        public readonly ICombatEntity Victim;
        /// <summary>
        /// 实际扣血量
        /// </summary>
        public readonly float Damage;
        /// <summary>
        /// 受到伤害后剩余的HP
        /// </summary>
        public readonly float CurrentHp;
        /// <summary>
        /// 收到攻击的方向
        /// </summary>
        public readonly UnityEngine.Vector3 FromDirection;

        public EnemyDamagedEvent(ICombatEntity victim, float damage, float currentHp, UnityEngine.Vector3 fromDirection)
        {
            Victim = victim;
            Damage = damage;
            CurrentHp = currentHp;
            FromDirection = fromDirection;
        }
    }
}
