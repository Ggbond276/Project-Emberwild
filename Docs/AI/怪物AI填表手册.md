# 怪物 AI 填表手册

> **读者**：策划、关卡设计。不需要读代码。
> **用途**：从零给一只怪填完四张表，且填错时知道错在哪。
> **配套工具**：`Tools/Monster AI Editor`（编辑器工作台）、`Data/Excel2Json.cmd`（导出）

---

## 0 一句话版本

先记住这四句，后面全是在展开它们：

1. **能力表**回答"这只怪会哪几招"，一行 = 一招。
2. **能力 ID** 是这一招的名字，**能力槽**是这一招的归类。名字唯一，归类可重复。
3. **执行器**决定这一招"怎么动"——是原地挥、还是冲出去、还是站着变硬。
4. **动画**只写"语义"（`action.melee`），不写 Animator 里的真实参数名；真实名字在动画表里映射。

---

## 1 四张表的分工

| 表 | 一行代表 | 回答的问题 | 主键 |
|---|---|---|---|
| `MonsterAbility` 能力表 | 一个招式 | 这招怎么动、打多少、多久、多远 | `TID + AbilityId` |
| `MonsterBehavior` 规则表 | 一条判断 | 什么情况下用哪招 | `TID + RuleIndex` |
| `MonsterBehaviorCondition` 条件表 | 一个条件 | 这条判断需要满足什么 | `TID + RuleIndex + ConditionIndex` |
| `MonsterAnimation` 动画表 | 一条映射 | 语义键对应 Animator 里哪个参数 | `TID + BindingIndex` |
| `MonsterAI` 主体表 | 一只怪 | 视野、速度、硬直、动画参数名 | `TID` |

**装配关系**：规则表用 `AbilityId` 指向能力表；能力表的 `AnimationAction / AnimationState` 指向动画表；条件表用 `RuleIndex` 挂到规则表上。

---

## 2 三个前置概念

### 2.1 TID 才是关联键，`Key` 列不是

Excel 第一列的 `Key` 只是行号，**业务上没有意义**。真正把五张表串起来的是每一行里的 `TID` 字段。

- `MonsterAI` 表恰好把 Key 填成了 TID，所以看起来像主键；
- 其余四张表的 Key 是 1、2、3……，**改 Key 不会影响任何行为**。

**规范**：新增行时 Key 可以随便给（不重复即可），但 `TID` 必须和怪物主体表一致。

### 2.2 一只怪最少要填几行

| 表 | 最少行数 | 说明 |
|---|---|---|
| 能力表 | **3 行** | 1 个攻击 + 1 个 `reaction.hit` + 1 个 `dead` |
| 规则表 | **2 行** | 1 条 `ability` + 1 条 `idle` 兜底 |
| 条件表 | 1 行 | `has_target` |
| 动画表 | 2 行 | `action.melee` + `action.take_damage` |

少于这个数，**启动就会失败**（不是运行到一半才出问题）。

### 2.3 三种"填错"的后果完全不同

| 类型 | 表现 | 例子 |
|---|---|---|
| **启动失败** | 怪物不生成，Console 报红 | 执行器名拼错、动画绑定缺失 |
| **静默不生效** | 不报错，但那一格没被读 | 在 `attack` 上填 `AnimationState` |
| **行为退化** | 能跑，但表现不对 | `MaxRange` 填 999999，隔着十米挥爪 |

**最危险的是第二、三种**——校验器抓不到。本手册每章末尾的"契约"就是在标这两类。

---

## 3 契约一：能力 ID（`AbilityId`）

### 3.1 规范

| 项 | 规定 |
|---|---|
| 命名格式 | `怪物名.招式名`，全小写，用点分隔 |
| 字符 | 只允许 `a-z`、`0-9`、`.`、`_` |
| 唯一性 | **同一只怪内必须唯一**（校验器查重）；跨怪可以重名，但建议加前缀区分 |
| 长度 | 建议 ≤ 20 字符，它会被打进日志 |

现有取值参考：

```
shell.melee      shell.ranged    shell.roll     shell.hide
shell.hit        shell.dead
werewolf.melee   werewolf.heavy  werewolf.pounce  werewolf.ranged
werewolf.alert   werewolf.spell  werewolf.hit     werewolf.dead
```

### 3.2 它在哪些地方被用到

| 用处 | 说明 |
|---|---|
| 规则表引用 | `AbilityId` 列点名要放哪招 |
| 冷却记账 | 冷却表以 `AbilityId` 为键，**不是以槽为键** |
| "整场一次"记账 | `OncePerCombat` 同样按 ID 记 |
| 报错信息 | 所有启动错误都会带 `ability 'xxx'`，方便定位 |

**结论：ID 是这一招的身份证，不能省。**

### 3.3 契约（违反会怎样）

| 违反 | 后果 |
|---|---|
| 同一只怪两行用了同一个 ID | **启动失败**：`duplicate ability id 'xxx'` |
| ID 留空 | **启动失败**：`ability with empty id` |
| 规则表写了 ID，能力表没有 | **启动失败**：`behavior references missing ability 'xxx'` |
| 改了 ID 但没改规则表 | 同上，启动失败 |

### 3.4 反例

```
❌ shell.Melee          ← 大写，虽然能用，但和其余行风格不一致，日志里难 grep
❌ 近战               ← 中文，导出后编码风险
❌ melee              ← 没前缀，两只怪都叫 melee 时日志分不清是谁
✅ werewolf.melee
```

---

## 4 契约二：能力槽（`Slot`）

### 4.1 先搞清楚它和 ID 的区别

| | 能力 ID | 能力槽 |
|---|---|---|
| 是什么 | 这一招的**身份** | 这一招的**归类** |
| 唯一吗 | 必须唯一 | 可以多招共用 |
| 用来做什么 | 精确点名 | 按类批量找 |
| 类比 | 身份证号 | 岗位 |

**代码里是两条查找路，先点名、后按类**：

```
规则行 → 有 AbilityId？
          ├─ 有 → 按 ID 精确查 → 能放就返回
          └─ 点名失败（没这招 / 在冷却）→ 按 Slot 取整槽 → 按 Priority 从高到低试
```

所以 **两个都填时，槽只在"ID 那招放不出来"时才生效**。

### 4.2 十一个合法值（只能用这些）

| 值 | 中文 | 用途 |
|---|---|---|
| `primary_attack` | 主攻击 | 普攻 |
| `secondary_attack` | 副攻击 | 连段、快速补刀 |
| `heavy_attack` | 重攻击 | 高伤慢招 |
| `gap_close` | 拉近距离 | 突进贴脸 |
| `ranged_attack` | 远程攻击 | 投射物 |
| `mobility` | 位移 | 翻滚、后跳 |
| `defense` | 防御 | 缩壳、格挡 |
| `alert` | 警戒 | 首次发现的演出 |
| `special` | 特殊能力 | 施法、召唤 |
| **`reaction.hit`** | 受击反应 | **必填**，见下 |
| **`reaction.dead`** | 死亡反应 | 建议填 |

### 4.3 三条硬规则

**规则 1：`reaction.hit` 必须存在。**

校验器会检查，没有就启动失败：

```
TID 2001: missing 'reaction.hit' ability.
```

原因：受击不走规则表（它必须无条件触发），`EnemyBrain` 直接按槽名找这招。

**规则 2：`reaction.*` 两个槽跳过距离判定。**

其他招都要满足 `MinRange ≤ 距离 ≤ MaxRange` 才能放；受击和死亡不看距离，否则"隔着三米被打到却播不出受击动画"。

**规则 3：近战类槽必须填有限的 `MaxRange`。**

以下五个槽，`MaxRange` 填 `999999` 会直接启动失败：

```
primary_attack  secondary_attack  heavy_attack  gap_close  mobility
```

报错：

```
TID 2001: ability 'shell.melee' uses a close-range slot but has no finite MaxRange.
```

**这一条是专门防"隔空挥爪"的**，别绕过它。

### 4.4 什么时候填、什么时候留空

| 你的意图 | 能力表 Slot | 规则表 AbilityId | 规则表 Slot |
|---|---|---|---|
| 就要这一招（**默认用法**） | ✅ 填（分类用） | ✅ 填 | **留空** |
| 这一类里自动挑一个能放的 | ✅ 填 | 留空 | ✅ 填 |
| 优先这招，不行再同类里找 | ✅ 填 | ✅ 填 | ✅ 填 |

**当前项目的实际状态**：每只怪的每个槽都只有 1 招，所以第三种用法**没有任何收益**——回退到槽里找到的还是同一招。

**规范：规则表里 `AbilityId` 与 `Slot` 二选一，默认只填 `AbilityId`。**

### 4.5 契约（违反会怎样）

| 违反 | 后果 |
|---|---|
| 没有 `reaction.hit` | **启动失败** |
| 近战槽 `MaxRange = 999999` | **启动失败** |
| 槽名拼错（`primary_atack`） | 不报错，但规则表按槽引用时找不到 → 启动失败；只按 ID 引用时**静默无害** |
| 规则行同时填 ID 和槽，且槽里只有 1 招 | 不报错，**槽那格是死数据** |

---

## 5 契约三：执行器（`Executor`）

执行器决定这一招"怎么动"。**它是七选一，不能自创。**

### 5.1 七个执行器各干什么

| 值 | 中文 | 它会替你做这些事 |
|---|---|---|
| `attack` | 近战攻击 | 锁移动 → 面向目标 → 播 `AnimationAction` → 到 `HitDelay` 发伤害 → 到 `Duration` 解锁 |
| `ranged_attack` | 远程攻击 | 同上，但伤害判定用远程形状 |
| `dash` | 纯位移 | **强制解锁移动** → 按 `Speed` 每帧冲刺 → 播 `AnimationState` → 到 `Duration` 停 |
| `dash_attack` | 位移攻击 | 同上 + 到 `HitDelay` 发一次伤害 |
| `stance` | 持续姿态 | 锁移动 → 播 `AnimationState` → 改承伤倍率 → 到时恢复 |
| `play_and_wait` | 播放并等待 | 锁移动 → 播 `AnimationAction` → 干等到 `Duration`（不造成伤害） |
| `dead` | 死亡 | 锁移动 → `SetDead(true)` → 关导航 → **永不结束** |

### 5.2 选型决策

按顺序问自己四个问题：

```
① 这招要造成伤害吗？
   ├─ 不要 → ② 
   └─ 要   → ③
② 它要移动吗？
   ├─ 要   → dash
   └─ 不要 → ④
③ 它要冲出去吗？
   ├─ 要   → dash_attack
   └─ 不要 → 距离 > 5 米？→ ranged_attack : attack
④ 它要持续一段时间并改变状态（减伤/护盾/变身）吗？
   ├─ 要   → stance
   └─ 不要 → play_and_wait
```

**死亡是特殊情况**：`dead` 只有一个用途，每只怪必须有且只有一行。

### 5.3 每个执行器读哪些列（最容易踩的地方）

打 ✅ 的才会被读，其余**填了也不生效、也不报错**：

| 列 | attack | ranged_attack | dash | dash_attack | stance | play_and_wait | dead |
|---|---|---|---|---|---|---|---|
| `AnimationAction` | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | ❌ |
| `AnimationState` | ❌ | ❌ | ✅ | ✅ | ✅ | ❌ | ❌ |
| `Duration` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ 永不结束 |
| `HitDelay` | ✅ | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ |
| `DamageComboIndex` | ✅ | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ |
| `Ranged` | ❌ 由执行器决定 | ❌ | — | ✅ | — | — | — |
| `MinRange`/`MaxRange` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ 跳过 |
| `Speed` | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ | ❌ |
| `LockMovement` | ✅ | ✅ | ❌ 强制解锁 | ❌ 强制解锁 | ✅ | ✅ | ❌ 强制锁 |
| `StopMovementAtEnd` | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ | ❌ |
| `DamageTakenMultiplier` | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ |
| `OncePerCombat` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `Cooldown` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

**三条最容易踩的**：

1. **在 `attack` 上填 `AnimationState` 不会播**。想用 Bool 参数控制姿态，必须换成 `stance` 或 `dash`。
2. **在 `dead` 上填 `AnimationAction` 不会播**。死亡动画走 `MonsterAI` 表的 `deadParameter`，不走语义键。
3. **`Ranged` 列只对 `dash_attack` 有效**。`attack` 用近战形状、`ranged_attack` 用远程形状，是执行器自己定的，填这一列没用。

### 5.4 契约（违反会怎样）

| 违反 | 后果 |
|---|---|
| 执行器名拼错（`atack`） | **启动失败**：`uses unknown executor 'atack'` |
| 没有 `dead` 执行器的能力 | **启动失败**：`missing an ability using executor 'dead'` |
| `HitDelay ≥ Duration` | **不报错，但永远打不出伤害**——到 `Duration` 就结束了，命中判定还没到 |
| `Duration = 0` 且执行器是 `dead` | 正常（它就是永不结束） |

---

## 6 契约四：动作动画

### 6.1 两个字段，两种用法

| 字段 | 用于 | 典型绑定类型 | 谁播 |
|---|---|---|---|
| `AnimationAction` | **一次性动作**（挥砍、施法、警戒） | `Trigger` | 执行器 `Start` 时播一次 |
| `AnimationState` | **持续状态**（翻滚、姿态、护盾） | `Bool` | `Start` 打开，`Stop` 关闭 |

**都留空是合法的**——这一招不播任何专属动画，只做逻辑（比如纯位移的 `dash`）。

### 6.2 只能填这 11 个语义键

`AnimationAction` 只能从下面 8 个里选，`AnimationState` 只能从最后 2 个里选：

| 语义键 | 中文 | 该填在哪 |
|---|---|---|
| `action.melee` | 近战攻击 | Action |
| `action.heavy` | 重攻击 | Action |
| `action.pounce` | 扑击 | Action |
| `action.leap` | 跃击 | Action |
| `action.ranged` | 远程攻击 | Action |
| `action.spell` | 施法 | Action |
| `action.alert` | 警戒 | Action |
| `action.take_damage` | 受击 | Action |
| `state.roll` | 滚动 | State |
| `state.guard` | 防御姿态 | State |

> `move.speed` 是移动混合树用的连续量，由 `MonsterAI` 表配置，**不在能力表里填**。

### 6.3 动画表三列怎么填

| 列 | 填什么 | 规范 |
|---|---|---|
| `SemanticKey` | 上面 11 个之一 | 必须和能力表里填的**完全一致** |
| `BindingType` | `Trigger` / `Bool` / `State` | Action 用 `Trigger`，State 用 `Bool` |
| `ParameterName` | Animator Controller 里真实的名字 | **大小写不敏感**，但类型必须匹配 |

`BindingIndex` 只是行号，**跳号完全合法**（现有表里 Werewolf 就缺 B3）。

真实例子（Werewolf）：

| BindingIndex | SemanticKey | BindingType | ParameterName |
|---|---|---|---|
| 0 | `action.melee` | `Trigger` | `Melee` |
| 1 | `action.heavy` | `Trigger` | `Heavy` |
| 2 | `action.pounce` | `Trigger` | `Pounce` |
| 4 | `action.ranged` | `Trigger` | `RangedAttack` |
| 5 | `action.spell` | `Trigger` | `Spell` |
| 6 | `action.alert` | `Trigger` | `Alert` |
| 7 | `action.take_damage` | `Trigger` | `TakeDamage` |

### 6.4 三道关卡

| 关卡 | 检查什么 | 拦得住 |
|---|---|---|
| 导出时 | `BindingType` 是不是四个合法值之一 | 把 `Trigger` 写成 `Triggr` |
| 启动校验 | 能力表填的每个语义键，动画表里有没有对应行 | 漏配一行 |
| 运行时绑定 | 这个参数在 Animator 上存不存在、类型对不对 | 换了模型、参数改名 |

**任何一道不过，怪物都不生成**——不会带着半套绑定跑起来。

### 6.5 契约（违反会怎样）

| 违反 | 后果 |
|---|---|
| 能力表填了 `action.spell`，动画表没有这行 | **启动失败**：`missing animation binding 'action.spell' for ability 'werewolf.spell'` |
| `BindingType` 写成 `Bool`，但 Animator 上是 Trigger | **启动失败**：`animation profile validation failed` |
| `ParameterName` 写成 `Spell01`，Animator 里叫 `Spell` | **启动失败**：参数不存在 |
| `AnimationAction` 填了 `state.roll`（Bool 绑定） | 能启动，但会**把 Bool 置 true 后再也不关**，姿态永久卡住 |
| 在 `attack` 上填 `AnimationState` | 能启动、不报错、**永远不播** |

---

## 7 规则表与条件表：决定"什么时候用哪招"

### 7.1 规则表四列

| 列 | 填什么 |
|---|---|
| `TID` | 怪物 ID |
| `RuleIndex` | 优先级，**从 0 开始，数字小的先判**。可以跳号 |
| `Action` | 三选一：`ability` / `chase` / `idle` |
| `AbilityId` | 仅 `ability` 行填，指向能力表 |

**读法**：从上往下，**第一条"全部条件为真"的规则胜出，后面的不再看**。

所以规则表本质上是：

```
if (规则0 全部条件为真) 执行规则0;
else if (规则1 全部条件为真) 执行规则1;
...
else if (规则N) ...;
```

### 7.2 条件表四列 + 一个可选列

| 列 | 填什么 |
|---|---|
| `TID` | 怪物 ID |
| `RuleIndex` | 挂到哪条规则上 |
| `ConditionIndex` | 这条规则内的第几个条件，**从 0 递增，顺序有意义** |
| `ConditionId` | 条件名，见下表 |
| `ConditionValue` | 只有两个条件需要填 |

**同一 `RuleIndex` 下的所有条件必须同时为真**（AND 关系，没有 OR）。

### 7.3 六个条件

| ConditionId | 中文 | 需要 ConditionValue | 含义 |
|---|---|---|---|
| `has_target` | 有目标 | ❌ | 当前锁定了玩家 |
| `hp_below` | 血量低于 | ✅ 0~1 的比例 | 例如 `0.3` = 三成血以下 |
| `attack_cooldown_ready` | 攻击冷却完成 | ✅ 秒数 | 距上次"算作攻击"的招已过 N 秒 |
| `recently_hit` | 受击硬直中 | ❌ | 处于硬直计时内 |
| `is_dead` | 已经死亡 | ❌ | 血量为 0 |
| `always` | 始终成立 | ❌ | 兜底用 |

**空条件的规则 = 恒真**，这是 `chase` / `idle` 兜底规则能工作的原因。

### 7.4 三条规范

1. **每条 `ability` 规则第一个条件都写 `has_target`**。否则没目标时也会尝试出招，虽然会被距离判定拦下，但白白浪费判定。
2. **最后一条必须是 `idle`，且不带条件**。否则所有规则都不命中时，怪物会停在上一帧的行为里。
3. **`chase` 放在所有 `ability` 之后、`idle` 之前**。它负责填补"距离不在任何招式区间"的空档。

### 7.5 一个真实例子（Shell）

| RuleIndex | Action | AbilityId | 条件 |
|---|---|---|---|
| 0 | ability | `shell.hide` | `has_target` ∧ `hp_below(0.3)` |
| 1 | ability | `shell.melee` | `has_target` |
| 2 | ability | `shell.roll` | `has_target` |
| 3 | ability | `shell.ranged` | `has_target` ∧ `attack_cooldown_ready(2.0)` |
| 4 | chase | — | `has_target` |
| 5 | idle | — | （无） |

**注意 R1–R3 只有 `has_target`**——距离判断全部交给能力表的 `MinRange/MaxRange`。这是 2026-10-21 那次"门控去重"迁移的结果：**距离只写一处，写在能力表**。

---

## 8 八份配方（照着抄）

> 每份配方给的是"能力表一行 + 规则表一行 + 条件表若干行"。`怪物名` 换成你的前缀。

### 配方 A：近战普攻

```
能力表：
  AbilityId = 怪物名.melee     Slot = primary_attack     Executor = attack
  AnimationAction = action.melee    AnimationState = 留空
  Duration = 0.6    Cooldown = 0.9    HitDelay = 0.2    ← HitDelay 必须 < Duration
  DamageComboIndex = 0    MinRange = 0    MaxRange = 2.0    Speed = 0
  LockMovement = true    StopMovementAtEnd = true    OncePerCombat = false
  DamageTakenMultiplier = 1    CountsAsAttack = true    Priority = 0

规则表：Action = ability, AbilityId = 怪物名.melee
条件表：has_target
```

### 配方 B：远程攻击

```
能力表：
  AbilityId = 怪物名.ranged    Slot = ranged_attack    Executor = ranged_attack
  AnimationAction = action.ranged   AnimationState = 留空
  Duration = 0.6    Cooldown = 4    HitDelay = 0.3
  DamageComboIndex = 1    MinRange = 6    MaxRange = 11    Speed = 0
  LockMovement = true    CountsAsAttack = true

规则表：Action = ability, AbilityId = 怪物名.ranged
条件表：has_target ∧ attack_cooldown_ready(2.0)
```

> `Cooldown` 是"这招自身"的硬冷却，`attack_cooldown_ready` 是"整体攻击节奏"，两者都要填。前者防止连放，后者防止"远程和近战交替刷"。

### 配方 C：纯位移（翻滚 / 后跳）

```
能力表：
  AbilityId = 怪物名.roll     Slot = mobility     Executor = dash
  AnimationAction = 留空      AnimationState = state.roll
  Duration = 1.0    Cooldown = 4    HitDelay = 0
  DamageComboIndex = -1    MinRange = 3    MaxRange = 7    Speed = 8
  LockMovement = false    StopMovementAtEnd = true    CountsAsAttack = false

规则表：Action = ability, AbilityId = 怪物名.roll
条件表：has_target
```

> 三个要点：`DamageComboIndex = -1`（不造成伤害）、`CountsAsAttack = false`（不占用攻击节奏）、`MaxRange` 必须有限（`mobility` 属于近战类槽）。

### 配方 D：位移攻击（突进斩）

```
能力表：
  AbilityId = 怪物名.lunge    Slot = gap_close    Executor = dash_attack
  AnimationAction = action.leap    AnimationState = 留空
  Duration = 0.5    Cooldown = 5    HitDelay = 0.2
  DamageComboIndex = 3    Ranged = false    MinRange = 3    MaxRange = 8    Speed = 12
  CountsAsAttack = true

规则表：Action = ability, AbilityId = 怪物名.lunge
条件表：has_target
```

> `Ranged` 列**只有这个执行器会读**。突进过程中用近战形状判定填 `false`，用远程形状填 `true`。

### 配方 E：一次性演出（警戒 / 首次登场）

```
能力表：
  AbilityId = 怪物名.alert    Slot = alert    Executor = play_and_wait
  AnimationAction = action.alert    AnimationState = 留空
  Duration = 0.8    Cooldown = 0    HitDelay = 0
  DamageComboIndex = -1    MinRange = 0    MaxRange = 999999
  LockMovement = true    OncePerCombat = true    CountsAsAttack = false

规则表：Action = ability, AbilityId = 怪物名.alert
条件表：has_target
```

> `OncePerCombat = true` 是"整场只演一次"的唯一开关。放在规则表最前面（RuleIndex 小），否则永远轮不到。

### 配方 F：持续姿态（缩壳 / 减伤）

```
能力表：
  AbilityId = 怪物名.hide    Slot = defense    Executor = stance
  AnimationAction = 留空      AnimationState = state.guard
  Duration = 10    Cooldown = 0    HitDelay = 0
  DamageComboIndex = -1    MinRange = 0    MaxRange = 999999
  LockMovement = true    OncePerCombat = true
  DamageTakenMultiplier = 0.5    CountsAsAttack = false

规则表：Action = ability, AbilityId = 怪物名.hide
条件表：has_target ∧ hp_below(0.3)
```

> `DamageTakenMultiplier` **只有 `stance` 会读**，且退出时会自动恢复原值。填在别的执行器上无效。

### 配方 G：受击反应（必填）

```
能力表：
  AbilityId = 怪物名.hit     Slot = reaction.hit     Executor = play_and_wait
  AnimationAction = action.take_damage    AnimationState = 留空
  Duration = 0.3    Cooldown = 0    HitDelay = 0
  DamageComboIndex = -1    MinRange = 0    MaxRange = 999999
  LockMovement = true    CountsAsAttack = false

规则表：不需要！   ← 它不走规则表，由受击事件直接触发
```

> `Duration` 建议 ≤ `MonsterAI` 表的 `hitStunDuration`，否则硬直结束动画还没播完，会被下一招打断。

### 配方 H：死亡（必填）

```
能力表：
  AbilityId = 怪物名.dead    Slot = reaction.dead    Executor = dead
  AnimationAction = 留空（填了也不播）
  AnimationState = 留空
  Duration = 0    Cooldown = 0    DamageComboIndex = -1
  LockMovement = true    CountsAsAttack = false

规则表：不需要！   ← 由死亡事件直接触发
```

> 死亡动画走 `MonsterAI` 表的 `deadParameter`（Bool），不在这里配。

---

## 9 新增一只怪的 8 步

| 步骤 | 做什么 | 在哪 |
|---|---|---|
| 1 | 定 `TID`（如 2003），在 `MonsterAI` 表加一行 | MonsterAI |
| 2 | 在 `CharacterDefine` 加对应行（血量、防御、`SubType`） | CharacterDefine |
| 3 | 在 `AttackDefine` 加这只怪的伤害段（`SubType:0`、`:1`…） | AttackDefine |
| 4 | 填能力表：至少 攻击 + `reaction.hit` + `dead` | MonsterAbility |
| 5 | 填动画表：把第 4 步用到的每个语义键都映射上 | MonsterAnimation |
| 6 | 填规则表：按优先级从高到低，最后 `chase` + `idle` | MonsterBehavior |
| 7 | 填条件表：给每条 `ability` 规则挂条件 | MonsterBehaviorCondition |
| 8 | 跑 `Excel2Json.cmd`，进游戏生成一只，看 Console | — |

**顺序不能反**：第 5 步必须覆盖第 4 步用到的所有语义键，否则第 8 步启动失败。

---

## 10 报错对照表

Console 里看到这些，按下表改：

| 报错原文 | 原因 | 改哪里 |
|---|---|---|
| `no abilities configured` | 能力表这行怪没有任何行 | 能力表加行 |
| `no behavior rules configured` | 规则表没有这行怪 | 规则表加行 |
| `ability with empty id` | `AbilityId` 空着 | 补 ID |
| `duplicate ability id 'x'` | 同怪两行同 ID | 改名 |
| `uses unknown executor 'x'` | 执行器拼错 | 对照 7 个合法值 |
| `maxRange below minRange` | 最小 > 最大 | 对调 |
| `a negative minRange` | 最小值 < 0 | 改 0 |
| `uses a close-range slot but has no finite MaxRange` | 近战槽填了 999999 | 填真实距离 |
| `a negative timing or speed` | `Duration`/`Cooldown`/`Speed` 有负数 | 改 0 |
| `missing 'reaction.hit' ability` | 没配受击招 | 加配方 G |
| `missing an ability using executor 'dead'` | 没配死亡招 | 加配方 H |
| `unknown behavior action 'x'` | `Action` 写了别的 | 只能 ability/chase/idle |
| `behavior references missing ability 'x' or slot 'y'` | 规则引用了不存在的能力 | 补能力或改 ID |
| `behavior uses unknown condition 'x'` | 条件名拼错 | 对照 6 个合法值 |
| `missing animation binding 'x' for ability 'y'` | 能力用了语义键，动画表没配 | 动画表加行 |
| `missing animation state binding 'x'` | 同上，State 那一列 | 动画表加行 |
| `animation profile must define speedParameter and deadParameter` | `MonsterAI` 表两个参数名空着 | 补 `MoveSpeed` / `IsDead` |
| `CharacterDefine entry is missing` | 主体表没这只怪 | 加 CharacterDefine |
| `references missing attack 'SubType:index'` | 伤害段不存在 | 加 AttackDefine 行 |

---

## 11 填完自检 12 条

发布前逐条打勾：

- [ ] 1. 每只怪都有 `reaction.hit` 和 `dead` 两个能力
- [ ] 2. 所有 `AbilityId` 在同怪内不重复，且都是 `怪物名.xxx` 格式
- [ ] 3. 近战类槽（primary/secondary/heavy/gap_close/mobility）的 `MaxRange` 都是有限值
- [ ] 4. 所有 `HitDelay < Duration`（`dash` 和 `play_and_wait` 除外）
- [ ] 5. 所有 `DamageComboIndex ≥ 0` 的行，`AttackDefine` 里都有对应段
- [ ] 6. 能力表用到的每个 `AnimationAction` / `AnimationState`，动画表都有行
- [ ] 7. 动画表的 `BindingType` 与 Animator 上的参数类型一致
- [ ] 8. 规则表最后两行是 `chase` 和 `idle`，`idle` 无条件
- [ ] 9. 每条 `ability` 规则第一个条件是 `has_target`
- [ ] 10. 位移/姿态/演出的 `CountsAsAttack` 都是 `false`
- [ ] 11. 只在 `stance` 上填 `DamageTakenMultiplier`，只在 `dash`/`dash_attack` 上填 `Speed`
- [ ] 12. 规则表的 `AbilityId` 与 `Slot` 只填一个（默认填 `AbilityId`）

---

## 12 附：合法值速查

**执行器（7）**
```
attack  ranged_attack  dash  dash_attack  stance  play_and_wait  dead
```

**能力槽（11）**
```
primary_attack  secondary_attack  heavy_attack  gap_close  ranged_attack
mobility  defense  alert  special  reaction.hit  reaction.dead
```

**条件（6）**
```
has_target  hp_below  attack_cooldown_ready  recently_hit  is_dead  always
```

**动画语义（11）**
```
action.melee  action.heavy  action.pounce  action.leap  action.ranged
action.spell  action.alert  action.take_damage  state.roll  state.guard  move.speed
```

**绑定类型（3，`Float` 目前写不进去，别用）**
```
Trigger  Bool  State
```

**行为动作（3）**
```
ability  chase  idle
```

---

## 附录：本手册对应的代码位置

给需要核对的人：

| 内容 | 文件 |
|---|---|
| 能力字段定义 | `Assets/Scripts/AI/Enemy/Ability/Data/AbilityDefinition.cs` |
| 槽常量 | `.../Mapping/AbilitySlotIds.cs` |
| 执行器 ID | `.../Mapping/AbilityExecutorIds.cs` |
| 动画语义 | `.../Mapping/AnimationSemantics.cs` |
| 条件常量 | `.../Mapping/BehaviorConditions.cs` |
| 条件策略注册表 | `.../Registries/BehaviorConditionRegistry.cs` |
| 执行器策略注册表 | `.../Registries/AbilityExecutorRegistry.cs` |
| 执行器实现 | `.../Executors/*.cs` |
| 启动校验（20 条报错） | `.../Validation/MonsterAIDefinitionValidator.cs` |
| 查找与冷却 | `.../Runtime/EnemyAbilityRuntime.cs` |
| 编辑器下拉选项 | `Assets/Editor/MonsterAI/MonsterAIEditorOptions.cs` |
