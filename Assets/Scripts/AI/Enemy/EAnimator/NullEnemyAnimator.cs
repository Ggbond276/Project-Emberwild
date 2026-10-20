namespace Emberwild.AI.Enemy.EAnimator
{
    /// <summary>
    /// 空实现兜底（Null Object 模式）。
    ///
    /// 为什么需要：状态类里会无条件调 Context.Animator.SetMoveSpeed(...)，
    /// 如果没挂动画组件就是 null → NRE 刷屏。
    /// 用空对象替代 null，让"没有动画"退化成"不播动画"，而不是"游戏崩"。
    ///
    /// 4 个方法，和接口一致。
    /// </summary>
    public sealed class NullEnemyAnimator : IEnemyAnimator
    {
        public static readonly NullEnemyAnimator Instance = new NullEnemyAnimator();

        private NullEnemyAnimator() { }

        public void SetMoveSpeed(float normalizedSpeed) { }
        public void PlayAction(EnemyAction action) { }
        public void SetState(EnemyStateFlag state, bool active) { }
        public void SetDead(bool dead) { }
    }
}
