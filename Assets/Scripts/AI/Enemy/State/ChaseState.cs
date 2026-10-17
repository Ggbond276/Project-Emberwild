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
            var bb = brain.BB;

            if (bb.Target == null || !bb.CanSeeTarget)
            {
                brain.ChangeState(EnemyStateType.Idle);
                return;
            }

            if(bb.HpRatio < brain.HideHpThreshold)
            {
                brain.ChangeState(EnemyStateType.Hide);
                return;
            }

            // 距离足够近 → 切近战攻击
            if (bb.DistanceToTarget <= brain.MeleeRange)
            {
                brain.ChangeState(EnemyStateType.AttackMelee);
                return;
            }

            // 中等距离 -> 翻滚突进
            if(bb.DistanceToTarget >= brain.RollMinRange && 
                bb.DistanceToTarget <= brain.RollMaxRange &&
                Time.time - bb.LastRollTime >= brain.RollCooldown)
            {
                brain.ChangeState(EnemyStateType.RollChase);
                return;
            }

            // 中远距离 + 冷却就绪 -> 远程攻击
            if(bb.DistanceToTarget >= brain.RangedMinRange &&
                bb.DistanceToTarget <= brain.RangedMaxRange &&
                Time.time - bb.LastAttackTime >= brain.AttackCooldown)
            {
                brain.ChangeState(EnemyStateType.AttackRanged);
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
