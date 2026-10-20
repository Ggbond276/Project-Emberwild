namespace Emberwild.AI.Enemy.Ability
{
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
}
