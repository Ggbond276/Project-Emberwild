using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emberwild.AI.Enemy.State
{
    /// <summary>
    /// 状态工厂:状态零件库 + 注册入口。
    /// 新怪物加状态 = 实现 IEnemyState + Register 一行。
    /// </summary>
    public class EnemyStateFactory
    {
        /// <summary>
        /// 存放创建对象的方法
        /// </summary>
        private static readonly Dictionary<EnemyStateType, Func<IEnemyState>> _creators =
            new Dictionary<EnemyStateType, Func<IEnemyState>>
            {
                [EnemyStateType.Idle] = () => new IdleState(),
                [EnemyStateType.Chase] = () => new ChaseState(),
                [EnemyStateType.AttackMelee] = () => new AttackMeleeState(),
                [EnemyStateType.AttackRanged] = () => new AttackRangedState(),
                [EnemyStateType.RollChase] = () => new RollChaseState(),
                [EnemyStateType.Hide] = () => new HideState(),
                [EnemyStateType.Hit] = () => new HitState(),
                [EnemyStateType.Dead] = () => new DeadState(),
            };

        /// <summary>
        /// 传入状态类型，创建并返回对应的状态对象。
        /// </summary>
        public static IEnemyState Create(EnemyStateType type)
            => _creators.TryGetValue(type, out var creator) ? creator() : null;

        /// <summary>
        /// 传入状态类型和创建方法，将该状态注册到工厂中。
        /// </summary>
        public static void Register(EnemyStateType type, Func<IEnemyState> creator)
            => _creators[type] = creator;
    }
}
