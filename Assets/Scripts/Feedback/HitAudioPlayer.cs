using Emberwild.Core.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Emberwild.Feedback
{
    /// <summary>命中音效。挂 GameFeel,自带 AudioSource。</summary>
    [RequireComponent(typeof(AudioSource))]
    public class HitAudioPlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource _source;
        [Tooltip("打到怪的音效")] [SerializeField] private AudioClip _hitClip;
        [Tooltip("自己被打的音效")] [SerializeField] private AudioClip _hurtClip;

        private void Awake()
        {
            if (_source == null) _source = GetComponent<AudioSource>();
        }

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

        private void OnEnemyHit(EnemyDamagedEvent evt) => Play(_hitClip);
        private void OnPlayerHit(CharacterDamagedEvent evt) => Play(_hurtClip);

        private void Play(AudioClip clip)
        {
            if (clip == null || _source == null) return;
            _source.pitch = UnityEngine.Random.Range(0.9f, 1.1f);   // 随机音高,减少重复感
            _source.PlayOneShot(clip);
        }

    }
}
