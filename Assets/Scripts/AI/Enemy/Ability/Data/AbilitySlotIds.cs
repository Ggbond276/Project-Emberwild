namespace Emberwild.AI.Enemy.Ability
{
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
}
