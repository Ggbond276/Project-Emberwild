using Emberwild.Character;
using Emberwild.Core.AppLog;
using Emberwild.Core.Events;
using Emberwild.Core.Singleton;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Emberwild.Data
{
    public class PlayerDataManager : Singleton<PlayerDataManager>
    {
        private CharacterData _characterData;
        
        private bool _initialized = false;

        private const string LOG_TAG = "Player";

        public PlayerDataManager() {
            Initialize();
        }

        
        private void Initialize() {
            if(_initialized) return;

            if(_characterData == null) {
                _characterData = new CharacterData();
            }

            if(_characterData.MaxHp <= 0f)
            {
                _characterData.MaxHp = 100f;
            }
            if (_characterData.CurrentHp <= 0f || _characterData.CurrentHp > _characterData.MaxHp)
            {
                _characterData.CurrentHp = _characterData.MaxHp;
            }
            _characterData.IsDead = false;

            _initialized = true;

            AppLog.Info(LOG_TAG, $"PlayerDataManager initialized: {_characterData.CurrentHp}/{_characterData.MaxHp}");
        }


        public float CurrentHp => _characterData.CurrentHp;
        public float MaxHp => _characterData.MaxHp;
        public bool IsDead => _characterData.IsDead;

        public void TakeDamage(float damage, Vector3 fromDirection)
        {
            if (!_initialized) Initialize();
            // 死亡后不再受伤
            if (_characterData.IsDead) return;

            if (damage <= 0f) return;

            _characterData.CurrentHp = Mathf.Max(0f, _characterData.CurrentHp - damage);

            if (_characterData.CurrentHp <= 0f)
            {
                _characterData.IsDead = true;
                EventBus.Publish(new PlayerDiedEvent());
                AppLog.Info(LOG_TAG, $"Player died (damage={damage:F1})");
            }
            else
            {
                EventBus.Publish(new PlayerDamagedEvent(damage, _characterData.CurrentHp, fromDirection));
                AppLog.Info(LOG_TAG, $"Damaged -{damage:F1} HP={_characterData.CurrentHp:F1}/{_characterData.MaxHp:F1}");
            }
        }

        /// <summary>
        /// 重置为满血状态。供测试/复活用。
        /// </summary>
        public void ResetHealth()
        {
            if (!_initialized) Initialize();
            _characterData.ResetToFull();
            EventBus.Publish(new PlayerHealthResetEvent());
            AppLog.Info(LOG_TAG, $"Health reset: {_characterData.CurrentHp}/{_characterData.MaxHp}");
        }

        /// <summary>
        /// 供外部获取完整数据快照(存档时调用)。
        /// </summary>
        public CharacterData GetData() => _characterData;

        /// <summary>
        /// 加载数据快照(读档时调用)。暂时不知道这个有啥用处
        /// </summary>
        public void LoadData(CharacterData data)
        {
            if (data == null)
            {
                AppLog.Warning(LOG_TAG, "LoadData: data is null, ignored");
                return;
            }
            _characterData = data;
            _initialized = true;
            AppLog.Info(LOG_TAG, $"Data loaded: HP={_characterData.CurrentHp}/{_characterData.MaxHp}, IsDead={_characterData.IsDead}");
        }
    }
}
