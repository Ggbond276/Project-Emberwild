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
            brain.Agent.SetMotionLocked(true);
            brain.Animator.SetMoveSpeed(0f);
        }

        public void OnUpdate(EnemyBrain brain)
        {
        }

        public void OnExit(EnemyBrain brain)
        {
            brain.Agent.SetMotionLocked(false);
        }


    }
}
