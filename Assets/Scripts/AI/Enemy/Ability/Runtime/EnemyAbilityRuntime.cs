using System;
using System.Collections.Generic;
using Emberwild.Combat;
using UnityEngine;

namespace Emberwild.AI.Enemy.Ability
{
    public sealed class EnemyAbilityRuntime
    {
        #region 能力执行上下文 (Ability Context)
        private readonly IEnemyAbilityContext _context;
        #endregion

        #region 能力索引与活动状态 (Ability Index & Active State)
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
        #endregion

        #region 能力状态查询 (Ability Queries)
        public bool IsActive => _activeDefinition != null;
        public string ActiveAbilityId =>
            _activeDefinition != null ? _activeDefinition.id : string.Empty;
        public string ActiveSlot =>
            _activeDefinition != null ? _activeDefinition.slot : string.Empty;
        public float TimeSinceLastAttack => Time.time - _lastAttackTime;
        #endregion

        #region 构造与索引建立 (Construction & Indexing)
        /// <summary>建立能力 ID、槽位和优先级查找表。</summary>
        public EnemyAbilityRuntime(
            IEnemyAbilityContext context,
            IEnumerable<AbilityDefinition> abilities)
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
        #endregion

        #region 能力推进与条件判断 (Ability Tick & Checks)
        /// <summary>推进当前活动能力。</summary>
        public void Tick(float deltaTime)
        {
            if (!IsActive) return;
            float elapsed = Time.time - _activeStartTime;
            if (_activeExecutor.Tick(_activeDefinition, _context, deltaTime, elapsed))
            {
                StopActive();
            }
        }

        /// <summary>判断指定能力是否已经冷却完成。</summary>
        public bool IsAbilityReady(string abilityId)
        {
            if (string.IsNullOrWhiteSpace(abilityId)) return false;
            return _byId.TryGetValue(abilityId, out var ability)
                && IsCooldownReady(ability);
        }

        /// <summary>判断槽位中是否存在可用能力。</summary>
        public bool IsSlotReady(string slot)
        {
            if (string.IsNullOrWhiteSpace(slot)
                || !_bySlot.TryGetValue(slot, out var list))
            {
                return false;
            }

            foreach (var ability in list)
            {
                if (CanActivate(ability)) return true;
            }
            return false;
        }

        /// <summary>判断槽位中的能力是否都未使用。</summary>
        public bool IsSlotUnused(string slot)
        {
            if (string.IsNullOrWhiteSpace(slot)
                || !_bySlot.TryGetValue(slot, out var list))
            {
                return false;
            }

            foreach (var ability in list)
            {
                if (_usedOnce.Contains(ability.id)) return false;
            }
            return true;
        }
        #endregion

        #region 能力激活与清理 (Ability Activation & Cleanup)
        /// <summary>按能力 ID 尝试激活能力。</summary>
        public bool TryActivateAbility(string abilityId)
        {
            var ability = FindExecutableAbility(abilityId, string.Empty);
            return ability != null && StartAbility(ability);
        }

        /// <summary>在槽位中按优先级激活一个可用能力。</summary>
        public bool TryActivateSlot(string slot)
        {
            var ability = FindExecutableAbility(string.Empty, slot);
            return ability != null && StartAbility(ability);
        }

        /// <summary>查找当前可执行的能力，但不启动它。</summary>
        public AbilityDefinition FindExecutableAbility(string abilityId, string slot)
        {
            if (IsActive) return null;

            if (!string.IsNullOrWhiteSpace(abilityId))
            {
                if (_byId.TryGetValue(abilityId, out var directAbility)
                    && CanActivate(directAbility))
                {
                    return directAbility;
                }
            }

            if (string.IsNullOrWhiteSpace(slot)
                || !_bySlot.TryGetValue(slot, out var abilities))
            {
                return null;
            }

            foreach (var ability in abilities)
            {
                if (CanActivate(ability)) return ability;
            }
            return null;
        }

        /// <summary>启动规则选择器已经解析完成的能力。</summary>
        public bool TryStartResolvedAbility(AbilityDefinition ability)
        {
            if (IsActive || ability == null || string.IsNullOrWhiteSpace(ability.id))
                return false;
            if (!_byId.TryGetValue(ability.id, out var registered)
                || !ReferenceEquals(registered, ability))
            {
                return false;
            }

            return StartAbility(ability);
        }

        /// <summary>中断当前能力并执行清理。</summary>
        public void Abort()
        {
            StopActive();
        }

        /// <summary>创建执行器并启动指定能力。</summary>
        private bool StartAbility(AbilityDefinition ability)
        {
            var executor = AbilityExecutorRegistry.Create(ability.executor);
            if (executor == null)
            {
                Debug.LogError(
                    $"[EnemyAbility] Unknown executor '{ability.executor}' for ability '{ability.id}'.");
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

        /// <summary>检查能力是否满足冷却、次数和距离条件。</summary>
        private bool CanActivate(AbilityDefinition ability)
        {
            if (!IsCooldownReady(ability)) return false;
            if (!AbilityExecutorRegistry.Contains(ability.executor)) return false;
            return IsInRange(ability);
        }

        /// <summary>判断能力冷却和一次性使用限制是否满足。</summary>
        private bool IsCooldownReady(AbilityDefinition ability)
        {
            if (ability.oncePerCombat && _usedOnce.Contains(ability.id)) return false;
            return Time.time - GetLastUsed(ability.id)
                >= Mathf.Max(0f, ability.cooldown);
        }

        /// <summary>判断能力是否处于有效距离。</summary>
        private bool IsInRange(AbilityDefinition ability)
        {
            if (ability.slot == AbilitySlotIds.HitReaction
                || ability.slot == AbilitySlotIds.DeathReaction)
            {
                return true;
            }

            float distance = _context.Blackboard.DistanceToTarget;
            return distance >= ability.minRange && distance <= ability.maxRange;
        }

        /// <summary>获取能力最近一次使用时间。</summary>
        private float GetLastUsed(string abilityId)
        {
            return _lastUsed.TryGetValue(abilityId, out var time) ? time : -999f;
        }

        /// <summary>停止当前能力并清除活动状态。</summary>
        private void StopActive()
        {
            if (_activeDefinition == null) return;
            _activeExecutor.Stop(_activeDefinition, _context);
            _activeDefinition = null;
            _activeExecutor = null;
            _activeStartTime = 0f;
        }
        #endregion
    }
}
