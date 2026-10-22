# Ability Registries

本目录存放策略模式注册表。

- `BehaviorConditionRegistry`：条件 ID 到求值委托。
- `AbilityExecutorRegistry`：执行器 ID 到执行器工厂。

这里负责注册和创建策略。具体策略实现仍放在 `Ability/Executors`。
