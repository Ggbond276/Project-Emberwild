using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Emberwild.AI.Enemy.State
{
    public class AttackRangedState : IEnemyState
    {
        public void OnEnter(EnemyBrain brain)
        {
            brain.Agent.Stop();
            brain.Animator.SetMoveSpeed(0f);

            var target = brain.BB.Target;
            if(target != null)
            {
                brain.Agent.FaceTowards(target.position);
                brain.Combat.DoRangedAttack(target.position);
            }

            brain.BB.LastAttackTime = Time.time;
        }

        public void OnUpdate(EnemyBrain brain)
        {
            if(!brain.Combat.IsAttacking)
            {
                brain.ChangeState(brain.BB.CanSeeTarget ? EnemyStateType.Chase : EnemyStateType.Idle);
            }
        }

        public void OnExit(EnemyBrain brain)
        {
            // 无需退出清理
        }

        
    }
}
