using UnityEngine;

namespace Emberwild.AI.Enemy.Ability
{
    public class DashAbilityExecutor : IAbilityExecutor
    {
        #region 执行器运行时状态 (Executor Runtime State)
        private readonly bool _dealsDamage;
        private float _endsAt;
        private float _hitAt;
        private bool _hitEmitted;
        private Vector3 _direction;
        private Vector3 _targetPosition;
        #endregion

        #region 构造方法 (Constructor)
        /// <summary>创建纯位移或位移攻击执行器。</summary>
        public DashAbilityExecutor(bool dealsDamage)
        {
            _dealsDamage = dealsDamage;
        }
        #endregion

        #region 能力进度 (Ability Progress)
        /// <summary>持续施加突进速度并在命中时间发布攻击。</summary>
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
                context.Combat.PublishAttack(
                    definition.damageComboIndex,
                    definition.ranged,
                    _targetPosition);
            }
            return Time.time >= _endsAt;
        }
        #endregion

        #region 能力开始、结束与方向计算 (Ability Start, Stop & Direction)
        /// <summary>播放位移动画并计算突进方向和时间。</summary>
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

        /// <summary>停止位移并关闭持续动画状态。</summary>
        public void Stop(AbilityDefinition definition, IEnemyAbilityContext context)
        {
            if (definition.stopMovementAtEnd) context.Agent.Stop();
            context.Animator.SetMoveSpeed(0f);
            context.Animator.SetSemanticState(definition.animationState, false);
        }

        /// <summary>计算从自身指向目标的水平方向。</summary>
        private static Vector3 ResolveDirection(Transform target, Transform self)
        {
            if (target == null) return self.forward;
            Vector3 direction = target.position - self.position;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.001f ? direction.normalized : self.forward;
        }
        #endregion
    }
}
