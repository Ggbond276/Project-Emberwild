using Emberwild.Core.AppLog;
using Emberwild.Core.Events;
using UnityEngine;

namespace Emberwild.AI.Enemy
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyEntity))]
    public class EnemyCombat : MonoBehaviour
    {
        #region 本地核心组件 (Local Components)
        private EnemyEntity _entity;
        #endregion

        #region Unity 生命周期与装配 (Lifecycle & Setup)
        /// <summary>获取怪物身上的 EnemyEntity。</summary>
        private void Awake()
        {
            _entity = GetComponent<EnemyEntity>();
        }
        #endregion

        #region 攻击事件发布 (Attack Publishing)
        /// <summary>发布一次怪物攻击命中事件。</summary>
        public void PublishAttack(int comboIndex, bool ranged, Vector3 targetPosition)
        {
            if (comboIndex < 0)
            {
                AppLog.Warning("Combat", $"{name}: ability has no damage combo index.");
                return;
            }

            EventBus.Publish(new CharacterAttackHitEvent(
                _entity,
                comboIndex,
                ranged ? targetPosition : default));
            AppLog.Info("Combat", $"{name}: hit event combo={comboIndex} ranged={ranged}");
        }
        #endregion
    }
}
