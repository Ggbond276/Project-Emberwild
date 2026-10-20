using Emberwild.AI.BT;
using Emberwild.AI.Enemy.EAnimator;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Emberwild.AI.Enemy
{
    /// <summary>动作 → 攻击表段号的映射项。</summary>
    [Serializable]
    public class AttackActionEntry
    {
        [Tooltip("动作枚举")]
        public EnemyAction action;

        [Tooltip("AttackDefine 里该怪 ComboGroup 下的 ComboIndex")]
        public int comboIndex;
    }

    /// <summary>
    /// 怪物 AI 配置。每只怪一份实例(ShellAI / WerewolfAI)。
    /// </summary>
    [CreateAssetMenu(menuName = "AI/EnemyAIConfig", fileName = "EnemyAIConfig")]
    public class EnemyAIConfig : ScriptableObject
    {
        [Header("感知(执行)")]
        public float viewRange = 10f;
        public float loseTargetRange = 13f;
        public float scanInterval = 0.2f;

        [Header("移动(执行)")]
        public float runSpeed = 4f;
        public float turnSpeed = 10f;
        public float stoppingDistance = 1.2f;

        [Header("执行时长/速度(执行)")]
        public float hitstunDuration = 0.4f;
        public float attackDuration = 0.6f;
        public float rollSpeed = 8f;
        public float rollDuration = 1f;
        public float hideDuration = 10f;

        [Header("Werewolf")]
        public float heavyAttackDuration = 0.9f;
        public float pounceSpeed = 11f;
        public float pounceDuration = 0.35f;
        public float leapSpeed = 8f;
        public float leapDuration = 0.6f;
        public float spellDuration = 1.2f;
        public float alertDuration = 0.8f;

        [Header("招式 → 攻击表段号")]
        [Tooltip("决定每个动作去 AttackDefine 查哪一段。未配置的动作会用段 0 并打警告")]
        public List<AttackActionEntry> attackActions = new List<AttackActionEntry>();

        [Header("决策树(决策参数在节点里)")]
        public List<BTNodeDef> decisionTree = new List<BTNodeDef>();

        /// <summary>取某动作对应的攻击表段号，未配置返回 -1。</summary>
        public int GetComboIndex(EnemyAction action)
        {
            for (int i = 0; i < attackActions.Count; i++)
            {
                if (attackActions[i] != null && attackActions[i].action == action)
                    return attackActions[i].comboIndex;
            }
            return -1;
        }
    }
}
