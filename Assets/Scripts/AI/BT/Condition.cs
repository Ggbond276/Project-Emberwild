using Emberwild.AI.Enemy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emberwild.AI.BT
{
    public class Condition : BTNode
    {
        /// <summary>
        /// 这是一个判定规则委托
        /// </summary>
        private readonly Func<EnemyBrain, bool> _predicate;

        public Condition(Func<EnemyBrain, bool> predicate)
        {
            _predicate = predicate;
        }

        /// <summary>
        /// 执行这个节点可以告诉我们 节点是否可以执行
        /// </summary>
        /// <param name="brain"></param>
        /// <returns></returns>
        public override NodeState Evaluate(EnemyBrain brain)
        {
            return _predicate(brain) ? NodeState.Success : NodeState.Failure;
        }
    }
}
