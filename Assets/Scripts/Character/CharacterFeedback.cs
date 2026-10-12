using Emberwild.Core.AppLog;
using Emberwild.Core.Events;
using UnityEngine;

namespace Emberwild.Character
{
    /// <summary>
    /// 角色受击反馈组件 — 订阅受伤/死亡/复活事件,驱动受击与死亡动画。
    ///
    /// 职责:
    /// - 订阅 CharacterDamagedEvent 播放受击动画。
    /// - 订阅 CharacterDiedEvent 播放死亡动画。
    /// - 订阅 CharacterHealthResetEvent 复位死亡标志与死亡动画。
    ///
    /// 严格不做:
    /// - 移动 / 攻击 / 跳跃。
    /// - HP 计算(由 CharacterDataManager 单点处理)。
    /// - 移动锁(由 CharacterStateController 自动处理)。
    /// </summary>
    [DisallowMultipleComponent]
    public class CharacterFeedback : MonoBehaviour
    {
        [SerializeField] private CharacterAnimator _animator;

        private bool _isDead = false;

        public bool IsDead => _isDead;

        private void OnEnable()
        {
            EventBus.Subscribe<CharacterDamagedEvent>(OnDamaged);
            EventBus.Subscribe<CharacterDiedEvent>(OnDied);
            EventBus.Subscribe<CharacterHealthResetEvent>(OnReset);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<CharacterDamagedEvent>(OnDamaged);
            EventBus.Unsubscribe<CharacterDiedEvent>(OnDied);
            EventBus.Unsubscribe<CharacterHealthResetEvent>(OnReset);
        }

        private void OnDamaged(CharacterDamagedEvent evt)
        {
            // 死了不再响应普通受伤
            if (_isDead) return;
            if (_animator == null) return;
            _animator.PlayGetHit();
        }

        private void OnDied(CharacterDiedEvent evt)
        {
            _isDead = true;
            if (_animator == null) return;
            _animator.PlayDie();
        }

        private void OnReset(CharacterHealthResetEvent evt)
        {
            _isDead = false;
            if (_animator == null) return;
            _animator.ResetDie();
        }

        /// <summary>
        /// 重置反馈状态。供复活系统调用。
        /// </summary>
        public void ResetFeedback()
        {
            _isDead = false;
        }
    }
}
