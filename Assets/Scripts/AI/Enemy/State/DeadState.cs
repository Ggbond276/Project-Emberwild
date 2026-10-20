using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emberwild.AI.Enemy.State
{
    public class DeadState : IEnemyState
    {
        public void OnEnter(EnemyBrain brain)
        {
            brain.Agent.SetMotionLocked(true);
            brain.Animator.SetMoveSpeed(0f);
            brain.Animator.SetDead(true);

            brain.Agent.enabled = false;
            brain.Perception.enabled = false;
        }

        public void OnUpdate(EnemyBrain brain)
        {
            // 终止状态没有出口
        }


        public void OnExit(EnemyBrain brain)
        {
            // 终止状态没有出口
        }
    }
}
