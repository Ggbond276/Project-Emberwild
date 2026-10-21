# Project Emberwild

Unity 项目。第三方状态:早期开发。

---

## 目录结构

```
Assets/          Unity 资产与脚本
Packages/        Unity 包清单与已锁定的依赖
ProjectSettings/ Unity 项目设置
Data/            游戏数据(配置表 + JSON 产物 + 转换工具)
```

`Data/Tables/*.xlsx`、`Data/Data/*.txt`、`Data/json-excel/` 各司其职:

- `Data/Tables/*.xlsx`:策划维护的 Excel 源表(**不入库**,本地保留)。
- `Data/Data/*.txt` / `*.json`:`json-excel.exe` 生成的 JSON 产物,**入库**。
- `Data/json-excel/`:Excel → JSON 转换工具,**入库**(`EPPlus.xml`、`Newtonsoft.Json.xml` 文档被 `.gitignore` 排除)。

---

## 依赖安装

Newtonsoft.Json 由 Unity 官方 NuGet 包提供,**已在 `Packages/manifest.json` 中锁定**:

```
"com.unity.nuget.newtonsoft-json": "3.2.2"
```

协作者克隆仓库后在 Unity 中打开项目,Package Manager 会自动解析并安装该依赖,无需手动操作。

> 仓库**不包含**任何 NuGet 包产物。手动放入的 `Packages/Newtonsoft.Json.13.0.4/` 已被 `.gitignore` 忽略,
> 且与 Unity 官方维护的 3.2.2 版本冲突,不要放在 `Packages/` 目录下。

### Excel → JSON 转换工具(`Data/json-excel/`)

Windows 下双击 `Excel2Json.cmd` 或在命令行执行:

```cmd
Data\Excel2Json.cmd
```

工具会扫描 `Data/Tables/*.xlsx` 并把产物输出到 `Data/Data/`。

> `Data/json-excel/` 已入库,无需额外安装;只运行 `EPPlus.dll` + `json-excel.exe` + `Newtonsoft.Json.dll`(`EPPlus.xml` / `Newtonsoft.Json.xml` 文档不入库)。

`Data/Data/_Excel2Json.state.json` 保存最近一次 Excel 导入后的 JSON 哈希。
如果工作台修改过 JSON，`Excel2Json.cmd` 会阻止 Excel 静默覆盖，并提示先处理
这两份数据的冲突。

---

## 数据流

```
Data/Tables/*.xlsx  ──(Excel2Json.cmd)──>  Data/Data/*.json
                                              │
                                              ▼
                                  DataManager.Load() 反序列化
                                              │
                                              ▼
                              CharacterEntity.Initialize(define)
                                              │
                                              ▼
                                  CharacterData 运行时数据
```

---

## 开发约定

提交前请确认:

- `.meta` 文件与资产文件**同步新增/删除**。
- `Data/Tables/*.xlsx` 与 `Data/Data/*.json` 内容一致(改表后跑一次 `Excel2Json.cmd`)。
- 不提交 `Library/`、`Temp/`、`Logs/`、`UserSettings/`、`obj/`、`Build/`。
