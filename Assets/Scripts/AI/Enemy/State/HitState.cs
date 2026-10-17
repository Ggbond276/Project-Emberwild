using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Emberwild.AI.Enemy.State
{
    public class HitState : IEnemyState
    {
        private float _hitEndTime;

        public void OnEnter(EnemyBrain brain)
        {
            _hitEndTime = Time.time + brain.HitStunDuration;

            brain.Agent.Stop();
            brain.Animator.SetMoveSpeed(0f);
            brain.Animator.PlayTakeDamage();
        }

        public void OnUpdate(EnemyBrain brain)
        {
            if(Time.time >= _hitEndTime)
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
