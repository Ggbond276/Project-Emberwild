using System;

namespace Emberwild.AI.Enemy.Ability
{
    [Serializable]
    public class AbilityDefinition
    {
        #region 能力配置数据 (Ability Data)
        // 能力身份与执行配置
        public string id;
        public string slot;
        public string executor = AbilityExecutorIds.Attack;
        public string animationAction;
        public string animationState;

        // 时间、伤害与距离配置
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
        #endregion
    }
}
