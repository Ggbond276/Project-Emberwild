using Emberwild.Combat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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
        public readonly Vector3 FromDirection;

        /// <summary>
        /// 攻击命中点，特效生成位置
        /// </summary>
        public readonly Vector3 HitPoint;

        public EnemyDamagedEvent(ICombatEntity victim, float damage, float currentHp, Vector3 fromDirection, Vector3 hitPoint)
        {
            Victim = victim;
            Damage = damage;
            CurrentHp = currentHp;
            FromDirection = fromDirection;
            HitPoint = hitPoint;
        }
    }
}
