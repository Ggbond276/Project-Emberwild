using System;
using System.Collections.Generic;
using Emberwild.AI.Enemy.EAnimator;
using Emberwild.AI.Pathfinding;
using Emberwild.Combat;
using UnityEngine;

namespace Emberwild.AI.Enemy.Ability
{
    /// <summary>
    /// 能力执行器可以使用的最小运行时上下文。
    /// 执行器通过它访问移动、战斗、动画和黑板，而不依赖完整的 EnemyBrain。
    /// </summary>
    public interface IEnemyAbilityContext
    {
        Transform Transform { get; }
        EnemyBlackboard Blackboard { get; }
        CustomNavAgent Agent { get; }
        IEnemyAnimator Animator { get; }
        EnemyCombat Combat { get; }
        CombatAttribute Attribute { get; }
    }

    /// <summary>
    /// 通用能力执行器契约。一次能力经历 Start、持续 Tick 和最终 Stop。
    /// </summary>
    public interface IAbilityExecutor
    {
        /// <summary>能力开始时初始化动画、移动和命中状态。</summary>
        void Start(AbilityDefinition definition, IEnemyAbilityContext context);

        /// <summary>每帧推进能力；返回 true 表示能力已经结束。</summary>
        bool Tick(AbilityDefinition definition, IEnemyAbilityContext context, float deltaTime, float elapsed);

        /// <summary>正常结束或被中断时清理锁移动、姿态和临时属性。</summary>
        void Stop(AbilityDefinition definition, IEnemyAbilityContext context);
    }

    /// <summary>
    /// 执行器注册表。普通能力使用内置执行器，特殊 Boss 机制可以注册独立实现。
    /// </summary>
    public static class AbilityExecutorRegistry
    {
        private static readonly Dictionary<string, Func<IAbilityExecutor>> Factories =
            new Dictionary<string, Func<IAbilityExecutor>>(StringComparer.OrdinalIgnoreCase)
            {
                [AbilityExecutorIds.Attack] = () => new AttackAbilityExecutor(false),
                [AbilityExecutorIds.RangedAttack] = () => new AttackAbilityExecutor(true),
                [AbilityExecutorIds.Dash] = () => new DashAbilityExecutor(false),
                [AbilityExecutorIds.DashAttack] = () => new DashAbilityExecutor(true),
                [AbilityExecutorIds.Stance] = () => new StanceAbilityExecutor(),
                [AbilityExecutorIds.PlayAndWait] = () => new PlayAndWaitAbilityExecutor(),
                [AbilityExecutorIds.Dead] = () => new DeadAbilityExecutor()
            };

        /// <summary>注册或替换一个执行器工厂。</summary>
        public static void Register(string executorId, Func<IAbilityExecutor> factory)
        {
            if (string.IsNullOrWhiteSpace(executorId) || factory == null) return;
            Factories[executorId] = factory;
        }

        /// <summary>判断指定执行器 ID 是否已注册。</summary>
        public static bool Contains(string executorId)
        {
            return !string.IsNullOrWhiteSpace(executorId) && Factories.ContainsKey(executorId);
        }

        /// <summary>创建一个新的执行器实例；未知 ID 返回 null。</summary>
        public static IAbilityExecutor Create(string executorId)
        {
            return !string.IsNullOrWhiteSpace(executorId)
                && Factories.TryGetValue(executorId, out var factory)
                ? factory()
                : null;
        }
    }

    /// <summary>
    /// 单只怪物的能力运行时，负责能力查找、距离与冷却判断、激活和中断。
    /// </summary>
    public sealed class EnemyAbilityRuntime
    {
        private readonly IEnemyAbilityContext _context;
        private readonly Dictionary<string, AbilityDefinition> _byId =
            new Dictionary<string, AbilityDefinition>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<AbilityDefinition>> _bySlot =
            new Dictionary<string, List<AbilityDefinition>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, float> _lastUsed =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _usedOnce =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private AbilityDefinition _activeDefinition;
        private IAbilityExecutor _activeExecutor;
        private float _activeStartTime;
        private float _lastAttackTime = -999f;

        public EnemyAbilityRuntime(IEnemyAbilityContext context, IEnumerable<AbilityDefinition> abilities)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            if (abilities == null) return;

            foreach (var ability in abilities)
            {
                if (ability == null || string.IsNullOrWhiteSpace(ability.id)) continue;
                _byId[ability.id] = ability;

                if (string.IsNullOrWhiteSpace(ability.slot)) continue;
                if (!_bySlot.TryGetValue(ability.slot, out var list))
                {
                    list = new List<AbilityDefinition>();
                    _bySlot[ability.slot] = list;
                }
                list.Add(ability);
                list.Sort((a, b) => b.priority.CompareTo(a.priority));
            }
        }

        /// <summary>当前是否有能力正在执行。</summary>
        public bool IsActive => _activeDefinition != null;

        /// <summary>当前执行能力 ID；没有能力时为空字符串。</summary>
        public string ActiveAbilityId => _activeDefinition != null ? _activeDefinition.id : string.Empty;

        /// <summary>当前执行能力所属槽 ID。</summary>
        public string ActiveSlot => _activeDefinition != null ? _activeDefinition.slot : string.Empty;

        /// <summary>距离最近一次攻击型能力开始的时间。</summary>
        public float TimeSinceLastAttack => Time.time - _lastAttackTime;

        /// <summary>按能力 ID 尝试激活能力；已有能力或冷却未结束时返回 false。</summary>
        public bool TryActivateAbility(string abilityId)
        {
            if (IsActive || string.IsNullOrWhiteSpace(abilityId)) return false;
            if (!_byId.TryGetValue(abilityId, out var ability)) return false;
            return TryActivate(ability);
        }

        /// <summary>在指定槽内按优先级寻找一个当前可用的能力并激活。</summary>
        public bool TryActivateSlot(string slot)
        {
            if (IsActive || string.IsNullOrWhiteSpace(slot)) return false;
            if (!_bySlot.TryGetValue(slot, out var abilities)) return false;

            foreach (var ability in abilities)
            {
                if (CanActivate(ability)) return TryActivate(ability);
            }
            return false;
        }

        /// <summary>推进当前能力，完成时自动调用执行器的 Stop。</summary>
        public void Tick(float deltaTime)
        {
            if (!IsActive) return;
            float elapsed = Time.time - _activeStartTime;
            if (_activeExecutor.Tick(_activeDefinition, _context, deltaTime, elapsed))
            {
                StopActive();
            }
        }

        /// <summary>中断当前能力并执行统一清理。</summary>
        public void Abort()
        {
            StopActive();
        }

        /// <summary>判断指定能力是否已经超过给定冷却时间。</summary>
        public bool IsAbilityReady(string abilityId, float cooldown)
        {
            if (string.IsNullOrWhiteSpace(abilityId)) return false;
            if (!_byId.ContainsKey(abilityId)) return false;
            return Time.time - GetLastUsed(abilityId) >= Mathf.Max(0f, cooldown);
        }

        /// <summary>判断槽内是否至少有一个能力满足冷却和距离条件。</summary>
        public bool IsSlotReady(string slot, float cooldown)
        {
            if (string.IsNullOrWhiteSpace(slot) || !_bySlot.TryGetValue(slot, out var list)) return false;
            float now = Time.time;
            foreach (var ability in list)
            {
                if (ability.oncePerCombat && _usedOnce.Contains(ability.id)) continue;
                float effectiveCooldown = Mathf.Max(cooldown, ability.cooldown);
                if (now - GetLastUsed(ability.id) >= effectiveCooldown && IsInRange(ability))
                    return true;
            }
            return false;
        }

        /// <summary>判断槽内能力是否都没有使用过，用于一次性警戒或姿态能力。</summary>
        public bool IsSlotUnused(string slot)
        {
            if (string.IsNullOrWhiteSpace(slot) || !_bySlot.TryGetValue(slot, out var list)) return false;
            foreach (var ability in list)
            {
                if (_usedOnce.Contains(ability.id)) return false;
            }
            return true;
        }

        private bool TryActivate(AbilityDefinition ability)
        {
            if (!CanActivate(ability)) return false;

            var executor = AbilityExecutorRegistry.Create(ability.executor);
            if (executor == null)
            {
                Debug.LogError($"[EnemyAbility] Unknown executor '{ability.executor}' for ability '{ability.id}'.");
                return false;
            }

            _activeDefinition = ability;
            _activeExecutor = executor;
            _activeStartTime = Time.time;
            _lastUsed[ability.id] = _activeStartTime;
            _usedOnce.Add(ability.id);

            if (ability.countsAsAttack)
                _lastAttackTime = _activeStartTime;

            _activeExecutor.Start(ability, _context);
            return true;
        }

        private bool CanActivate(AbilityDefinition ability)
        {
            if (ability.oncePerCombat && _usedOnce.Contains(ability.id)) return false;
            if (Time.time - GetLastUsed(ability.id) < Mathf.Max(0f, ability.cooldown)) return false;
            return IsInRange(ability);
        }

        private bool IsInRange(AbilityDefinition ability)
        {
            if (ability.slot == AbilitySlotIds.HitReaction || ability.slot == AbilitySlotIds.DeathReaction)
                return true;
            float distance = _context.Blackboard.DistanceToTarget;
            return distance >= ability.minRange && distance <= ability.maxRange;
        }

        private float GetLastUsed(string abilityId)
        {
            return _lastUsed.TryGetValue(abilityId, out var time) ? time : -999f;
        }

        private void StopActive()
        {
            if (_activeDefinition == null) return;
            _activeExecutor.Stop(_activeDefinition, _context);
            _activeDefinition = null;
            _activeExecutor = null;
            _activeStartTime = 0f;
        }
    }

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

        /// <summary>开始攻击动画、面向目标，并记录本次攻击的目标位置。</summary>
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

    /// <summary>
    /// 持续姿态执行器。用于缩壳等能力，统一处理移动锁和承伤倍率。
    /// </summary>
    public class StanceAbilityExecutor : IAbilityExecutor
    {
        private float _endsAt;
        private float _previousDamageMultiplier = 1f;

        /// <summary>锁定移动、开启姿态动画并设置承伤倍率。</summary>
        public void Start(AbilityDefinition definition, IEnemyAbilityContext context)
        {
            context.Agent.SetMotionLocked(true);
            context.Animator.SetMoveSpeed(0f);
            context.Animator.SetSemanticState(definition.animationState, true);
            _endsAt = Time.time + Mathf.Max(0f, definition.duration);

            if (context.Attribute != null)
            {
                _previousDamageMultiplier = context.Attribute.DamageTakenMultiplier;
                context.Attribute.DamageTakenMultiplier = definition.damageTakenMultiplier;
            }
        }

        /// <summary>姿态时间结束后返回完成。</summary>
        public bool Tick(
            AbilityDefinition definition,
            IEnemyAbilityContext context,
            float deltaTime,
            float elapsed)
        {
            return Time.time >= _endsAt;
        }

        /// <summary>关闭姿态并恢复进入前的承伤倍率。</summary>
        public void Stop(AbilityDefinition definition, IEnemyAbilityContext context)
        {
            context.Agent.SetMotionLocked(false);
            context.Animator.SetSemanticState(definition.animationState, false);
            if (context.Attribute != null)
                context.Attribute.DamageTakenMultiplier = _previousDamageMultiplier;
        }
    }

    /// <summary>
    /// 播放并等待执行器。用于警戒、受击等没有持续逻辑的一次性动作。
    /// </summary>
    public class PlayAndWaitAbilityExecutor : IAbilityExecutor
    {
        private float _endsAt;

        /// <summary>播放语义动作，可选锁移动，并开始计时。</summary>
        public void Start(AbilityDefinition definition, IEnemyAbilityContext context)
        {
            if (definition.lockMovement) context.Agent.SetMotionLocked(true);
            context.Animator.SetMoveSpeed(0f);
            if (!string.IsNullOrWhiteSpace(definition.animationAction))
                context.Animator.PlaySemanticAction(definition.animationAction);
            _endsAt = Time.time + Mathf.Max(0f, definition.duration);
        }

        /// <summary>计时结束后返回完成。</summary>
        public bool Tick(
            AbilityDefinition definition,
            IEnemyAbilityContext context,
            float deltaTime,
            float elapsed)
        {
            return Time.time >= _endsAt;
        }

        /// <summary>解除可选的移动锁定。</summary>
        public void Stop(AbilityDefinition definition, IEnemyAbilityContext context)
        {
            if (definition.lockMovement) context.Agent.SetMotionLocked(false);
        }
    }

    /// <summary>
    /// 死亡执行器。锁定移动、播放死亡并关闭寻路组件。
    /// </summary>
    public class DeadAbilityExecutor : IAbilityExecutor
    {
        /// <summary>进入不可逆死亡表现。</summary>
        public void Start(AbilityDefinition definition, IEnemyAbilityContext context)
        {
            context.Agent.SetMotionLocked(true);
            context.Animator.SetMoveSpeed(0f);
            context.Animator.SetDead(true);
            context.Agent.enabled = false;
        }

        /// <summary>死亡能力不会正常结束。</summary>
        public bool Tick(
            AbilityDefinition definition,
            IEnemyAbilityContext context,
            float deltaTime,
            float elapsed)
        {
            return false;
        }

        /// <summary>死亡状态不恢复移动。</summary>
        public void Stop(AbilityDefinition definition, IEnemyAbilityContext context)
        {
        }
    }
}
