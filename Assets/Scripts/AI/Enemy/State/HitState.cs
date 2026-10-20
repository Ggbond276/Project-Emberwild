using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Emberwild.AI.Enemy.EAnimator;

namespace Emberwild.AI.Enemy.State
{
    /// <summary>受击硬直：停下并播放受击动作，硬直结束后转 Chase / Idle。</summary>
    public class HitState : IEnemyState
    {
        private float _hitEndTime;

        public void OnEnter(EnemyBrain brain)
        {
            _hitEndTime = Time.time + brain.HitStunDuration;

            brain.Agent.SetMotionLocked(true);
            brain.Animator.SetMoveSpeed(0f);
            brain.Animator.PlayAction(EnemyAction.TakeDamage);
        }

        public void OnUpdate(EnemyBrain brain)
        {
            if(Time.time >= _hitEndTime)
            {
                brain.ChangeState(brain.BB.CanSeeTarget ? EnemyStateType.Chase : EnemyStateType.Idle);
            }
        }

        public void OnExit(EnemyBrain brain) { brain.Agent.SetMotionLocked(false); }
    }
}
