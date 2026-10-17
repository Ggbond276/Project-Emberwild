using Emberwild.AI.Enemy;
using Emberwild.AI.Enemy.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emberwild.AI.BT
{
    public class ActionNode : BTNode
    {
        private readonly EnemyStateType _stateType;

        public ActionNode(EnemyStateType stateType)
        {
            _stateType = stateType;
        }

        /// <summary>
        /// 执行节点切换状态
        /// </summary>
        /// <param name="brain"></param>
        /// <returns></returns>
        public override NodeState Evaluate(EnemyBrain brain)
        {
            brain.ChangeState(_stateType);
            return NodeState.Running;
        }
    }
}
