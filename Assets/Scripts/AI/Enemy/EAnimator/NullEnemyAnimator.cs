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
        /// <summary>
        /// 所有需要动画端口但当前没有 Animator 的组件共享的空实现。
        /// </summary>
        public static readonly NullEnemyAnimator Instance = new NullEnemyAnimator();

        /// <summary>
        /// 禁止外部创建新的空动画实例。
        /// </summary>
        private NullEnemyAnimator() { }

        /// <summary>
        /// 空动画实现始终认为配置有效，不做任何参数校验。
        /// </summary>
        /// <param name="profile">当前怪物使用的动画配置。</param>
        /// <returns>固定返回 true。</returns>
        public bool Configure(Emberwild.AI.Enemy.Ability.AnimationProfileDefinition profile) => true;

        /// <summary>
        /// 空动画实现忽略一次性动作请求。
        /// </summary>
        /// <param name="semanticKey">动作语义键。</param>
        public void PlaySemanticAction(string semanticKey) { }

        /// <summary>
        /// 空动画实现忽略持续状态切换请求。
        /// </summary>
        /// <param name="semanticKey">状态语义键。</param>
        /// <param name="active">状态开关。</param>
        public void SetSemanticState(string semanticKey, bool active) { }

        /// <summary>
        /// 空动画实现忽略移动混合值。
        /// </summary>
        /// <param name="normalizedSpeed">归一化移动速度。</param>
        public void SetMoveSpeed(float normalizedSpeed) { }

        /// <summary>
        /// 空动画实现忽略死亡状态切换。
        /// </summary>
        /// <param name="dead">死亡状态开关。</param>
        public void SetDead(bool dead) { }

        /// <summary>
        /// 空动画实现不需要重置任何 Animator 参数。
        /// </summary>
        public void ResetForSpawn() { }
    }
}
