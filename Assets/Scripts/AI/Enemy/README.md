# 怪物 AI 快速上手指南（Enemy AI）

> 本文档让你**不读任何其他文档**，也能独立新建一只怪物 AI。  
> 详细原理见 `Docs/Planing/18`（架构）、`19`（策划）、`20`（手册）。



---

## 一、核心概念（30 秒理解）

怪物 AI = **通用积木（复用） + 特殊积木（每怪实现） + 配置（结构复用、数据独立）**。

```text
通用积木（挂上就能用，不改）：EnemyBrain / EnemyPerception / CustomNavAgent / EnemyCombat / EnemyEntity
特殊积木（每怪实现一份）：    Animator（实现 IEnemyAnimator）+ 特殊状态（可选）
配置（每怪一份资产）：        EnemyAIConfig 结构（统一）+ ShellAI.asset / WolfAI.asset（独立数据）
```

**改 `WolfAI` 的参数绝不影响 `ShellAI`**——它们是两份独立资产。

---

## 二、目录结构

```text
Assets/Scripts/AI/Enemy/
├─ EnemyBrain.cs        通用·大脑(决策驱动 + 状态机容器)
├─ EnemyPerception.cs   通用·感知(视野扫描写黑板)
├─ EnemyCombat.cs       通用·攻击(判定 + 发事件)
├─ EnemyEntity.cs       通用·数据桥接(读数据表)
├─ EnemyBlackboard.cs   通用·黑板(共享数据)
├─ EnemyAIConfig.cs     配置·统一结构(所有怪共用字段定义)
├─ EnemyStateFactory.cs 通用·状态工厂(状态零件库 + 注册)
├─ IEnemyAnimator.cs    抽象·动画接口
├─ ShellAnimator.cs     特殊·Shell 动画(示例)
├─ WolfAnimator.cs      特殊·Wolf 动画(示例)
└─ State/               通用·8 个状态类(Idle/Chase/攻击/…)

Assets/Scripts/AI/BT/
├─ BTNode.cs / NodeState.cs / Selector.cs / Sequence.cs / Condition.cs / ActionNode.cs   行为树框架
├─ BTNodeDef.cs         配置·节点定义(BTNodeType + ConditionType + BTNodeDef)
├─ ConditionFactory.cs  通用·条件类型 → 求值函数
└─ BTBuilder.cs         通用·配置树 → 运行时树
```

---

## 三、配置项说明

### 3.1 `EnemyAIConfig` 字段（执行参数）

| 分组    | 字段                                                                | 含义                      |
| ----- | ----------------------------------------------------------------- | ----------------------- |
| 感知    | `viewRange` / `loseTargetRange` / `scanInterval`                  | 视野 / 丢失距离 / 扫描间隔        |
| 移动    | `runSpeed` / `turnSpeed` / `stoppingDistance`       | 奔跑速度 / 转向 / 刹车   |
| 时长/速度 | `hitstunDuration` / `attackDuration` / `rollSpeed` / `rollDuration` / `hideDuration` | 受击硬直 / 攻击硬直 / 滚动速度 / 滚动时长 / 缩壳时长 |
| 决策树   | `decisionTree`                                                    | 见 3.2                   |

### 3.2 决策树（决策参数在节点里）

`decisionTree` 是一棵 `Selector`，自上而下 = 优先级从高到低，每分支 = `Sequence(条件…, 动作)`。

- **节点类型**：`Selector` / `Sequence` / `Condition` / `Action`
- **条件类型**（`ConditionType`）：

| 条件                                          | 参数             | 含义        |
| ------------------------------------------- | -------------- | --------- |
| `Always`                                    | —              | 恒真（兜底）    |
| `IsDead`                                    | —              | 已死亡       |
| `RecentlyHit`                               | —              | 受击硬直中(读 hitstunDuration)     |
| `HpBelow`                                   | p1             | 血量低于阈值    |
| `InRange`                                   | p1=min, p2=max | 距离在区间     |
| `CooldownAttackReady` / `CooldownRollReady` | p1             | 攻击/滚动冷却就绪 |
| `HasTarget`                                 | —              | 能看到目标     |
| `NotHidden`                                 | —              | 未缩壳过      |

- **动作**（`Action`）即一个状态：`Idle` / `Chase` / `AttackMelee` / `AttackRanged` / `RollChase` / `Hide` / `Hit` / `Dead`

> 决策参数（距离/冷却/阈值）直接填在条件节点的 `p1/p2`，不要放进 `EnemyAIConfig` 字段——避免两处同步。

---

## 四、新建怪物最小步骤（以 Wolf 为例）

### 第 1 步：挂通用组件

复制一个现有怪物 prefab（如 `Shell.prefab`）→ 改名 `Wolf.prefab`，换网格/材质，保留这些通用组件：

```text
Rigidbody + CapsuleCollider + Animator(Unity 内置)
EnemyBrain + EnemyPerception + CustomNavAgent + EnemyCombat + EnemyEntity
```

### 第 2 步：写专属 Animator（实现 `IEnemyAnimator`）

新建 `WolfAnimator.cs`，把语义动作翻译成 Wolf 自己的 Animator 参数：

```csharp
using UnityEngine;

namespace Emberwild.AI.Enemy
{
    public class WolfAnimator : MonoBehaviour, IEnemyAnimator
    {
        [SerializeField] private Animator _animator;
        private int _speedHash, _attackHash, _deadHash, _hitHash;

        private void Awake()
        {
            if (_animator == null) _animator = GetComponent<Animator>();
            _speedHash = Animator.StringToHash("WolfSpeed");   // 改成 Wolf 实际参数名
            _attackHash = Animator.StringToHash("WolfBite");
            _deadHash = Animator.StringToHash("WolfDead");
            _hitHash = Animator.StringToHash("WolfHit");
        }

        public void SetMoveSpeed(float speed) { if (_animator) _animator.SetFloat(_speedHash, speed); }
        public void PlayAttack() { if (_animator) _animator.SetTrigger(_attackHash); }
        public void PlayRangedAttack() { /* Wolf 无远程,空实现 */ }
        public void SetRoll(bool roll) { /* Wolf 无滚动,空实现 */ }
        public void SetHidden(bool hidden) { /* Wolf 无缩壳,空实现 */ }
        public void PlayTakeDamage() { if (_animator) _animator.SetTrigger(_hitHash); }
        public void SetDead(bool dead) { if (_animator) _animator.SetBool(_deadHash, dead); }
    }
}
```

> Wolf 没有的能力（远程/滚动/缩壳）空实现即可；只要决策树里不配这些分支，它们永远不会被调用。

### 第 3 步：建专属配置资产

1. `Project 右键 → Create → AI → EnemyAIConfig`，命名 `WolfAI`。
2. 填 Wolf 自己的执行参数（与 Shell **独立**），例如：`viewRange=10, runSpeed=6`。
3. 配 Wolf 的决策树（没有远程/滚动/缩壳，只有追+咬+受击+死亡）：

| # | 节点       | 配置                                                                            |
| - | -------- | ----------------------------------------------------------------------------- |
| 1 | Sequence | Condition(IsDead) → Action(Dead)                                              |
| 2 | Sequence | Condition(RecentlyHit) → Action(Hit)                                  |
| 3 | Sequence | Condition(HasTarget) + Condition(InRange, p1=0, p2=2.0) → Action(AttackMelee) |
| 4 | Sequence | Condition(HasTarget) → Action(Chase)                                          |
| 5 | Action   | Action(Idle)                                                                  |

### 第 4 步：挂 Animator + 引配置 + 配数据

1. `Wolf.prefab`：删除 `ShellAnimator`，挂 `WolfAnimator`；`EnemyBrain` 的 `Config` 槽拖入 `WolfAI`。
2. `Data/Data/CharacterDefine.txt` 加条目（`SubType` 与 `AttackDefine` 的 `ComboGroup` 一致）：

```json
  "2002": { "TID": 2002, "Name": "Wolf", "MainType": "Monster", "SubType": "Wolf",
            "Resource": "Prefabs/Monsters/Wolf", "HP": 80.0, "AD": 12.0, "DEF": 8.0 }
```

3.（若 Wolf 要攻击）`AttackDefine.txt` 加 `ComboGroup: "Wolf"` 的近战段。  
4\. 把 `Spawner` 的 `Enemy Prefab` 换成 `Wolf.prefab`，Play 验证。

---

## 五、完成清单（自检）

- [ ] Wolf 能发现玩家并追击
- [ ] 贴脸会咬、玩家掉血
- [ ] 被打会受击硬直、打死播死亡动画
- [ ] 改 `WolfAI` 参数不影响 `ShellAI`
- [ ] 全程**没改** `EnemyBrain` / 状态机 / 行为树框架代码

> 若 Wolf 有特有行为（如嚎叫），额外做两件事：① 写 `HowlState : IEnemyState`；② `EnemyStateFactory.Register(EnemyStateType.Howl, () => new HowlState())` + 在 `EnemyStateType` 枚举加 `Howl` + 决策树加对应分支。框架其余零改动。
