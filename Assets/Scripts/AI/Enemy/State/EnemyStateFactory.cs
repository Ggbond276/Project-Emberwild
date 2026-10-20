using System;
using System.Collections.Generic;

namespace Emberwild.AI.Enemy.State
{
    /// <summary>
    /// 状态工厂：状态类型 → 实例构造器。
    /// 新增状态必须在此注册，否则运行时 ChangeState 会报「状态未注册」。
    /// </summary>
    public class EnemyStateFactory
    {
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

                [EnemyStateType.Alert] = () => new AlertState(),
                [EnemyStateType.AttackHeavy] = () => new AttackHeavyState(),
                [EnemyStateType.Pounce] = () => new PounceState(),
                [EnemyStateType.Leap] = () => new LeapState(),
                [EnemyStateType.CastSpell] = () => new CastSpellState(),
            };

        /// <summary>创建状态实例，未注册返回 null。</summary>
        public static IEnemyState Create(EnemyStateType type)
            => _creators.TryGetValue(type, out var creator) ? creator() : null;

        /// <summary>运行时注册新状态。</summary>
        public static void Register(EnemyStateType type, Func<IEnemyState> creator)
            => _creators[type] = creator;

        /// <summary>已注册的状态数量（调试用）。</summary>
        public static int RegisteredCount => _creators.Count;
    }
}
