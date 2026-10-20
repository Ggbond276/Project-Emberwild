using Emberwild.AI.Enemy.EAnimator;
using Emberwild.AI.Pathfinding;
using Emberwild.Combat;
using UnityEngine;

namespace Emberwild.AI.Enemy.Ability
{
    /// <summary>
    /// 能力执行器可以使用的最小运行时上下文。
    /// 执行器通过它访问移动、战斗、动画和黑板，而不依赖完整的 EnemyBrain。
    /// </summary>
    public interface IEnemyAbilityContext
    {
        Transform Transform { get; }
        EnemyBlackboard Blackboard { get; }
        CustomNavAgent Agent { get; }
        IEnemyAnimator Animator { get; }
        EnemyCombat Combat { get; }
        CombatAttribute Attribute { get; }
    }
}
