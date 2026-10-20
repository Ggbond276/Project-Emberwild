using Emberwild.Core.AppLog;
using Emberwild.Core.Events;
using UnityEngine;

namespace Emberwild.AI.Enemy
{
    /// <summary>
    /// 怪物攻击事件发布层。命中时机由能力执行器决定，本组件只负责把攻击段发送给战斗系统。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyEntity))]
    public class EnemyCombat : MonoBehaviour
    {
        private EnemyEntity _entity;

        /// <summary>在组件启动时自动解析本地怪物实体。</summary>
        private void Awake()
        {
            _entity = GetComponent<EnemyEntity>();
        }

        /// <summary>
        /// 发布一次已经计算好时机的攻击事件。
        /// </summary>
        /// <param name="comboIndex">AttackDefine 中的攻击段号，小于 0 表示该能力不产生伤害。</param>
        /// <param name="ranged">是否为远程攻击。</param>
        /// <param name="targetPosition">远程攻击冻结的目标位置。</param>
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
    }
}
