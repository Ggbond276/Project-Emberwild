using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Emberwild.AI.Enemy.State
{
    public class AttackMeleeState : IEnemyState
    {
        public void OnEnter(EnemyBrain brain)
        {
            brain.Agent.Stop();
            brain.Animator.SetMoveSpeed(0f);

            // 攻击前先面向玩家:玩家在攻击范围内移动时,保证判定框正对目标,避免空攻击
            if (brain.BB.Target != null)
            {
                brain.Agent.FaceTowards(brain.BB.Target.position);
            }

            brain.Combat.DoAttack();
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
            // 无需清理
        }
    }
}
