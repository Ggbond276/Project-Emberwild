using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Emberwild.AI.Enemy.State
{
    public enum EnemyStateType
    {
        [InspectorName("待机")] Idle,
        [InspectorName("追击")] Chase,
        [InspectorName("近战攻击")] AttackMelee,
        [InspectorName("受击硬直")] Hit,
        [InspectorName("死亡")] Dead,
        [InspectorName("滚动冲刺")] RollChase,
        [InspectorName("远程攻击")] AttackRanged,
        [InspectorName("缩壳防御")] Hide,
    }
}
