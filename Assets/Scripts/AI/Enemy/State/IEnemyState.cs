using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emberwild.AI.Enemy.State
{
    /// <summary>
    /// 所有怪物状态的统一契约
    /// 所有状态所有三个生命周期 ： 进入 / 每帧 / 退出
    /// </summary>
    public interface IEnemyState
    {
        /// <summary>
        /// 进入状态的时候调用一次
        /// </summary>
        /// <param name="brain"></param>
        void OnEnter(EnemyBrain brain);

        /// <summary>
        /// 处于状态的时候每帧都调用
        /// </summary>
        /// <param name="brain"></param>
        void OnUpdate(EnemyBrain brain);

        /// <summary>
        /// 离开该状态的时候调用一次
        /// </summary>
        /// <param name="brain"></param>
        void OnExit(EnemyBrain brain);
    }
}
