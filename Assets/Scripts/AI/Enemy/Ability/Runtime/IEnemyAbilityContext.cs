using Emberwild.AI.Enemy.EAnimator;
using Emberwild.AI.Pathfinding;
using Emberwild.Combat;
using UnityEngine;

namespace Emberwild.AI.Enemy.Ability
{
    public interface IEnemyAbilityContext
    {
        #region 能力上下文属性 (Ability Context)
        Transform Transform { get; }
        EnemyBlackboard Blackboard { get; }
        CustomNavAgent Agent { get; }
        IEnemyAnimator Animator { get; }
        EnemyCombat Combat { get; }
        CombatAttribute Attribute { get; }
        #endregion
    }
}
