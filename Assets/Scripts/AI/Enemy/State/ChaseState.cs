using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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
            
            if(brain.BB.Target != null)
            {
                brain.Agent.SetDestination(brain.BB.Target.position);
                brain.Animator.SetMoveSpeed(brain.NormalizedSpeed);
            }
            
        }

        public void OnExit(EnemyBrain brain)
        {
            brain.Agent.Stop();
            brain.Animator.SetMoveSpeed(0f);
        }
    }
}
