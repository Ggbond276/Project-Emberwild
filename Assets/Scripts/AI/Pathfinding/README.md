# 寻路系统使用文档

本系统让怪物自动避开障碍物,追着玩家跑。不需要懂 A* 算法,看完这篇就能用。

---

## 第一步:确认 Tag 和 Layer 已经配好

打开 `Edit → Project Settings → Tags and Layers`,确认以下 Layer 存在:

- `Obstacle`(8 号位)—— 静态障碍物(墙、石头、不可破坏的地形)
- `Player`(3 号位)—— 玩家角色(用于怪物雷达扫描)

没有就在对应空位手动添加。

---

## 第二步:给障碍物打 Layer

场景里所有"不能踩"的东西(墙、石头、箱子),Inspector 右上角 Layer 下拉,改成 `Obstacle`。

> Y 轴方向的浮空障碍也要打,系统在节点中心做球形检测,浮空障碍会被自动识别为不可走。

---

## 第三步:初始化寻路网格(每个场景只写一次)

在场景启动脚本里(本项目是 `TestRunner`),`Start` 方法里加一行:

```csharp
GridManager.Instance.Init(
    origin:     Vector3.zero,           // 网格左下角(以场景地形的左下角为准)
    worldSize:  new Vector2(20, 20),    // 长宽,单位米
    radius:     0.1f,                   // 单格半径,0.1 表示 0.2 米一格
    clearanceRadius: 0.4f,              // 角色避障半径,不要跟随单格半径缩小
    mask:       LayerMask.GetMask("Obstacle")
);
```

`TestRunner` 已经在 Inspector 暴露了这五个参数
(`_gridOrigin / _gridWorldSize / _nodeRadius / _agentRadius / _obstacleMask`),
直接拖值就行,**不用改代码**。

参数建议:
- `worldSize`:等于或略大于场景可走区域的 XZ 尺寸
- `nodeRadius`:控制格子密度,`0.1` 表示 0.2 米一格
- `agentRadius`:控制障碍物安全距离,通常按怪物碰撞体半径配置
- 缩小 `nodeRadius` 不会自动缩小 `agentRadius`，两者职责不同

---

## 第四步:配置怪物 Prefab

把怪物 Prefab 打开,Inspector 确认以下组件:

| 组件 | 必须做的配置 |
|---|---|
| `Rigidbody` | `Use Gravity` 勾上;`Is Kinematic` **不要勾**;`Constraints` 冻结 X 和 Z 轴旋转 |
| `Collider` | `CapsuleCollider` 调成怪物大小 |
| `CustomNavAgent` | 挂上即可,默认参数已能用 |
| `EnemyAI` | 挂上;Inspector 里的 `Player Layer` 拖成 `Player` |

---

## 第五步:配置刷怪器(可选,想刷怪才用)

场景里新建空 GameObject,挂 `EnemySpawner`,Inspector 填:

- `Enemy Prefab`:拖怪物 Prefab
- `Spawn Interval`:3 秒一只
- `Max Enemies`:同屏上限,超过就不再生成
- `Spawn Radius`:刷怪器周围多大圆圈里随机生成

---

## 怎么驱动怪物追玩家

**什么都不用写**。

`EnemyAI` 是个状态机:
- **Idle** (发呆):每 0.5 秒扫一次雷达(圆形范围,过滤 `Player` 层)
- 发现玩家 → 切到 **Chase**,持续把玩家位置传给 `CustomNavAgent`
- 玩家跑出视野 → 切回 **Idle**,自动刹车

`CustomNavAgent` 拿到目标后:
- 定时向 A* 引擎请求路径(默认 0.2 秒算一次)
- 离下一个航点足够近时提前切下一个点，最多提前一个节点直径
- 距离目标 < 1.5 米自动 Stop

---

## 调参指南(给策划/关卡)

打开怪物 Prefab 的 Inspector,看 `Custom Nav Agent` 区块:

| 想达到的效果 | 改哪个参数 | 改成多少 |
|---|---|---|
| 怪物跑得更快 | Move Speed | 提到 6~8 |
| 怪物更灵活(转身不僵硬) | Turn Speed | 提到 12~15 |
| 远程怪不要贴脸才停 | Stopping Distance | 改成 5~8 |
| 同屏怪太多卡顿 | Path Update Interval | 改成 0.5 |
| 怪物过墙角抖 | Next Waypoint Distance | 改成 0.6~0.8 |

`Enemy AI` 区块:

| 想达到的效果 | 改哪个参数 | 改成多少 |
|---|---|---|
| 怪物视野更远 | Sight Range | 提到 15~20 |
| 同屏怪多降低 CPU | Scan Interval | 改成 0.8~1.0 |

---

## 常见问题

**Q: 怪物站着不动?**
A: 99% 是障碍物 Layer 没打。选中障碍物,看 Inspector 右上角 Layer 是不是 `Obstacle`。

**Q: 怪物穿墙?**
A: 网格初始化时没扫到墙。检查 `TestRunner` 的 `_obstacleMask` 是不是 `Obstacle` Layer。

**Q: 怪物追到一半停了?**
A: 玩家跑出 `sightRange` 范围,怪物回 Idle。加大 `sightRange` 或缩短 `scanInterval`。

**Q: 怪物拐弯卡住 / 转弯抖?**
A: 把 `Next Waypoint Distance` 从 0.5 调到 0.7~0.8。

**Q: 怎么让怪物停下来做别的事(比如攻击)?**
A: 在 `EnemyAI` 加新状态(Attack),进入攻击时调用 `agent.Stop()`,退出时再 `SetDestination`。

**Q: 怎么让怪物走预设巡逻路线?**
A: 写一个 `PatrolState` 协程,依次 `SetDestination(waypoint1)` → 等到达 → `SetDestination(waypoint2)`,到达检测看 `agent.VelocityMagnitude < 0.1f`。

---

## 调试小技巧

- 选中场景里正在跑的怪物,Scene 视图会自动画一条**黑色折线**和小节点框。
  节点框尺寸跟随 `NodeRadius`，折线表示怪物脑内的规划路径。怪物卡墙时看这条线,
  一眼看出问题。
- 选中带 `EnemyAI` 的实体,Scene 视图画**黄色线框球**——怪物视野范围。
- `TestRunner` 的启动日志里有 `[2/4] 初始化环境系统(寻路网格)...` 这一行,Console 跟着看,网格生成失败会报错。
