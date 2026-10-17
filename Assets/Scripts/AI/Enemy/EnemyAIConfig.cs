using Emberwild.AI.BT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Emberwild.AI.Enemy
{
    /// <summary>
    /// 怪物 AI 统一配置结构(所有怪共用字段定义)。
    /// 每只怪创建一份独立实例(ShellAI.asset / WolfAI.asset),保存各自参数与决策树。
    /// </summary>
    [CreateAssetMenu(menuName = "AI/EnemyAIConfig", fileName = "EnemyAIConfig")]
    public class EnemyAIConfig : ScriptableObject
    {
        [Header("感知(执行)")]
        public float viewRange = 10f;
        public float loseTargetRange = 13f;
        public float scanInterval = 0.2f;

        [Header("移动(执行)")]
        public float runSpeed = 4f;      // 奔跑速度:既是实际移动速度,也是动画归一化基准
        public float turnSpeed = 10f;
        public float stoppingDistance = 1.2f;

        [Header("执行时长/速度(执行)")]
        public float hitstunDuration = 0.4f;  // 受击硬直时长(RecentlyHit 条件与 HitState 共用,单一来源)
        public float attackDuration = 0.6f;   // 单段攻击硬直时长
        public float rollSpeed = 8f;
        public float rollDuration = 1f;
        public float hideDuration = 10f;

        [Header("决策树(决策参数在节点里)")]
        public List<BTNodeDef> decisionTree = new List<BTNodeDef>();
    }
}
