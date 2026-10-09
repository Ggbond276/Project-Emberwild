using Emberwild.Core.AppLog;
using Emberwild.Core.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Emberwild.Character
{
    public class Health : MonoBehaviour
    {
        [Header("血量")]
        [Tooltip("最大血量")]
        [SerializeField] private float _maxHp = 100f;

        private float _currentHp;
        private bool _isDead;

        /// <summary>
        /// 当前血量
        /// </summary>
        public float CurrentHp => _currentHp;
        /// <summary>
        /// 是否已死亡
        /// </summary>
        public bool IsDead => _isDead;


        private void Start()
        {
            _currentHp = _maxHp;
            _isDead = false;
            AppLog.Info("Player", $"Health initialized: {_currentHp}/{_maxHp}");
        }

        /// <summary>
        /// 受到伤害的入口 由事件中心调用
        /// </summary>
        /// <param name="damage">收到的伤害量</param>
        /// <param name="fromDirection">受到伤害的受击方向</param>
        public void TakeDamage(float damage, Vector3 fromDirection)
        {
            if (_isDead) return;
            if (damage <= 0f) return;

            _currentHp = Mathf.Max(0f, _currentHp - damage);
            EventBus.Publish(new PlayerDamagedEvent(damage, _currentHp, fromDirection));

            AppLog.Info("Player", $"Damaged -{damage:F1} HP={_currentHp:F1}/{_maxHp:F1}");

            if(_currentHp <= 0f)
            {
                _isDead = true;
                EventBus.Publish(new PlayerDiedEvent());
                AppLog.Info("Player", "Player died");
            }
        }


    }
}
