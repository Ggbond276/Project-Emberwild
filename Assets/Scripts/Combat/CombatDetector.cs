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
        public CombatDetector()
        {
            EventBus.Subscribe<CharacterAttackHitEvent>(OnAttackHitTriggered);
            AppLog.Info("CombatDetector", "CombatDetector 就绪(按阵营判定目标层)");
        }
        public void Init()
        {
            AppLog.Info("CombatDetector", "CombatDetector Init");
        }
        private void OnAttackHitTriggered(CharacterAttackHitEvent evt)
        {
            var attacker = evt.Attacker;
            if (attacker == null) return;

            var attackerAttr = attacker.Attribute; 
            if (attackerAttr == null || attackerAttr.IsDead) return;

            string subType = attacker.SubType;

            if(!DataManager.Instance.ComboGroupDict.TryGetValue(subType, out var comboDict) ||
                !comboDict.TryGetValue(evt.ComboIndex, out var atkDefine))
            {
                AppLog.Error("Combat", $"查表失败: 找不到武器 [{subType}] 的第 [{evt.ComboIndex}] 段攻击配置！");
                return;
            }

            // 计算伤害中心 远程攻击(TargetPosition != zero)以目标点为中心;近战以 forward*OffsetZ 为中
            Vector3 center = evt.TargetPosition != Vector3.zero ? evt.TargetPosition : attacker.Transform.position + attacker.Transform.forward * atkDefine.OffsetZ;

            // 计算攻击目标层级
            int targetMask = GetTargetLayer(attacker.Faction);
            // 获取攻击目标
            Collider[] hits = PerformPhysicsOverlap(center, attacker.Transform, atkDefine, targetMask);
            // 对攻击目标执行伤害
            ProcessHits(hits, attackerAttr, atkDefine, center, attacker.Transform.forward);
        }

        /// <summary>
        /// 根据玩家所在阵营 得到目标攻击层级
        /// </summary>
        /// <param name="faction"></param>
        /// <returns></returns>
        private int GetTargetLayer(FactionType faction)
        {
            return faction == FactionType.Player ? LayerMask.GetMask("Monster") : LayerMask.GetMask("Player");
        }

        /// <summary>
        /// TODO : 这里的技能判定是存在问题的 关于矩形是否应该倾斜配置表应该引入Param3 第三个参数来参与计算
        /// </summary>
        /// <param name="center"></param>
        /// <param name="attackerTransform"></param>
        /// <param name="atkDefine"></param>
        /// <param name="targetLayerMask"></param>
        /// <returns></returns>
        private Collider[] PerformPhysicsOverlap(Vector3 center, Transform attackerTransform, AttackDefine atkDefine, int targetLayerMask)
        {
            switch(atkDefine.HitboxType)
            {
                case 1: // 纯圆形判定
                    return Physics.OverlapSphere(center, atkDefine.Param1, targetLayerMask);
                case 2: // 矩形判定 (计算长宽的一半，根据玩家朝向画矩形)
                    Vector3 halfExtents = new Vector3(atkDefine.Param1 / 2f, 1f, atkDefine.Param2 / 2f);
                    Quaternion tiltedRotation = attackerTransform.rotation * Quaternion.Euler(0, 0, -20f);
                    return Physics.OverlapBox(center, halfExtents, tiltedRotation, targetLayerMask);
                case 3: // 扇形判定
                    return Physics.OverlapSphere(center, atkDefine.Param1, targetLayerMask);
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

                // 计算攻击来向 从受害者指向命中点， 压平Y用于水平击退
                Vector3 fromDir = center - defenderAttr.transform.position;
                fromDir.y = 0f;
                if (fromDir.sqrMagnitude > 0.0001) fromDir.Normalize();

                defenderAttr.TakeDamage(finalDamage, center, fromDir);
            }
        }



    }
}
