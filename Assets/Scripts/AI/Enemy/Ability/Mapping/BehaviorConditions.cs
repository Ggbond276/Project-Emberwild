namespace Emberwild.AI.Enemy.Ability
{
    /// <summary>
    /// 行为条件常量
    /// </summary>
    public static class BehaviorConditions
    {
        public const string Always = "always";
        public const string HasTarget = "has_target";
        public const string IsDead = "is_dead";
        public const string RecentlyHit = "recently_hit";
        public const string HpBelow = "hp_below";
        public const string AttackCooldownReady = "attack_cooldown_ready";
    }
}
