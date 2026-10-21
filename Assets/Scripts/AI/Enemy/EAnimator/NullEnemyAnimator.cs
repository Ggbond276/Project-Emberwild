using Emberwild.AI.Enemy.Ability;

namespace Emberwild.AI.Enemy.EAnimator
{
    public sealed class NullEnemyAnimator : IEnemyAnimator
    {
        #region 单例状态 (Singleton State)
        public static readonly NullEnemyAnimator Instance = new NullEnemyAnimator();
        #endregion

        #region 构造与配置 (Construction & Configuration)
        /// <summary>阻止外部创建新的空动画实例。</summary>
        private NullEnemyAnimator() { }

        /// <summary>接收任意动画配置并返回有效。</summary>
        public bool Configure(AnimationProfileDefinition profile) => true;

        /// <summary>忽略动画重置请求。</summary>
        public void ResetForSpawn() { }
        #endregion

        #region 空动画指令 (No-op Animation Commands)
        /// <summary>忽略一次性动作请求。</summary>
        public void PlaySemanticAction(string semanticKey) { }

        /// <summary>忽略持续状态切换请求。</summary>
        public void SetSemanticState(string semanticKey, bool active) { }

        /// <summary>忽略移动动画参数。</summary>
        public void SetMoveSpeed(float normalizedSpeed) { }

        /// <summary>忽略死亡动画参数。</summary>
        public void SetDead(bool dead) { }
        #endregion
    }
}
