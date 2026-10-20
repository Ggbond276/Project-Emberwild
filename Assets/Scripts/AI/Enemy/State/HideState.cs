using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Emberwild.AI.Enemy.EAnimator;

namespace Emberwild.AI.Enemy.State
{
    /// <summary>缩壳：进入 Stance 姿态并减伤，超时后退出。</summary>
    public class HideState : IEnemyState
    {
        private float _hideEndTime;

        public void OnEnter(EnemyBrain brain)
        {
            _hideEndTime = Time.time + brain.HideDuration;
            brain.BB.HasHidden = true;

            brain.Agent.SetMotionLocked(true);
            brain.Animator.SetMoveSpeed(0f);
            brain.Animator.SetState(EnemyStateFlag.Stance, true);
            brain.Entity.Attribute.IsHidden = true;
        }

        public void OnUpdate(EnemyBrain brain)
        {
            if(Time.time >= _hideEndTime)
            {
                brain.ChangeState(brain.BB.CanSeeTarget ? EnemyStateType.Chase : EnemyStateType.Idle);
            }
        }

        public void OnExit(EnemyBrain brain)
        {
            brain.Agent.SetMotionLocked(false);
            brain.Animator.SetState(EnemyStateFlag.Stance, false);
            brain.Entity.Attribute.IsHidden = false;
        }
    }
}
