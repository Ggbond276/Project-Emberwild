using System;
using System.Collections.Generic;

namespace Emberwild.AI.Enemy.Ability
{
    /// <summary>
    /// 执行器注册表。普通能力使用内置执行器，特殊 Boss 机制可以注册独立实现。
    /// </summary>
    public static class AbilityExecutorRegistry
    {
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

        /// <summary>注册或替换一个执行器工厂。</summary>
        public static void Register(string executorId, Func<IAbilityExecutor> factory)
        {
            if (string.IsNullOrWhiteSpace(executorId) || factory == null) return;
            Factories[executorId] = factory;
        }

        /// <summary>判断指定执行器 ID 是否已注册。</summary>
        public static bool Contains(string executorId)
        {
            return !string.IsNullOrWhiteSpace(executorId) && Factories.ContainsKey(executorId);
        }

        /// <summary>创建一个新的执行器实例；未知 ID 返回 null。</summary>
        public static IAbilityExecutor Create(string executorId)
        {
            return !string.IsNullOrWhiteSpace(executorId)
                && Factories.TryGetValue(executorId, out var factory)
                ? factory()
                : null;
        }
    }
}
