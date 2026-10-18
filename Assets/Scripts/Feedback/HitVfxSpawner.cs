using Emberwild.Core.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Emberwild.Feedback
{
    /// <summary>
    /// 打击特效:在命中点生成粒子。挂 GameFeel。
    /// </summary>
    public class HitVfxSpawner : MonoBehaviour
    {
        [Tooltip("命中粒子预制体")] 
        [SerializeField] private GameObject _hitEffectPrefab;

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

        private void OnEnemyHit(EnemyDamagedEvent evt) => Spawn(evt.HitPoint, evt.FromDirection);
        private void OnPlayerHit(CharacterDamagedEvent evt) => Spawn(evt.HitPoint, evt.FromDirection);

        private void Spawn(Vector3 position, Vector3 direction)
        {
            if (_hitEffectPrefab == null) return;
            // 朝向攻击来向,让火花喷向被打的方向
            Quaternion rot = direction.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(direction, Vector3.up)
                : Quaternion.identity;
            var go = Instantiate(_hitEffectPrefab, position, rot);
            Destroy(go, 0.2f); // 兜底回收(粒子本身也会自动结束)
        }
    }
}
