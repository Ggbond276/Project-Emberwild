using UnityEngine;

namespace Emberwild.AI.Enemy.Ability
{
    /// <summary>
    /// 原地攻击执行器，支持近战和远程攻击、命中延迟以及移动锁定。
    /// </summary>
    public class AttackAbilityExecutor : IAbilityExecutor
    {
        private readonly bool _ranged;
        private bool _hitEmitted;
        private Vector3 _targetPosition;

        public AttackAbilityExecutor(bool ranged)
        {
            _ranged = ranged;
        }

        /// <summary>
        /// 开始攻击动画、面向目标，并记录本次攻击的目标位置。
        /// </summary>
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

        /// <summary>到达命中时间时发布攻击事件，持续时间结束后返回完成。</summary>
        public bool Tick(
            AbilityDefinition definition,
            IEnemyAbilityContext context,
            float deltaTime,
            float elapsed)
        {
            if (!_hitEmitted && elapsed >= definition.hitDelay)
            {
                _hitEmitted = true;
                context.Combat.PublishAttack(definition.damageComboIndex, _ranged, _targetPosition);
            }
            return elapsed >= Mathf.Max(0f, definition.duration);
        }

        /// <summary>解除攻击期间的移动锁定。</summary>
        public void Stop(AbilityDefinition definition, IEnemyAbilityContext context)
        {
            if (definition.lockMovement) context.Agent.SetMotionLocked(false);
        }
    }
}
