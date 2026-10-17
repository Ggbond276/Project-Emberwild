using Emberwild.AI.Enemy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emberwild.AI.BT
{
    /// <summary>
    /// 优先级选择：从左到右返回第一个Sucess的子节点结果
    /// </summary>
    public class Selector : BTNode
    {
        private readonly List<BTNode> _children;

        public Selector(params BTNode[] children)
        {
            _children = new List<BTNode>(children);
        }

        public override NodeState Evaluate(EnemyBrain brain)
        {
            foreach(var child in _children)
            {
                var state = child.Evaluate(brain);
                if (state != NodeState.Failure)
                    return state;
            }

            return NodeState.Failure;
        }
    }
}
