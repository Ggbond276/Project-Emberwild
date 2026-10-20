using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emberwild.AI.Enemy.Ability;

namespace Emberwild.AI.Enemy.EAnimator
{
    /// <summary>
    /// Animation port used by ability executors. Implementations map semantic keys
    /// to the actual parameters or states of one Animator Controller.
    /// </summary>
    public interface IEnemyAnimator
    {
        /// <summary>
        /// 应用一只怪物的动画映射，并校验参数名称与类型。
        /// </summary>
        /// <param name="profile">包含移动、死亡和动作语义绑定的动画配置。</param>
        /// <returns>所有必要参数都存在且类型正确时返回 true。</returns>
        bool Configure(AnimationProfileDefinition profile);

        /// <summary>
        /// 播放一个一次性动画语义键。
        /// </summary>
        /// <param name="semanticKey">与具体 Animator 参数解耦的动作语义键。</param>
        void PlaySemanticAction(string semanticKey);

        /// <summary>
        /// 打开或关闭一个持续动画语义状态。
        /// </summary>
        /// <param name="semanticKey">与具体 Animator 参数解耦的状态语义键。</param>
        /// <param name="active">true 表示启用该状态；false 表示关闭。</param>
        void SetSemanticState(string semanticKey, bool active);

        /// <summary>
        /// 移动强度。0 = 待机，1 = 全速跑。
        /// </summary>
        /// <param name="normalizedSpeed">归一化移动速度，取值会被限制在 0 到 1。</param>
        void SetMoveSpeed(float normalizedSpeed);

        /// <summary>
        /// 设置 Animator 的死亡布尔参数，出生重置时需要恢复为 false。
        /// </summary>
        /// <param name="dead">true 表示进入死亡动画；false 表示恢复正常状态。</param>
        void SetDead(bool dead);

        /// <summary>
        /// 清空上一轮生命周期留下的 Trigger、布尔状态、移动混合值和动画播放位置。
        /// </summary>
        void ResetForSpawn();
    }
}
