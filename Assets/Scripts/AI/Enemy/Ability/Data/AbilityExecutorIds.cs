namespace Emberwild.AI.Enemy.Ability
{
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
}
