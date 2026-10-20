using System;
using System.Collections.Generic;
using Emberwild.Combat;
using UnityEngine;

namespace Emberwild.AI.Enemy.Ability
{
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
}
