using Emberwild.AI.Enemy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emberwild.AI.BT
{
    public class Sequence : BTNode
    {
        private readonly List<BTNode> _children;

        public Sequence(params BTNode[] children)
        {
            _children = new List<BTNode>(children);
        }

        public override NodeState Evaluate(EnemyBrain brain)
        {
            foreach(var child in _children)
            {
                var state = child.Evaluate(brain);
                if (state != NodeState.Success)
                    return state;
            }
            return NodeState.Success;
        }
    }
}
