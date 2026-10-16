using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emberwild.AI.Enemy.State
{
    public class IdleState : IEnemyState
    {
        public void OnEnter(EnemyBrain brain)
        {
            brain.Agent.Stop();
            brain.Animator.SetMoveSpeed(0f);
        }

        public void OnUpdate(EnemyBrain brain)
        {
            if (brain.BB.CanSeeTarget)
            {
                brain.ChangeState(EnemyStateType.Chase);
            }
        }

        public void OnExit(EnemyBrain brain)
        {
            // 待机无需清理
        }


    }
}
