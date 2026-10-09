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
        [SerializeField] private Animator _animator;

        [SerializeField] private string _getHitTrigger = "GetHit";
        [SerializeField] private string _dieTrigger = "Die";

        private bool _isDead = false;
        private int _getHitHash;
        private int _dieHash;

        private void Start()
        {
            if(_animator == null)
            {
                _animator = GetComponent<Animator>();
                if(_animator == null)
                {
                    Debug.LogWarning("[PlayerFeedback] Animator not found on this GameObject.");
                }
            }

            // 缓存 Trigger 参数 hash
            _getHitHash = Animator.StringToHash(_getHitTrigger);
            _dieHash = Animator.StringToHash(_dieTrigger);
        }

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
            _animator.SetTrigger(_getHitHash);
        }

        private void OnDied(PlayerDiedEvent evt)
        {
            _isDead = true;
            if (_animator == null) return;
            _animator.SetTrigger(_dieHash);
        }

        private void OnReset(PlayerHealthResetEvent evt)
        {
            _isDead = false;
            if (_animator == null) return;
            _animator.ResetTrigger(_dieHash);
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
