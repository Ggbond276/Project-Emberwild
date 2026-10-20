using System;

namespace Emberwild.AI.Enemy.Ability
{
    /// <summary>
    /// 一次可执行能力的完整数据。执行器根据字段决定移动、命中、动画和冷却。
    /// </summary>
    [Serializable]
    public class AbilityDefinition
    {
        public string id;
        public string slot;
        public string executor = AbilityExecutorIds.Attack;
        public string animationAction;
        public string animationState;
        public float duration = 0.6f;
        public float cooldown;
        public int damageComboIndex = -1;
        public bool ranged;
        public bool countsAsAttack = true;
        public float minRange;
        public float maxRange = float.MaxValue;
        public float speed;
        public float hitDelay;
        public bool lockMovement = true;
        public bool stopMovementAtEnd = true;
        public bool oncePerCombat;
        public float damageTakenMultiplier = 1f;
        public int priority;
    }
}
