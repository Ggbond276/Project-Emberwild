using Emberwild.Core.AppLog;
using Emberwild.Core.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Emberwild.AI
{
    [DisallowMultipleComponent]
    public class EnemyBase : MonoBehaviour
    {
        [Header("血量")]
        [Tooltip("最大血量")]
        [SerializeField] private float _maxHp = 10f;

        private float _currentHp;
        private bool _isDead;

        public float CurrentHp => _currentHp;
        public float MaxHp => _maxHp;
        public bool IsDead => _isDead;


        private void Start()
        {
            _currentHp = _maxHp;
            _isDead = false;
            AppLog.Info("AI", $"EnemyBase initialized: {_currentHp}/{_maxHp}");
        }

        /// <summary>
        /// 收到伤害的入口
        /// </summary>
        /// <param name="damage"></param>
        /// <param name="fromDirection"></param>
        public void TakeDamage(float damage, Vector3 fromDirection)
        {
            if (_isDead) return;
            if (damage <= 0f) return;

            _currentHp = Mathf.Max(0f, _currentHp - damage);

            EventBus.Publish(new EnemyDamagedEvent(damage, _currentHp, fromDirection));

            AppLog.Info("AI", $"Damaged -{damage:F1} HP={_currentHp:F1}/{_maxHp:F1}");

            if(_currentHp <= 0f)
            {
                _isDead = true;
                EventBus.Publish(new EnemyDiedEvent());
                AppLog.Info("AI", "Enemy died");
            }
        }
    }
}
