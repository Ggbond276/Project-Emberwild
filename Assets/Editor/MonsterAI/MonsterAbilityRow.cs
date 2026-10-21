using Newtonsoft.Json;

namespace Emberwild.Editor.MonsterAI
{
    public class MonsterAbilityRow : MonsterAIEditableRow
    {
        #region 能力数据 (Ability Data)
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

        [JsonIgnore]
        public bool IsExpanded = true;
        #endregion
    }
}
