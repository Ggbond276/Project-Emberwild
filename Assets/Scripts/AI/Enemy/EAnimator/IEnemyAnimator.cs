using Emberwild.AI.Enemy.Ability;

namespace Emberwild.AI.Enemy.EAnimator
{
    public interface IEnemyAnimator
    {
        #region 动画配置 (Animation Configuration)
        /// <summary>应用动画配置并验证参数。</summary>
        bool Configure(AnimationProfileDefinition profile);

        /// <summary>清理池复用残留的动画参数。</summary>
        void ResetForSpawn();
        #endregion

        #region 动画指令 (Animation Commands)
        /// <summary>播放一次性语义动作。</summary>
        void PlaySemanticAction(string semanticKey);

        /// <summary>开启或关闭持续语义状态。</summary>
        void SetSemanticState(string semanticKey, bool active);

        /// <summary>设置移动动画混合值。</summary>
        void SetMoveSpeed(float normalizedSpeed);

        /// <summary>设置死亡动画参数。</summary>
        void SetDead(bool dead);
        #endregion
    }
}
