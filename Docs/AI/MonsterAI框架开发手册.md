# Monster AI 框架开发手册

分支：`codex/ai-ability-refactor`

## 1. 本期范围

本期完成运行时框架重构，目标如下：

- 普通怪与精英怪优先通过配置组合能力，不新增具体状态类。
- Shell、Werewolf 的行为全部由外联表定义。
- EnemyBrain 自动补齐并解析本地组件，不再手动拖组件引用。
- Spawner 支持按 TID 选择怪物。
- 配置、动画映射和引用在启动时校验。
- 可视化行为树编辑器不在本期范围内。

本次没有修改场景、Prefab、Animator Controller 或已有 ScriptableObject 资产。
新增了怪物核心、能力、行为规则、行为条件和动画绑定五张 Excel 外联表；
运行时 txt 全部由 `Excel2Json` 工具生成。
Unity 首次导入新增脚本时会自动生成对应 `.meta` 文件。

## 2. 架构

```text
Monster external tables
                  |
                  v
          MonsterAIDefinition
                  |
     +------------+-------------+
     |                          |
 BehaviorRuleSelector      EnemyAbilityRuntime
     |                          |
     |                     AbilityExecutor
     |                 +----------+----------+
     |                 |          |          |
     v                 v          v          v
 Ability/Chase/Idle  Movement   Combat   AnimationPort
                                      |
                                      v
                               Animator parameters
```

主要模块：

| 模块 | 文件 | 职责 |
|---|---|---|
| 运行时装配 | `EnemyBrain.cs` | 自动解析组件、加载定义、驱动模式和能力 |
| 统一数据 | `Ability/Data/` | 怪物、能力、行为规则、动画映射的数据类型 |
| 能力运行时 | `Ability/Runtime/`、`Ability/Executors/` | 冷却、能力选择、通用执行器 |
| 行为选择 | `Ability/Behaviour/` | 条件求值、顺序判断并选择能力或追击 |
| 定义组装 | `Ability/Repository/` | 按 TID 组装外联表 |
| 配置校验 | `Ability/Validation/` | 校验结构、引用和战斗数据连接 |
| 动画适配 | `EAnimator/EnemyAnimator.cs` | 语义动作到 Animator 参数的映射 |

运行模式只保留 `Idle / Combat / Stagger / Dead`。具体招式不再对应 FSM 状态类。

## 3. Animator 参数契约

AI 代码不直接使用 `Melee`、`Smash`、`Dash` 等资源名称。它只使用稳定语义键：

| 语义键 | 含义 | 建议类型 |
|---|---|---|
| `action.melee` | 普通近战 | Trigger |
| `action.heavy` | 重击 | Trigger |
| `action.pounce` | 扑击 | Trigger |
| `action.leap` | 跃击 | Trigger |
| `action.ranged` | 远程攻击 | Trigger |
| `action.spell` | 施法表现 | Trigger |
| `action.alert` | 警戒表现 | Trigger |
| `action.take_damage` | 受击 | Trigger |
| `state.roll` | 滚动姿态 | Bool |
| `state.guard` | 防御姿态 | Bool |

`move.speed` 使用 Float，死亡使用 Bool。每个怪物在 `animation` 中映射真实资源名称：

```json
"animation": {
  "speedParameter": "MoveSpeed",
  "deadParameter": "IsDead",
  "bindings": [
    { "semanticKey": "action.melee", "bindingType": "Trigger", "parameterName": "Bite" },
    { "semanticKey": "state.guard", "bindingType": "Bool", "parameterName": "ShellClosed" }
  ]
}
```

`bindingType` 支持 `Trigger / Bool / Float / State`。`State` 使用 Animator State 名称并通过 `CrossFade` 播放。加载时会检查参数是否存在且类型一致；错误会在启动阶段打印怪物名称、TID、语义键和实际参数名。

Shell 映射示例：

| 语义 | Shell 参数 |
|---|---|
| `action.melee` | `Melee` |
| `action.ranged` | `RangedAttack` |
| `state.roll` | `Dash` |
| `state.guard` | `Stance` |

Werewolf 映射示例：

| 语义 | Werewolf 参数 |
|---|---|
| `action.melee` | `Melee` |
| `action.heavy` | `Heavy` |
| `action.pounce` | `Pounce` |
| `action.leap` | `Leap` |
| `action.ranged` | `RangedAttack` |
| `action.alert` | `Alert` |
| `action.spell` | `Spell` |

参数名称不同不需要改代码，只需要填写各自的动画映射。

## 4. 统一怪物配置

Monster AI 使用五张独立 Excel 外联表，作者只修改 Excel，不直接维护 txt：

| Excel 源表 | 生成文件 | 关联键 |
|---|---|---|
| `Data/Tables/MonsterAI.xlsx` | `Data/Data/MonsterAI.txt` | `TID` |
| `Data/Tables/MonsterAbility.xlsx` | `Data/Data/MonsterAbility.txt` | `TID` |
| `Data/Tables/MonsterBehavior.xlsx` | `Data/Data/MonsterBehavior.txt` | `TID + RuleIndex` |
| `Data/Tables/MonsterBehaviorCondition.xlsx` | `Data/Data/MonsterBehaviorCondition.txt` | `TID + RuleIndex + ConditionIndex` |
| `Data/Tables/MonsterAnimation.xlsx` | `Data/Data/MonsterAnimation.txt` | `TID + BindingIndex` |

标准流程：

```text
编辑 Excel 外联表
-> 运行 Data/Excel2Json.cmd
-> 生成 Data/Data/Monster*.txt
-> DataManager 建立按 TID 分组的外联字典
-> EnemyBrain 按 TID 组装 MonsterAIDefinition
```

`Excel2Json` 会记录最近一次生成的 JSON 哈希。若工作台保存过 JSON，
再次转换会中止并提示，避免 Excel 静默覆盖工作台修改。

现有表已经包含 TID 2001 Shell 和 TID 2002 Werewolf。所有表沿用现有三行表头：

- 第 1 行：中文显示名。
- 第 2 行：字段类型，支持 `Integer`、`Float`、`String`、`Boolean`。
- 第 3 行：导出 JSON 字段名，也是 Excel Table 的表头。
- 第 4 行开始：数据。

### 4.1 MonsterAI

主体表，每只怪物一行：

```text
Key, TID, Name, viewRange, loseTargetRange, scanInterval,
runSpeed, turnSpeed, stoppingDistance, hitStunDuration,
speedParameter, deadParameter
```

`Key` 是导出 JSON 的顶层键，填写 TID。`speedParameter` 和 `deadParameter`
分别对应 Animator 的移动 Float 参数和死亡 Bool 参数。

### 4.2 MonsterAbility

每只怪物的每一招一行：

```text
Key, TID, AbilityId, Slot, Executor,
AnimationAction, AnimationState,
Duration, Cooldown, DamageComboIndex, Ranged,
MinRange, MaxRange, Speed, HitDelay,
LockMovement, StopMovementAtEnd, OncePerCombat,
DamageTakenMultiplier, CountsAsAttack, Priority
```

同一个 `TID` 可以有多行能力。`AbilityId` 必须唯一。`Executor` 使用内置执行器 ID；
不需要动画动作或状态时留空，不再填写 JSON。

### 4.3 MonsterBehavior

每只怪物的一条行为规则一行：

```text
Key, TID, RuleIndex, Action, AbilityId, Slot
```

`RuleIndex` 从 0 开始，越小优先级越高。`Action` 支持 `ability`、`chase`、`idle`。
`chase` 和 `idle` 的 `AbilityId`、`Slot` 留空。

### 4.4 MonsterBehaviorCondition

行为规则的每个条件一行：

```text
Key, TID, RuleIndex, ConditionIndex, ConditionId,
Text1, Text2, P1, P2
```

通过 `TID + RuleIndex` 关联 `MonsterBehavior`，通过 `ConditionIndex`
确定同一规则中的条件顺序。`Text1`、`Text2` 用于能力 ID、槽 ID 等字符串参数，
`P1`、`P2` 用于距离、血量阈值和冷却等数值参数。

### 4.5 MonsterAnimation

每只怪物的一个动画绑定一行：

```text
Key, TID, BindingIndex, SemanticKey, BindingType, ParameterName
```

通过 `TID` 关联怪物主体，通过 `BindingIndex` 保证绑定顺序。
`BindingType` 支持 `Trigger`、`Bool`、`Float`、`State`。不同怪物可以使用完全不同的
真实参数名，但 AI 只引用 `action.melee`、`state.guard` 这类语义键。

### 4.6 DataManager 外联字典

`DataManager` 加载生成文件后建立：

```csharp
MonsterAICores
MonsterAbilitiesByTid
MonsterBehaviorsByTid
MonsterBehaviorConditionsByTid
MonsterAnimationBindingsByTid
```

`MonsterAIDefinitionRepository` 再按 TID 把主体、能力、规则、条件和动画绑定组装成
运行时 `MonsterAIDefinition`。这与 `ComboGroupDict` 通过攻击组和段号组装攻击配置
属于同一种外联表思路。

如果某个 TID 不在 `MonsterAI.txt` 中，EnemyBrain 会在启动时直接报错，不存在旧配置回退。

## 5. 能力字段

| 字段 | 说明 |
|---|---|
| `id` | 全局唯一能力 ID |
| `slot` | 能力槽，例如 `primary_attack`、`gap_close` |
| `executor` | 通用执行机制 |
| `animationAction` | 一次性动作语义 |
| `animationState` | 持续状态语义 |
| `duration` | 能力持续时间 |
| `cooldown` | 冷却时间 |
| `damageComboIndex` | AttackDefine 段号，不产生伤害时小于 0 |
| `minRange/maxRange` | 启动距离 |
| `speed` | 突进速度 |
| `hitDelay` | 从能力开始到伤害事件的时间 |
| `lockMovement` | 是否锁定位移 |
| `oncePerCombat` | 当前实例是否只允许使用一次 |
| `damageTakenMultiplier` | 姿态期间的承伤倍率 |
| `countsAsAttack` | 是否更新全局攻击冷却 |
| `priority` | 同一槽内多个能力的尝试顺序 |

内置执行器：

| `executor` | 用途 |
|---|---|
| `attack` | 原地攻击、重击、近战 |
| `ranged_attack` | 向目标位置发送远程攻击 |
| `dash` | 纯位移，例如 Shell Roll |
| `dash_attack` | 位移攻击；当前 Werewolf 未使用 |
| `stance` | 持续姿态，例如 Hide |
| `play_and_wait` | 播放动作并等待，例如 Alert、Hit |
| `dead` | 死亡反应 |

现有两只怪物的能力分类：

| 怪物 | 能力 |
|---|---|
| Shell | Attack/Melee、RangedAttack、Dash/Roll、Stance/Hide、Hit、Dead |
| Werewolf | Attack/Melee、Attack/Heavy、Attack/Pounce、RangedAttack、PlayAndWait/Alert、PlayAndWait/Spell、Hit、Dead |

当前 `Pounce` 是原地攻击，和 Heavy、Melee 一样使用 `attack` 执行器；`Roll` 不产生伤害，属于纯位移能力。
当前 Werewolf 配置已移除 `Leap`；框架仍保留通用 Leap 语义，其他怪物需要时可以重新配置。

当前 Werewolf 攻击节奏：

| 能力 | 距离 | Duration | Cooldown | HitDelay |
|---|---|---|---|---|
| Melee | 0～2.4 | 1.0 | 1.0 | 0.35 |
| Heavy | 0～2.4 | 1.5 | 5.0 | 0.55 |
| Pounce | 0～2.4 | 0.35 | 2.0 | 0.15 |
| Ranged | 6.0～11.0 | 0.8 | 4.0 | 0.30 |

设计意图：贴身攻击按 `Heavy -> Pounce -> Melee` 轮换，远距离使用 Ranged。
Pounce 使用原地 `attack` 执行器，不产生位移。Melee 使用 1 秒全局攻击冷却兜底，
避免三种近战同时进入冷却后出现长时间空窗。

## 6. 行为条件

规则按数组顺序从高到低匹配，全部条件成功才执行动作。

| 条件 ID | 参数 |
|---|---|
| `always` | 无 |
| `has_target` | 无 |
| `is_dead` | 无 |
| `recently_hit` | 无 |
| `hp_below` | `p1` 为 0 到 1 的血量比例 |
| `in_range` | `p1` 最小距离，`p2` 最大距离 |
| `ability_ready` | `text1` 能力 ID；冷却读取 Ability.Cooldown |
| `slot_ready` | `text1` 槽 ID；冷却和距离读取 Ability |
| `slot_unused` | `text1` 槽 ID |
| `attack_cooldown_ready` | `p1` 冷却 |

技能的距离、冷却和一次性使用限制以 `MonsterAbility` 为准。普通技能规则
不需要重复填写 `in_range`、`ability_ready`、`slot_ready` 或 `slot_unused`。
这些条件保留用于兼容旧配置或表达额外策略限制。

动作支持：

| `action` | 字段 |
|---|---|
| `ability` | 优先 `abilityId`，不可用时回退 `slot` |
| `chase` | 无 |
| `idle` | 无 |

特殊条件通过 `BehaviorConditionRegistry.Register` 增加，不修改行为选择器。

## 7. 新增怪物最小流程

1. 准备模型和 Animator Controller。
2. 在 Prefab 根节点只添加 `EnemyBrain`。`RequireComponent` 会自动补齐 `CustomNavAgent`、`EnemyPerception`、`EnemyEntity`、`EnemyCombat`、`Rigidbody` 和 `CombatAttribute`。
3. 保证层级中存在 Animator。`EnemyAnimator` 会自动查找或创建，组件引用无需拖拽。
4. 保留或添加与模型匹配的 Collider。AI 不需要手动绑定 Collider，但玩家攻击命中和目标层检测仍需要它。
5. 根节点 Layer 保持为 `Monster`；自动装配不会替你修改 Layer。
6. 在 `CharacterDefine.txt` 增加 TID、SubType 和 Resource。
7. 在 `AttackDefine.txt` 增加伤害段；不产生伤害的能力不配置。
8. 在 `MonsterAI.xlsx` 增加怪物主体行。
9. 在 `MonsterAbility.xlsx` 增加该怪物的能力行。
10. 在 `MonsterBehavior.xlsx` 增加行为规则，在 `MonsterBehaviorCondition.xlsx` 增加对应条件。
11. 在 `MonsterAnimation.xlsx` 增加真实 Animator 参数绑定。
12. 运行 `Data/Excel2Json.cmd`，确认生成 MonsterAI、MonsterAbility、MonsterBehavior、MonsterBehaviorCondition 和 MonsterAnimation 对应的五个 txt。
13. 把 Spawner 的 `spawnTid` 设置为该 TID，不需要拖 `enemyPrefab`。
14. 进入 Play，检查 Console 中的启动校验和 Inspector 的 Runtime Snapshot。

目前 Shell 和 Werewolf 都已存在于 `MonsterAI.xlsx`。配置表中缺少 TID 时直接启动失败。

## 8. 文件存在性检查

教程中作为实际运行入口提到的文件：

| 文件 | 状态 |
|---|---|
| `Data/Tables/MonsterAI.xlsx` | 怪物主体 Excel，包含 TID 2001、2002 |
| `Data/Tables/MonsterAbility.xlsx` | 能力外联表，包含 Shell 6 项、Werewolf 9 项 |
| `Data/Tables/MonsterBehavior.xlsx` | 行为规则外联表，包含 Shell 6 条、Werewolf 9 条 |
| `Data/Tables/MonsterBehaviorCondition.xlsx` | 行为条件外联表，包含 Shell 7 条、Werewolf 9 条 |
| `Data/Tables/MonsterAnimation.xlsx` | 动画绑定外联表，包含 Shell 5 条、Werewolf 8 条 |
| `Data/Data/MonsterAI.txt` | 已由 Excel2Json 生成 |
| `Data/Data/MonsterAbility.txt` | 已由 Excel2Json 生成 |
| `Data/Data/MonsterBehavior.txt` | 已由 Excel2Json 生成 |
| `Data/Data/MonsterBehaviorCondition.txt` | 已由 Excel2Json 生成 |
| `Data/Data/MonsterAnimation.txt` | 已由 Excel2Json 生成 |
| `Data/Data/CharacterDefine.txt` | 已存在 |
| `Data/Data/AttackDefine.txt` | 已存在 |
| `Assets/Scripts/AI/Enemy/Ability/Data/` | 运行时数据对象目录 |
| `Assets/Scripts/AI/Enemy/Ability/Runtime/` | 能力运行时与接口目录 |
| `Assets/Scripts/AI/Enemy/Ability/Executors/` | 通用能力执行器目录 |
| `Assets/Scripts/AI/Enemy/Ability/Behaviour/` | 行为条件与选择器目录 |
| `Assets/Scripts/AI/Enemy/Ability/Repository/` | 怪物定义组装目录 |
| `Assets/Scripts/AI/Enemy/Ability/Validation/` | 怪物定义校验目录 |
| `Assets/Scripts/AI/Enemy/EnemyBrain.cs` | 已存在 |
| `Assets/Scripts/AI/Enemy/EAnimator/EnemyAnimator.cs` | 已存在 |

## 9. 启动校验

启动失败会停止 EnemyBrain 并输出明确错误，包括：

- 能力 ID 重复或为空。
- 未知执行器。
- 未知行为条件。
- 行为引用了不存在的能力或槽。
- 能力缺少动画语义映射。
- Animator 参数不存在或类型不符。
- CharacterDefine 或 AttackDefine 段号缺失。

## 10. 扩展方式

新增普通能力：填写能力数据和动画映射，不修改 EnemyBrain。

新增条件：实现一个 `BehaviorConditionRegistry.Evaluator`，调用 `BehaviorConditionRegistry.Register` 注册。

新增执行机制：实现 `IAbilityExecutor`，调用 `AbilityExecutorRegistry.Register` 注册。Boss 模块应放在独立文件中，不修改普通怪执行器。

## 11. 当前限制与后续

- 无可视化行为树编辑器。
- 无运行时行为树节点高亮。
- 无配置热重载。
- 伤害使用通用事件和 AttackDefine，尚未接入动画事件驱动。
- Boss 多阶段可继续用行为表与独立执行器扩展，本期不实现编辑器。

后续可视化编辑器应直接编辑 `MonsterAIDefinition`，不要建立第二套运行数据。

## 12. 测试清单

- Shell：发现、追击、近战、远程、Roll、Hide、受击、死亡。
- Werewolf：Alert、Melee、Heavy、Ranged、Pounce、Leap、Spell、受击、死亡。
- 任一怪物从 Spawner 按 TID 刷出。
- 故意删除动画映射、修改错误执行器、填写错误 AttackDefine 段号，确认启动时报错。
- 同时刷出多只同类怪物，确认能力冷却互不影响。
