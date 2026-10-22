using UnityEngine;

namespace Emberwild.AI.Enemy.Ability
{
    public class AttackAbilityExecutor : IAbilityExecutor
    {
        #region 执行器运行时状态 (Executor Runtime State)
        private readonly bool _ranged;
        private bool _hitEmitted;
        private Vector3 _targetPosition;
        #endregion

        #region 构造方法 (Constructor)
        /// <summary>创建近战或远程攻击执行器。</summary>
        public AttackAbilityExecutor(bool ranged)
        {
            _ranged = ranged;
        }
        #endregion

        #region 能力进度 (Ability Progress)
        /// <summary>
        /// 到达命中时间后发布攻击事件。
        /// </summary>
        public bool Tick(
            AbilityDefinition definition,
            IEnemyAbilityContext context,
            float deltaTime,
            float elapsed) // 从 Start 到现在过了多久，由运行时算好（Time.time - _activeStartTime）
        {
            if (!_hitEmitted && elapsed >= definition.hitDelay)
            {
                _hitEmitted = true;
                context.Combat.PublishAttack(
                    definition.damageComboIndex,
                    _ranged,
                    _targetPosition);
            }
            return elapsed >= Mathf.Max(0f, definition.duration);
        }
        #endregion

        #region 能力开始与结束 (Ability Start & End)
        /// <summary>播放攻击动画、面向目标并锁定移动。</summary>
        public void Start(AbilityDefinition definition, IEnemyAbilityContext context)
        {
            _hitEmitted = false;
            _targetPosition = context.Blackboard.Target != null
                ? context.Blackboard.Target.position
                : context.Transform.position + context.Transform.forward;

            if (definition.lockMovement) context.Agent.SetMotionLocked(true);
            context.Animator.SetMoveSpeed(0f);

            if (context.Blackboard.Target != null)
                context.Agent.FaceTowards(context.Blackboard.Target.position);

            if (!string.IsNullOrWhiteSpace(definition.animationAction))
                context.Animator.PlaySemanticAction(definition.animationAction);
        }

        /// <summary>解除攻击期间的移动锁定。</summary>
        public void Stop(AbilityDefinition definition, IEnemyAbilityContext context)
        {
            if (definition.lockMovement) context.Agent.SetMotionLocked(false);
        }
        #endregion
    }
}
