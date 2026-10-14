using Emberwild.Core.AppLog;
using Emberwild.Core.Events;
using Emberwild.Core.Singleton;
using Emberwild.Define;
using Emberwild.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Emberwild.Combat
{
    public class CombatDetector : Singleton<CombatDetector>
    {
        private readonly int _targetLayerMask;

        public CombatDetector()
        {
            _targetLayerMask = LayerMask.GetMask("Monster");
            EventBus.Subscribe<CharacterAttackHitEvent>(OnAttackHitTriggered);
            AppLog.Info("CombatDetector", $"{_targetLayerMask}");
        }
        public void Init()
        {
            AppLog.Info("CombatDetector", "CombatDetector Init");
        }
        private void OnAttackHitTriggered(CharacterAttackHitEvent evt)
        {
            var attackerEntity = evt.Attacker;
            if (attackerEntity == null) return;

            var comboIndex = evt.ComboIndex;

            var attackerAttr = attackerEntity.GetComponent<CombatAttribute>();
            if (attackerAttr == null || attackerAttr.IsDead) return;

            string subType = attackerEntity.SubType;

            if(!DataManager.Instance.ComboGroupDict.TryGetValue(subType, out var comboDict) ||
                !comboDict.TryGetValue(comboIndex, out var atkDefine))
            {
                AppLog.Error("Combat", $"查表失败: 找不到武器 [{subType}] 的第 [{evt.ComboIndex}] 段攻击配置！");
                return;
            }

            Vector3 center = attackerEntity.transform.position + attackerEntity.transform.forward * atkDefine.OffsetZ;
            Collider[] hits = PerformPhysicsOverlap(center, attackerEntity.transform, atkDefine);
            ProcessHits(hits, attackerAttr, atkDefine, center, attackerEntity.transform.forward);
        }

        private Collider[] PerformPhysicsOverlap(Vector3 center, Transform attackerTransform, AttackDefine atkDefine)
        {
            switch(atkDefine.HitboxType)
            {
                case 1: // 纯圆形判定
                    return Physics.OverlapSphere(center, atkDefine.Param1, _targetLayerMask);
                case 2: // 矩形判定 (计算长宽的一半，根据玩家朝向画矩形)
                    Vector3 halfExtents = new Vector3(atkDefine.Param1 / 2f, 1f, atkDefine.Param2 / 2f);
                    Quaternion tiltedRotation = attackerTransform.rotation * Quaternion.Euler(0, 0, -20f);
                    return Physics.OverlapBox(center, halfExtents, tiltedRotation, _targetLayerMask);
                case 3: // 扇形判定
                    return Physics.OverlapSphere(center, atkDefine.Param1, _targetLayerMask);
                default:
                    AppLog.Error("Combat", $"未知的 HitboxType: {atkDefine.HitboxType}");
                    return new Collider[0];
            }
        }

        /// <summary>
        /// 执行攻击
        /// </summary>
        /// <param name="hits"></param>
        /// <param name="attackerAttr"></param>
        /// <param name="atkDefine"></param>
        /// <param name="center"></param>
        /// <param name="forwardDir"></param>
        private void ProcessHits(Collider[] hits, CombatAttribute attackerAttr, AttackDefine atkDefine, Vector3 center, Vector3 forwardDir)
        {
            foreach(var hitCol in hits)
            {
                var defenderAttr = hitCol.GetComponent<CombatAttribute>();

                if (defenderAttr == null || defenderAttr.IsDead || defenderAttr == attackerAttr) continue;

                if(atkDefine.HitboxType == 3)
                {
                    Vector3 dirToTarget = (defenderAttr.transform.position - center).normalized;
                    float angle = Vector3.Angle(forwardDir, dirToTarget);
                    // 敌我夹角 > 扇形总角度的一半，说明敌人在扇形外面，躲过一劫
                    if (angle > atkDefine.Param2 / 2f)
                        continue;
                }

                float finalDamage = DamageCalculator.Calculate(attackerAttr, defenderAttr, atkDefine.DmgRate);
                defenderAttr.TakeDamage(finalDamage);
            }
        }
    }
}
