using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emberwild.AI.Enemy.State
{
    public class ChaseState : IEnemyState
    {
        public void OnEnter(EnemyBrain brain)
        {
            // 进入追击无需特殊动作
        }

        public void OnUpdate(EnemyBrain brain)
        {
            var bb = brain.BB;

            if (bb.Target == null || !bb.CanSeeTarget)
            {
                brain.ChangeState(EnemyStateType.Idle);
                return;
            }

            brain.Agent.SetDestination(bb.Target.position);

            brain.Animator.SetMoveSpeed(brain.NormalizedSpeed);
        }

        public void OnExit(EnemyBrain brain)
        {
            brain.Agent.Stop();
            brain.Animator.SetMoveSpeed(0f);
        }
    }
}
