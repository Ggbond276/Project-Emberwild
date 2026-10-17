using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Emberwild.AI.Enemy.State
{
    /// <summary>
    /// 滚动冲刺状态：朝向玩家直线高速突进，持续一段时间后回归Chase/Idle
    /// </summary>
    public class RollChaseState : IEnemyState
    {
        private float _rollEndTime;
        private Vector3 _rollDir;

        public void OnEnter(EnemyBrain brain)
        {
            brain.Agent.Stop();
            //TODO
            brain.Animator.SetRoll(true);
            brain.BB.LastRollTime = Time.time;
            _rollEndTime = Time.time + brain.RollDuration;

            var target = brain.BB.Target;
            if(target != null)
            {
                Vector3 dir = target.position - brain.transform.position;
                dir.y = 0;
                _rollDir = dir.sqrMagnitude > 0.0001f ? dir.normalized : brain.transform.forward;
            } else
            {
                _rollDir = brain.transform.forward;
            }
        }

        public void OnUpdate(EnemyBrain brain)
        {
            if(Time.time >= _rollEndTime)
            {
                brain.ChangeState(brain.BB.CanSeeTarget ? EnemyStateType.Chase : EnemyStateType.Idle);
                return;
            }
            brain.Agent.SetDirectVelocity(_rollDir, brain.RollSpeed);
            
        }

        public void OnExit(EnemyBrain brain)
        {
            brain.Agent.Stop();
            //TODO
            brain.Animator.SetRoll(false);
        }
    }
}
