using System.Collections.Generic;
using Emberwild.Core.AppLog;
using Emberwild.Core.Events;
using UnityEngine;

namespace Emberwild.Character
{
    /// <summary>
    /// 角色状态协调器 — 唯一提供"角色现在能否 X"的状态查询入口。
    ///
    /// 职责:
    /// - 维护 CanMove / CanAttack / CanJump / CanDodge 四个布尔查询入口。
    /// - 订阅攻击/受击/死亡/复活事件,自动翻转状态。
    /// - 提供 LockMovement / UnlockMovement 手动 API(给未来闪避等系统使用)。
    ///
    /// 严格不做:
    /// - 写 Animator / Transform / Rigidbody。
    /// - 直接订阅 InputReader。
    /// - 主动发 EventBus 事件。
    /// - 持有 CharacterCombat / CharacterMovementController 引用。
    /// </summary>
    [DisallowMultipleComponent]
    public class CharacterStateController : MonoBehaviour
    {
        [Header("受击硬直")]
        [Tooltip("受击后移动锁时长(秒)")]
        [SerializeField] private float _hitstunDuration = 0.3f;

        // 时间到期的软锁条目(攻击/受击使用)。
        private struct LockEntry
        {
            public float Deadline;
        }

        private readonly List<LockEntry> _lockEntries = new List<LockEntry>(4);
        // 手动锁的引用计数。
        private int _movementLockCount;
        // 死亡硬锁(只能由 CharacterHealthResetEvent 清除)。
        private bool _isDead;

        public bool CanMove => !_isDead && _movementLockCount == 0 && _lockEntries.Count == 0;
        public bool CanAttack => !_isDead;
        public bool CanJump => !_isDead && _movementLockCount == 0 && _lockEntries.Count == 0;
        public bool CanDodge => !_isDead && _movementLockCount == 0 && _lockEntries.Count == 0;
        public bool IsDead => _isDead;

        private void OnEnable()
        {
            EventBus.Subscribe<CharacterAttackStartedEvent>(OnAttackStarted);
            EventBus.Subscribe<CharacterDamagedEvent>(OnDamaged);
            EventBus.Subscribe<CharacterDiedEvent>(OnDied);
            EventBus.Subscribe<CharacterHealthResetEvent>(OnHealthReset);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<CharacterAttackStartedEvent>(OnAttackStarted);
            EventBus.Unsubscribe<CharacterDamagedEvent>(OnDamaged);
            EventBus.Unsubscribe<CharacterDiedEvent>(OnDied);
            EventBus.Unsubscribe<CharacterHealthResetEvent>(OnHealthReset);
        }

        private void Update()
        {
            // 扫描到期软锁,自动清除。不调用 UnlockMovement,避免与手动引用计数混淆。
            if (_lockEntries.Count == 0) return;
            for (int i = _lockEntries.Count - 1; i >= 0; i--)
            {
                if (Time.time >= _lockEntries[i].Deadline)
                {
                    _lockEntries.RemoveAt(i);
                }
            }
        }

        private void OnAttackStarted(CharacterAttackStartedEvent evt)
        {
            _lockEntries.Add(new LockEntry { Deadline = Time.time + evt.LockDuration });
            AppLog.Info("State", $"Attack started, lock movement for {evt.LockDuration:F2}s");
        }

        private void OnDamaged(CharacterDamagedEvent evt)
        {
            _lockEntries.Add(new LockEntry { Deadline = Time.time + _hitstunDuration });
            AppLog.Info("State", $"Damaged, lock movement for {_hitstunDuration:F2}s");
        }

        private void OnDied(CharacterDiedEvent evt)
        {
            _isDead = true;
            _lockEntries.Clear();
            _movementLockCount = 0;
            AppLog.Info("State", "Character died, all locks engaged");
        }

        private void OnHealthReset(CharacterHealthResetEvent evt)
        {
            _isDead = false;
            _lockEntries.Clear();
            _movementLockCount = 0;
            AppLog.Info("State", "Character health reset, all locks cleared");
        }

        /// <summary>手动锁定移动(给未来闪避/特殊状态使用)。</summary>
        public void LockMovement()
        {
            _movementLockCount++;
        }

        /// <summary>手动解锁移动。仅当存在对应 LockMovement 时才生效。</summary>
        public void UnlockMovement()
        {
            if (_movementLockCount > 0) _movementLockCount--;
        }
    }
}
