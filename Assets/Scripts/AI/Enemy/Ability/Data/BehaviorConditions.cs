namespace Emberwild.AI.Enemy.Ability
{
    public static class BehaviorConditions
    {
        #region 行为条件常量 (Behavior Conditions)
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
        #endregion
    }
}
