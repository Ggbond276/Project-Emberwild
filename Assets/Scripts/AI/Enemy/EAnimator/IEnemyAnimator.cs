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
        bool Configure(AnimationProfileDefinition profile);

        /// <summary>播放一个一次性动画语义键。</summary>
        void PlaySemanticAction(string semanticKey);

        /// <summary>打开或关闭一个持续动画语义状态。</summary>
        void SetSemanticState(string semanticKey, bool active);

        /// <summary>
        /// 移动强度。0 = 待机，1 = 全速跑。
        /// </summary>
        void SetMoveSpeed(float normalizedSpeed);

        /// <summary>
        /// 死亡。只能置 true。
        /// </summary>
        void SetDead(bool dead);
    }
}
