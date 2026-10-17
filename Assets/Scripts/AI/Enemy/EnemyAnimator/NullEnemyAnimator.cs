namespace Emberwild.AI.Enemy.EnemyAnimator
{
    /// <summary>
    /// 空动画实现：当怪物未挂任何 IEnemyAnimator 实现(如 ShellAnimator)时兜底，
    /// 避免状态类调用 brain.Animator.Xxx() 时 NRE。所有方法空实现，怪物照常移动/战斗，只是不播动画。
    /// </summary>
    public sealed class NullEnemyAnimator : IEnemyAnimator
    {
        public static readonly NullEnemyAnimator Instance = new NullEnemyAnimator();

        private NullEnemyAnimator() { }

        public void SetMoveSpeed(float speed) { }
        public void PlayAttack() { }
        public void PlayRangedAttack() { }
        public void SetRoll(bool roll) { }
        public void SetHidden(bool hidden) { }
        public void PlayTakeDamage() { }
        public void SetDead(bool dead) { }
    }
}
