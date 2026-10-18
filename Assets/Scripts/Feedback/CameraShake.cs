using Emberwild.Core.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Emberwild.Feedback
{
    /// <summary>震屏:命中时镜头衰减抖动。挂 Main Camera,执行顺序放在 CameraController 之后。</summary>
    [DefaultExecutionOrder(100)]
    public class CameraShake : MonoBehaviour
    {
        [Tooltip("最大位移偏移(米)")]
        [SerializeField] private float _maxOffset = 0.2f;

        [Tooltip("最大滚转角度(度)")]
        [SerializeField] private float _maxAngle = 2f;

        [Tooltip("每秒衰减量(越大越快停)")]
        [SerializeField] private float _decay = 1.5f;

        private float _trauma;

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

        private void OnEnemyHit(EnemyDamagedEvent evt) => AddTrauma(0.5f);
        private void OnPlayerHit(CharacterDamagedEvent evt) => AddTrauma(0.4f);
        public void AddTrauma(float amount) => _trauma = Mathf.Clamp01(_trauma + amount);

        private void LateUpdate()
        {
            if (_trauma <= 0.0001f) { _trauma = 0f; return; }

            // 用 unscaledDeltaTime:顿帧期间震动也要继续
            _trauma = Mathf.Max(0f, _trauma - _decay * Time.unscaledDeltaTime);
            float shake = _trauma * _trauma; // 平方让衰减更"戛然而止"

            float t = Time.unscaledTime * 20f; // 20 是噪声频率
            Vector3 offset = new Vector3(
                (Mathf.PerlinNoise(0f, t) - 0.5f) * 2f * _maxOffset * shake,
                (Mathf.PerlinNoise(100f, t) - 0.5f) * 2f * _maxOffset * shake,
                0f);
            float roll = (Mathf.PerlinNoise(200f, t) - 0.5f) * 2f * _maxAngle * shake;

            // 叠加在 CameraController 的跟随结果上(本组件执行顺序在其后)
            transform.position += offset;
            transform.rotation *= Quaternion.Euler(0f, 0f, roll);
        }

    }
}
