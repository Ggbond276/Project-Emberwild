# 怪物 AI 门控去重迁移 · 待审查问题清单

> 用途：交给 Codex 复核。每条都标注了「代码可证 / 需运行验证」，以及定位到的文件与行号。
> 背景：2026-10-21 18:11–18:22 有一次**未提交**的"门控去重迁移"——把"距离 / 冷却 / 一次性"的判定
> 从 `MonsterBehaviorCondition`（规则条件）下移到 `MonsterAbility`（能力表）+ 规则选择器预筛。
> 代码、xlsx、txt、文档四者已同步改动，但都还在工作区，未 commit。

---

## P0 · 状态风险：代码与数据强耦合，且都未提交

**状态**：代码可证（`git status` / `git diff`）

**位置**：
- 代码：`Assets/Scripts/AI/Enemy/Ability/Behaviour/BehaviourRuleSelector.cs`、`EnemyBrain.cs`、`Ability/Runtime/EnemyAbilityRuntime.cs`
- 数据：`Data/Tables/MonsterAbility.xlsx`、`Data/Tables/MonsterBehaviorCondition.xlsx`、`Data/Data/*.txt`
- 文档：`Docs/AI/MonsterAIEditorWindow使用说明.md`、`Docs/AI/MonsterAI框架开发手册.md`

**现象**：上述 10 个代码文件 + 4 个数据文件全部处于 modified 未提交状态。

**机制**：迁移把"可用性判定"从规则表搬到能力表，两侧必须成对生效。四种组合里只有两种正确：

| 代码 | 数据 | 结果 |
|---|---|---|
| 旧（HEAD） | 旧（条件表 30 行） | 正常 |
| 旧 | 新（条件表 16 行） | 错误：规则命中即激活，激活失败降级追击 → 狼人只追不打 |
| 新（工作区） | 新（16 行） | 正常（当前） |
| 新 | 旧（30 行） | 可运行但双门 AND，攻击距离被规则与能力同时收窄 |

**验证**：`git status --short`；`git diff --stat Data/ Assets/`；对照 `git show HEAD:Data/Data/MonsterBehaviorCondition.txt`（30 行）与当前文件（16 行）。

**期望**：代码与数据同一次提交，避免任一侧单独回滚。

---

## P1-1 · `Get()` 缓存命中不重置 `HasLoadError`，已缓存怪物被连坐

**状态**：代码可证

**位置**：`Assets/Scripts/AI/Enemy/Ability/Repository/MonsterAIDefinitionRepository.cs:25-47`
调用方：`EnemyBrain.ConfigureRuntime()`（先查 `HasLoadError`，再查 `definition == null`）

**现象**：
```csharp
public static MonsterAIDefinition Get(int tid)
{
    if (Cache.TryGetValue(tid, out var cached)) return cached;   // ← 直接返回，不碰 HasLoadError
    ...
    try { ...; HasLoadError = false; ... }                        // ← 只有真正 Build 才清标志
    catch { HasLoadError = true; ... }
}
```
`HasLoadError` 是**静态全局标志**，语义却是"最近一次装配的结果"。缓存命中路径既不读也不写它。

**复现**：
1. 让 TID A 的动画绑定 `BindingType` 写错（`Build` 会 `throw`）→ 刷 A，`HasLoadError = true`；
2. 刷 TID B（此前已成功装配并缓存）→ `Get(B)` 走缓存直接返回，`HasLoadError` 仍为 `true`；
3. `ConfigureRuntime` 先判 `HasLoadError` → B 也 `FailStartup`，尽管 B 的定义完全正常。

**期望**：缓存命中时把 `HasLoadError` 复位（或改为按 TID 记录错误，而非全局标志）。
**实际**：一个怪物的配置错误会让之后所有"已缓存"的怪物启动失败。

---

## P1-2 · `FindExecutableAbility` 丢失"AbilityId 失败回退 Slot"的语义

**状态**：代码可证；当前数据下无可见影响（潜在回归）

**位置**：`Assets/Scripts/AI/Enemy/Ability/Runtime/EnemyAbilityRuntime.cs:134-157`

**现象**：
```csharp
if (!string.IsNullOrWhiteSpace(abilityId))
{
    return _byId.TryGetValue(abilityId, out var directAbility) && CanActivate(directAbility)
        ? directAbility
        : null;          // ← abilityId 不可用时直接返回 null，不再尝试 slot
}
```
迁移前的 `EnemyBrain.TryActivateDecision` 是"先 `TryActivateAbility(id)`，失败再 `TryActivateSlot(slot)`"，即 **AbilityId 优先、Slot 兜底**。迁移后选择器只调用一次 `FindExecutableAbility(rule.abilityId, rule.slot)`，而该函数在 `abilityId` 非空时**不会**看 `slot`。

**复现条件**（当前数据不满足）：
- 规则同时填 `AbilityId` 与 `Slot`；
- 该 `Slot` 下还有**其它**能力（`priority` 不同）；
- 被点名的能力处于冷却 → 旧逻辑会用槽内另一招，新逻辑整条规则被跳过。

现有数据：`werewolf.pounce` 的规则同时填了 `AbilityId=werewolf.pounce` 与 `Slot=secondary_attack`，但该槽只有它一个，所以看不出差异。

**期望**：`abilityId` 不可用时继续尝试 `slot`（保持旧契约），或明确规定"填了 AbilityId 就不再回退"并同步文档。
**实际**：静默丢掉了槽兜底。

---

## P1-3 · 决策失败分支只打日志、不改模式，可能每帧刷 Error

**状态**：代码可证；触发条件目前近乎不可达（防御性缺口）

**位置**：`Assets/Scripts/AI/Enemy/EnemyBrain.cs:414-444`

**现象**：
```csharp
case BehaviorActions.Ability:
    if (TryActivateDecision(decision)) { _mode = RuntimeMode.Combat; return; }
    AppLog.Error("Enemy", $"{name}: failed to start resolved ability '{decision.AbilityId}'.");
    return;      // ← 不切换模式、不追击、不待机
```
`Select()` 已保证 `decision.Ability` 可执行，`TryStartResolvedAbility` 又校验了 `IsActive` 与引用相等，所以正常情况下不会走到这里。但一旦走到（例如未来改动引入竞态），`Update` 会在**每一帧**重新决策并重新打印同一条 Error，同时怪物停留在上一模式（可能是 Combat 却没有活跃能力）。

**期望**：失败时降级（追击/待机）并做一次性日志去重，而不是只打日志。
**实际**：日志放大 + 模式悬空。

---

## P1-4 · 工作台 `Normalize()` 按数组下标重写 `RuleIndex`，规则号被压缩

**状态**：代码可证

**位置**：`Assets/Editor/MonsterAI/MonsterAIEditorStore.cs:447-488`（`Normalize`），`SaveAll()` 于 406-445

**现象**：
```csharp
for (int ruleIndex = 0; ruleIndex < profile.Rules.Count; ruleIndex++)
{
    rule.Rule.RuleIndex = ruleIndex;                 // ← 按列表下标重写，不是保留原值
    for (int conditionIndex = 0; ...) condition.RuleIndex = ruleIndex;
}
```
当前 `MonsterBehavior` 里 Werewolf 的规则号是 `0,1,2,3,4,5,7,8`（R6 为空）。只要在**工作台**里打开并保存一次行为页，就会被压缩成 `0..7`。

**影响**：
- 文档 / 论文 / 日志中引用的 "R7 追击" 会指向另一条规则；
- 每次保存都重排全表，`git diff` 噪音大，review 时看不出"到底改了什么"；
- 若外部有按 `RuleIndex` 做的对账脚本（例如条件孤儿检查），会误报。

**期望**：保留已有 `RuleIndex`，只对新增行补空洞（或统一"允许跳号"并在文档中说明）。
**实际**：每次保存都静默重编号。

---

## P2-1 · 能力表数值缺少 sanity 校验（安全阀只剩数据本身）

**状态**：代码可证（缺失校验）；后果需运行验证

**位置**：`Assets/Scripts/AI/Enemy/Ability/Repository/MonsterAIDefinitionFactory.cs`（`MonsterAIDefinitionValidator.Validate`）

**现象**：迁移后"距离门控"只存在于 `MonsterAbility.MaxRange`。当前值：Shell 近战 `0–1.6`、远程 `7–10`、滚动 `3–7`（迁移前均为 `999999`）。校验器只检查 `maxRange >= minRange`，不检查数值是否合理。

**后果**：任何人把 `primary_attack` 的 `maxRange` 写回 `999999`，怪物会回到"隔空挥爪"，**启动无任何告警**（因为能力本身合法）。

**期望**：对 `primary_attack` / `heavy_attack` 等近战槽给出"`maxRange` 明显大于视野半径"的 warning。

---

## P2-2 · 旧条件仍可填，且只给 warning，双门 AND 会静默收窄行为

**状态**：代码可证

**位置**：`Assets/Editor/MonsterAI/MonsterAIEditorOptions.cs:16-28`（下拉仍含 4 个旧条件）；`MonsterAIEditorValidator.cs`（新增分支仅 `AddWarning`）

**现象**：`in_range` / `ability_ready` / `slot_ready` / `slot_unused` 仍在下拉里（标注"策略 / 兼容"）。校验器提示"建议只在能力表维护"，但不阻止。

**后果**：若策划按旧习惯又填回去，就变成"规则门 ∧ 能力门"，行为比设计更窄（例如远程要同时满足规则 `7–10` 与能力 `7–10` 才放），且没有任何报错。

**期望**：升级为 error，或在保存时自动剔除重复门控并提示。

---

## P2-3 · 双真相源：工作台写 txt，`Excel2Json` 从 xlsx 覆盖 txt

**状态**：代码可证（机制），文档已警告但未解决

**位置**：`Assets/Editor/MonsterAI/MonsterAIEditorStore.cs:502-507`（写 `Data/Data/*.txt`）；`Data/Excel2Json.cmd`（`json-excel json Tables\ Data\`）；`MonsterAIEditorWindow.cs:148` 的提示文案

**现象**：工作台保存只写 `Data/Data` 下的 txt；`Excel2Json` 从 `Data/Tables` 的 xlsx 重新生成同名 txt。**谁后跑谁赢**。

**复现**：工作台改一条规则 → 保存 → 运行 `Excel2Json.cmd` → 改动消失（被 xlsx 覆盖），无任何提示。

**期望**：工作台直接写 xlsx，或 `Excel2Json` 前检测 txt 是否比 xlsx 新并阻止覆盖。

---

## P3 · 需运行验证的行为变化（我无法在无 Unity 环境下确认）

1. **迁移后两只怪的实际行为**是否与设计一致：Shell 应在 `0–1.6` 近战、`3–7` 滚、`7–10` 远程、残血缩壳一次；Werewolf 应在 `0–2.4` 按 `heavy → pounce → melee` 轮换、`6–11` 远程、首次见面警戒一次、半血后每 20 s 施法。建议按 6.2 节用例逐条回归。
2. **`RefreshHpRatio()` 的语义变化**：`EnemyBrain.cs` 新增方法在 `attribute == null` 时把 `BB.HpRatio` 置 **0**（迁移前是"不更新，保持上一次值"）。若某帧属性缺失，`hp_below(0.3)` 会瞬时为真，可能触发缩壳/施法分支。属边缘情况，但属于行为差异。
3. **`RuleIndex` 是否已被重排**：当前 txt 仍是 `0,1,2,3,4,5,7,8`，说明迁移后**没有**在工作台保存过行为页；一旦保存就会变。

---

## 附：本次核查用到的证据

- xlsx 元数据：`docProps/app.xml` → `Application = Microsoft Excel Compatible / Openpyxl 3.1.5`（说明由 Python 脚本写入，非 Excel 保存）；`docProps/core.xml` 的 `dcterms:modified` 为 `2026-10-21T10:19:39Z` / `10:21:53Z`。
- txt 生成时间：`MonsterAbility 18:22:05 → MonsterAI 18:22:07 → MonsterAnimation 18:22:09 → MonsterBehavior 18:22:10 → MonsterBehaviorCondition 18:22:12`，与 `Excel2Json` 的顺序一致。
- 条件表 diff：`git diff Data/Data/MonsterBehaviorCondition.txt` → `18 insertions, 137 deletions`（删 14 行并重编号）。
- 能力表 diff：Shell `shell.melee` 的 `MaxRange` `999999 → 1.6`；`shell.ranged` `0–999999 → 7–10`；`shell.roll` `0–999999 → 3–7`。
