using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emberwild.AI.Enemy.EAnimator
{
    /// <summary>
    /// 怪物动画统一接口：状态类只依赖语义动作，各怪物Animator自行翻译成自己的Animator参数
    /// </summary>
    public interface IEnemyAnimator
    {
        /// <summary>
        /// 移动强度。0 = 待机，1 = 全速跑。
        /// </summary>
        void SetMoveSpeed(float normalizedSpeed);

        /// <summary>
        /// 播放一次性动作。actionId 见契约表（melee / heavy / ranged / …）。
        /// </summary>
        void PlayAction(EnemyAction action);

        /// <summary>
        /// 开关可逆持续状态。stateId 见契约表（dash / stance）。
        /// </summary>
        void SetState(EnemyStateFlag state, bool active);

        /// <summary>
        /// 死亡。只能置 true。
        /// </summary>
        void SetDead(bool dead);
    }
}
