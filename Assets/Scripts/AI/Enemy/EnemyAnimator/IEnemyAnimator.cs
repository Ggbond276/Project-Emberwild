using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emberwild.AI.Enemy.EnemyAnimator
{
    /// <summary>
    /// 怪物动画统一接口：状态类只依赖语义动作，各怪物Animator自行翻译成自己的Animator参数
    /// </summary>
    public interface IEnemyAnimator
    {
        void SetMoveSpeed(float speed);   // 移动(0~1 驱动 Locomotion)
        void PlayAttack();                // 近战攻击
        void PlayRangedAttack();          // 远程攻击
        void SetRoll(bool roll);          // 滚动冲刺(持续状态,Bool)
        void SetHidden(bool hidden);      // 缩壳(持续状态,Bool;无此能力的怪空实现)
        void PlayTakeDamage();            // 受击
        void SetDead(bool dead);          // 死亡

    }
}
