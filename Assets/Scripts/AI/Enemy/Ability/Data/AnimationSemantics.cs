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
}
