using Emberwild.Core.Events;
using Emberwild.Core.Singleton;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Emberwild.Feedback
{
    /// <summary>
    /// 顿帧：命中瞬间全局时间短暂冻结，继承项目已有的MonoSingleton，挂在GameFeel物体上
    /// </summary>
    public class HitStopController : MonoSingleton<HitStopController>
    {
        [Tooltip("顿帧时长(秒)")]
        [SerializeField] private float _duration = 0.09f;

        [Tooltip("顿帧期间的时间倍率(0 = 完全冻结)")]
        [SerializeField, Range(0f, 1f)] private float _timeScale = 0f;

        private bool _active;
        private float _endTime;
        private float _restoreScale = 1f;

        private void OnEnable()
        {
            EventBus.Subscribe<EnemyDamagedEvent>(OnEnemyHit);
            EventBus.Subscribe<CharacterDamagedEvent>(OnPlayerHit);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyDamagedEvent>(OnEnemyHit);
            EventBus.Unsubscribe<CharacterDamagedEvent>(OnPlayerHit);
        }

        private void OnEnemyHit(EnemyDamagedEvent evt) => Trigger();
        private void OnPlayerHit(CharacterDamagedEvent evt) => Trigger();

        public void Trigger()
        {
            if (!_active)
            {
                _restoreScale = Time.timeScale;   // 记住进入前的倍率，不污染其他时间系统
                _active = true;
            }

            // 连续命中取最晚结束时间，而不是互相覆盖
            _endTime = Mathf.Max(_endTime, Time.unscaledTime + _duration);
            Time.timeScale = _timeScale;
        }

        public void Update()
        {
            if (!_active) return;
            if (Time.unscaledTime < _endTime) return;

            Time.timeScale = _restoreScale;
            _active = false;
        }
    }
}
