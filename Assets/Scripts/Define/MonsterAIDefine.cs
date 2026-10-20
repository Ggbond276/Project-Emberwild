using System;

namespace Emberwild.Define
{
    /// <summary>怪物核心表和 Animator 根参数。</summary>
    [Serializable]
    public class MonsterAICoreDefine
    {
        public int TID;
        public string Name;
        public float viewRange;
        public float loseTargetRange;
        public float scanInterval;
        public float runSpeed;
        public float turnSpeed;
        public float stoppingDistance;
        public float hitStunDuration;
        public string speedParameter;
        public string deadParameter;
    }

    /// <summary>一只怪物的一项能力配置。</summary>
    [Serializable]
    public class MonsterAbilityDefine
    {
        public int TID;
        public string AbilityId;
        public string Slot;
        public string Executor;
        public string AnimationAction;
        public string AnimationState;
        public float Duration;
        public float Cooldown;
        public int DamageComboIndex = -1;
        public bool Ranged;
        public float MinRange;
        public float MaxRange = 999999f;
        public float Speed;
        public float HitDelay;
        public bool LockMovement = true;
        public bool StopMovementAtEnd = true;
        public bool OncePerCombat;
        public float DamageTakenMultiplier = 1f;
        public bool CountsAsAttack = true;
        public int Priority;
    }

    /// <summary>一条按 RuleIndex 排序的行为规则。</summary>
    [Serializable]
    public class MonsterBehaviorDefine
    {
        public int TID;
        public int RuleIndex;
        public string Action;
        public string AbilityId;
        public string Slot;
    }

    /// <summary>行为规则中的一个条件，通过 TID 和 RuleIndex 关联规则。</summary>
    [Serializable]
    public class MonsterBehaviorConditionDefine
    {
        public int TID;
        public int RuleIndex;
        public int ConditionIndex;
        public string ConditionId;
        public string Text1;
        public string Text2;
        public float P1;
        public float P2;
    }

    /// <summary>动画语义到真实 Animator 参数的一条绑定。</summary>
    [Serializable]
    public class MonsterAnimationBindingDefine
    {
        public int TID;
        public int BindingIndex;
        public string SemanticKey;
        public string BindingType;
        public string ParameterName;
    }
}
