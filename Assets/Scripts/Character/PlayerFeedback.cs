using Emberwild.Core.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Emberwild.Character
{
    /// <summary>
    /// 角色收击反馈
    /// </summary>
    public class PlayerFeedback : MonoBehaviour
    {
        [SerializeField] private CharacterAnimator _animator;

        private bool _isDead = false;

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerDamagedEvent>(OnDamaged);
            EventBus.Subscribe<PlayerDiedEvent>(OnDied);
            EventBus.Subscribe<PlayerHealthResetEvent>(OnReset);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerDamagedEvent>(OnDamaged);
            EventBus.Unsubscribe<PlayerDiedEvent>(OnDied);
            EventBus.Unsubscribe<PlayerHealthResetEvent>(OnReset);
        }

        private void OnDamaged(PlayerDamagedEvent evt)
        {
            // 死了不再响应普通受伤
            if (_isDead) return;
            if (_animator == null) return;
            _animator.PlayGetHit();
        }

        private void OnDied(PlayerDiedEvent evt)
        {
            _isDead = true;
            if (_animator == null) return;
            _animator.PlayDie();
        }

        private void OnReset(PlayerHealthResetEvent evt)
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
