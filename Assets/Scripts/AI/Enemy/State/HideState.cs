using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Emberwild.AI.Enemy.State
{
    public class HideState : IEnemyState
    {
        private float _hideEndTime;

        public void OnEnter(EnemyBrain brain)
        {
            _hideEndTime = Time.time + brain.HideDuration;
            brain.BB.HasHidden = true;   // 缩过一次壳就不再缩

            brain.Agent.Stop();
            brain.Animator.SetMoveSpeed(0f);
            brain.Animator.SetHidden(true);
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
            brain.Animator.SetHidden(false);
            brain.Entity.Attribute.IsHidden = false;
        }
    }
}
