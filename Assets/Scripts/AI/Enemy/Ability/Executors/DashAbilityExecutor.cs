using UnityEngine;

namespace Emberwild.AI.Enemy.Ability
{
    /// <summary>
    /// 位移执行器。用于纯位移和位移攻击，按能力开始时的方向直线移动。
    /// </summary>
    public class DashAbilityExecutor : IAbilityExecutor
    {
        private readonly bool _dealsDamage;
        private float _endsAt;
        private float _hitAt;
        private bool _hitEmitted;
        private Vector3 _direction;
        private Vector3 _targetPosition;

        public DashAbilityExecutor(bool dealsDamage)
        {
            _dealsDamage = dealsDamage;
        }

        /// <summary>清理旧速度、播放位移动画，并计算突进方向和伤害时间。</summary>
        public void Start(AbilityDefinition definition, IEnemyAbilityContext context)
        {
            context.Agent.Stop();
            context.Agent.SetMotionLocked(false);
            context.Animator.SetMoveSpeed(1f);
            _endsAt = Time.time + Mathf.Max(0f, definition.duration);
            _hitAt = Time.time + Mathf.Max(0f, definition.hitDelay);
            _hitEmitted = false;
            _direction = ResolveDirection(context.Blackboard.Target, context.Transform);
            _targetPosition = context.Blackboard.Target != null
                ? context.Blackboard.Target.position
                : context.Transform.position + _direction;

            if (!string.IsNullOrWhiteSpace(definition.animationAction))
                context.Animator.PlaySemanticAction(definition.animationAction);
            if (!string.IsNullOrWhiteSpace(definition.animationState))
                context.Animator.SetSemanticState(definition.animationState, true);
        }

        /// <summary>持续施加突进速度，并在需要时发布攻击事件。</summary>
        public bool Tick(
            AbilityDefinition definition,
            IEnemyAbilityContext context,
            float deltaTime,
            float elapsed)
        {
            context.Agent.SetDirectVelocity(_direction, definition.speed);

            if (_dealsDamage && !_hitEmitted && Time.time >= _hitAt)
            {
                _hitEmitted = true;
                context.Combat.PublishAttack(definition.damageComboIndex, definition.ranged, _targetPosition);
            }
            return Time.time >= _endsAt;
        }

        /// <summary>停止位移并关闭持续姿态动画。</summary>
        public void Stop(AbilityDefinition definition, IEnemyAbilityContext context)
        {
            if (definition.stopMovementAtEnd) context.Agent.Stop();
            context.Animator.SetMoveSpeed(0f);
            context.Animator.SetSemanticState(definition.animationState, false);
        }

        private static Vector3 ResolveDirection(Transform target, Transform self)
        {
            if (target == null) return self.forward;
            Vector3 direction = target.position - self.position;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.001f ? direction.normalized : self.forward;
        }
    }
}
