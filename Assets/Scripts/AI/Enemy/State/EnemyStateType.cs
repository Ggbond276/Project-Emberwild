using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emberwild.AI.Enemy.State
{
    public enum EnemyStateType
    {
        Idle,
        Chase,
        AttackMelee, // 近战攻击
        Hit,
        Dead,
    }
}
