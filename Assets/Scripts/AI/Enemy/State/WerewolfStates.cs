using Emberwild.AI.Enemy.EAnimator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Emberwild.AI.Enemy.State
{
    /// <summary>
    /// 警戒：张望一次后转入追击
    /// </summary>
    public class AlertState : IEnemyState
    {
        private float _endTime;

        public void OnEnter(EnemyBrain brain)
        {
            brain.Agent.SetMotionLocked(true);
            brain.Animator.SetMoveSpeed(0f);
            brain.Animator.PlayAction(EnemyAction.Alert);

            brain.BB.HasAlerted = true;
            _endTime = Time.time + brain.AlertDuration;
        }

        public void OnUpdate(EnemyBrain brain)
        {
            if (Time.time >= _endTime)
                brain.ChangeState(brain.BB.CanSeeTarget ? EnemyStateType.Chase : EnemyStateType.Idle);
        }

        public void OnExit(EnemyBrain brain) { brain.Agent.SetMotionLocked(false); }
    }


    /// <summary>重击：慢速高伤，使用独立冷却。</summary>
    public class AttackHeavyState : IEnemyState
    {
        // TODO: 我觉得这些问题都是可以使用配置表解决的
        public void OnEnter(EnemyBrain brain)
        {
            brain.Agent.SetMotionLocked(true);
            brain.Animator.SetMoveSpeed(0f);

            if (brain.BB.Target != null) brain.Agent.FaceTowards(brain.BB.Target.position);

            brain.Combat.DoAttack(EnemyAction.Heavy, brain.HeavyAttackDuration);
            brain.BB.LastHeavyTime = Time.time;
            brain.BB.LastAttackTime = Time.time;
        }

        public void OnUpdate(EnemyBrain brain)
        {
            if (!brain.Combat.IsAttacking)
                brain.ChangeState(brain.BB.CanSeeTarget ? EnemyStateType.Chase : EnemyStateType.Idle);
        }

        public void OnExit(EnemyBrain brain) { brain.Agent.SetMotionLocked(false); }
    }

    /// <summary>扑击：中距离突进并攻击。</summary>
    public class PounceState : IEnemyState
    {
        private Vector3 _direction;
        private float _endTime;

        public void OnEnter(EnemyBrain brain)
        {
            // 突进需要位移，只清速度不锁定
            brain.Agent.Stop();
            brain.Animator.SetMoveSpeed(1f);
            brain.Animator.PlayAction(EnemyAction.Pounce);

            _direction = ResolveForward(brain);
            brain.BB.LastPounceTime = Time.time;
            brain.BB.LastAttackTime = Time.time;
            brain.Combat.DoAttack(EnemyAction.Pounce);

            _endTime = Time.time + brain.PounceDuration;
        }

        public void OnUpdate(EnemyBrain brain)
        {
            brain.Agent.SetDirectVelocity(_direction, brain.PounceSpeed);

            if (Time.time >= _endTime)
                brain.ChangeState(brain.BB.CanSeeTarget ? EnemyStateType.Chase : EnemyStateType.Idle);
        }

        public void OnExit(EnemyBrain brain)
        {
            brain.Agent.SetMotionLocked(false);
            brain.Animator.SetMoveSpeed(0f);
        }

        private static Vector3 ResolveForward(EnemyBrain brain)
        {
            if (brain.BB.Target == null) return brain.transform.forward;

            Vector3 dir = brain.BB.Target.position - brain.transform.position;
            dir.y = 0f;
            return dir.sqrMagnitude > 0.001f ? dir.normalized : brain.transform.forward;
        }

    }

    /// <summary>跃击：远距离跳跃并攻击。</summary>
    public class LeapState : IEnemyState
    {
        private Vector3 _direction;
        private float _endTime;

        public void OnEnter(EnemyBrain brain)
        {
            // 跃击需要位移，只清速度不锁定
            brain.Agent.Stop();
            brain.Animator.SetMoveSpeed(1f);
            brain.Animator.PlayAction(EnemyAction.Leap);

            Vector3 dir = brain.BB.Target != null
                ? brain.BB.Target.position - brain.transform.position
                : brain.transform.forward;
            dir.y = 0f;
            _direction = dir.sqrMagnitude > 0.001f ? dir.normalized : brain.transform.forward;

            brain.BB.LastLeapTime = Time.time;
            brain.Combat.DoAttack(EnemyAction.Leap);

            _endTime = Time.time + brain.LeapDuration;
        }

        public void OnUpdate(EnemyBrain brain)
        {
            brain.Agent.SetDirectVelocity(_direction, brain.LeapSpeed);

            if (Time.time >= _endTime)
                brain.ChangeState(brain.BB.CanSeeTarget ? EnemyStateType.Chase : EnemyStateType.Idle);
        }

        public void OnExit(EnemyBrain brain)
        {
            brain.Agent.SetMotionLocked(false);
            brain.Animator.SetMoveSpeed(0f);
        }
    }

    /// <summary>施法：播放施法动作，期间不可移动。</summary>
    public class CastSpellState : IEnemyState
    {
        private float _endTime;

        public void OnEnter(EnemyBrain brain)
        {
            brain.Agent.SetMotionLocked(true);
            brain.Animator.SetMoveSpeed(0f);
            brain.Animator.PlayAction(EnemyAction.Spell);

            brain.BB.LastSpellTime = Time.time;
            _endTime = Time.time + brain.SpellDuration;
        }

        public void OnUpdate(EnemyBrain brain)
        {
            if (Time.time >= _endTime)
                brain.ChangeState(brain.BB.CanSeeTarget ? EnemyStateType.Chase : EnemyStateType.Idle);
        }

        public void OnExit(EnemyBrain brain) { brain.Agent.SetMotionLocked(false); }
    }


}
