using System;
using System.Collections.Generic;

namespace Emberwild.AI.Enemy.Ability
{
    /// <summary>
    /// 动画语义键。AI 和配置只使用这些稳定键，不直接依赖具体 Animator 参数名称。
    /// </summary>
    public static class AnimationSemantics
    {
        public const string MoveSpeed = "move.speed";

        public const string Melee = "action.melee";
        public const string Heavy = "action.heavy";
        public const string Pounce = "action.pounce";
        public const string Leap = "action.leap";
        public const string Ranged = "action.ranged";
        public const string Spell = "action.spell";
        public const string Alert = "action.alert";
        public const string TakeDamage = "action.take_damage";

        public const string Roll = "state.roll";
        public const string Guard = "state.guard";
    }

    /// <summary>
    /// 内置能力执行器 ID。配置中的 <c>executor</c> 字段使用这些值选择执行机制。
    /// </summary>
    public static class AbilityExecutorIds
    {
        public const string Attack = "attack";
        public const string RangedAttack = "ranged_attack";
        public const string Dash = "dash";
        public const string DashAttack = "dash_attack";
        public const string Stance = "stance";
        public const string PlayAndWait = "play_and_wait";
        public const string Dead = "dead";
    }

    /// <summary>
    /// 能力槽 ID。行为规则可以通过槽选择能力，从而让不同怪物复用自己的行为模板。
    /// </summary>
    public static class AbilitySlotIds
    {
        public const string PrimaryAttack = "primary_attack";
        public const string HeavyAttack = "heavy_attack";
        public const string GapClose = "gap_close";
        public const string RangedAttack = "ranged_attack";
        public const string Mobility = "mobility";
        public const string Defense = "defense";
        public const string Alert = "alert";
        public const string Special = "special";
        public const string HitReaction = "reaction.hit";
        public const string DeathReaction = "reaction.dead";
    }

    /// <summary>
    /// 行为规则可执行的动作类型。
    /// </summary>
    public static class BehaviorActions
    {
        public const string Ability = "ability";
        public const string Chase = "chase";
        public const string Idle = "idle";
    }

    /// <summary>
    /// 内置行为条件 ID。条件由 <see cref="BehaviorConditionRegistry"/> 求值。
    /// </summary>
    public static class BehaviorConditions
    {
        public const string Always = "always";
        public const string HasTarget = "has_target";
        public const string IsDead = "is_dead";
        public const string RecentlyHit = "recently_hit";
        public const string HpBelow = "hp_below";
        public const string InRange = "in_range";
        public const string AbilityReady = "ability_ready";
        public const string SlotReady = "slot_ready";
        public const string SlotUnused = "slot_unused";
        public const string AttackCooldownReady = "attack_cooldown_ready";
    }

    /// <summary>
    /// Animator 参数绑定类型。
    /// </summary>
    public enum AnimationBindingType
    {
        Trigger,
        Bool,
        Float,
        State
    }

    /// <summary>
    /// 单条动画语义到真实 Animator 参数或状态的映射。
    /// </summary>
    [Serializable]
    public class AnimationBinding
    {
        public string semanticKey;
        public AnimationBindingType bindingType;
        public string parameterName;
    }

    /// <summary>
    /// 一只怪物使用的 Animator 映射表。
    /// </summary>
    [Serializable]
    public class AnimationProfileDefinition
    {
        public string speedParameter = "MoveSpeed";
        public string deadParameter = "IsDead";
        public List<AnimationBinding> bindings = new List<AnimationBinding>();
    }

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

    /// <summary>
    /// 行为规则中的单个条件及其参数。
    /// </summary>
    [Serializable]
    public class BehaviorConditionDefinition
    {
        public string id;
        public string text1;
        public string text2;
        public float p1;
        public float p2;

        public BehaviorConditionDefinition() { }

        /// <summary>创建一个无参数或数值参数条件。</summary>
        public BehaviorConditionDefinition(string id, float p1 = 0f, float p2 = 0f)
        {
            this.id = id;
            this.p1 = p1;
            this.p2 = p2;
        }

        /// <summary>创建一个使用字符串参数的条件，例如能力 ID 或槽 ID。</summary>
        public BehaviorConditionDefinition(string id, string text1)
        {
            this.id = id;
            this.text1 = text1;
        }
    }

    /// <summary>
    /// 一条按优先级排列的行为规则。所有条件成功时执行指定动作。
    /// </summary>
    [Serializable]
    public class BehaviorRuleDefinition
    {
        public string action = BehaviorActions.Idle;
        public string abilityId;
        public string slot;
        public List<BehaviorConditionDefinition> conditions = new List<BehaviorConditionDefinition>();
    }

    /// <summary>
    /// 一只怪物的统一运行时定义，包含感知、移动、动画、能力和行为规则。
    /// </summary>
    [Serializable]
    public class MonsterAIDefinition
    {
        public int TID;
        public string Name;

        public float viewRange = 10f;
        public float loseTargetRange = 13f;
        public float scanInterval = 0.2f;
        public float runSpeed = 4f;
        public float turnSpeed = 10f;
        public float stoppingDistance = 1.2f;
        public float hitStunDuration = 0.4f;

        public AnimationProfileDefinition animation = new AnimationProfileDefinition();
        public List<AbilityDefinition> abilities = new List<AbilityDefinition>();
        public List<BehaviorRuleDefinition> behavior = new List<BehaviorRuleDefinition>();
    }
}
