using System;
using System.Collections.Generic;

namespace Emberwild.AI.Enemy.Ability
{
    public static class AbilityExecutorRegistry
    {
        #region 执行器注册表 (Executor Registry)
        private static readonly Dictionary<string, Func<IAbilityExecutor>> Factories =
            new Dictionary<string, Func<IAbilityExecutor>>(StringComparer.OrdinalIgnoreCase)
            {
                [AbilityExecutorIds.Attack] = () => new AttackAbilityExecutor(false),
                [AbilityExecutorIds.RangedAttack] = () => new AttackAbilityExecutor(true),
                [AbilityExecutorIds.Dash] = () => new DashAbilityExecutor(false),
                [AbilityExecutorIds.DashAttack] = () => new DashAbilityExecutor(true),
                [AbilityExecutorIds.Stance] = () => new StanceAbilityExecutor(),
                [AbilityExecutorIds.PlayAndWait] = () => new PlayAndWaitAbilityExecutor(),
                [AbilityExecutorIds.Dead] = () => new DeadAbilityExecutor()
            };
        #endregion

        #region 执行器注册与创建 (Executor Registration)
        /// <summary>注册或替换执行器工厂。</summary>
        public static void Register(string executorId, Func<IAbilityExecutor> factory)
        {
            if (string.IsNullOrWhiteSpace(executorId) || factory == null) return;
            Factories[executorId] = factory;
        }

        /// <summary>判断执行器 ID 是否已经注册。</summary>
        public static bool Contains(string executorId)
        {
            return !string.IsNullOrWhiteSpace(executorId) && Factories.ContainsKey(executorId);
        }

        /// <summary>按 ID 创建执行器实例。</summary>
        public static IAbilityExecutor Create(string executorId)
        {
            return !string.IsNullOrWhiteSpace(executorId)
                && Factories.TryGetValue(executorId, out var factory)
                ? factory()
                : null;
        }
        #endregion
    }
}
