using Emberwild.AI.Enemy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emberwild.AI.BT
{
    public abstract class BTNode
    {
        public abstract NodeState Evaluate(EnemyBrain brain);
    }
}
