# Project Emberwild Development Rules

## C# And Unity Architecture

- Use a Hub & Slots structure for complex Unity components.
- All C# files under `Assets/Scripts/AI` must follow the region and layout rules in this document.
- Keep one primary top-level type per source file.
- Keep namespaces and behavior stable during structural refactors unless the task explicitly changes them.
- Place component references, runtime state, and public API on the owning component instead of using unrelated global state.
- Resolve required components defensively. Auto-attach safe adapters when appropriate; otherwise fail with a clear Chinese error message.

## Field Regions

Fields and properties must be grouped before method regions and must not be scattered between methods.

- Put a concise Chinese group comment at the start of each field region.
- Use aligned inline comments when a field's role is not obvious.
- Group public properties under short numbered comments: blackboard/data, component exposure, runtime state, computed properties.
- Match the user's requested visual layout exactly when an example is supplied.
- For complex runtime components, prefer these region names when they accurately describe the class: `枚举与定义`, `检视面板调试`, `本地核心组件`, `运行时逻辑与状态`, `公共属性与接口`.
- For specialized classes, use accurate domain names. For example, a spawner uses `刷怪配置`, `刷怪运行时状态` and `怪物生成与回收操作`.

## Method Regions

Methods must be grouped by actual lifecycle and responsibility.

- Use `Unity 生命周期与装配`, `每帧心跳与大脑决策`, `状态机切换指令` and `外部事件响应` only when those descriptions are true for the class.
- Do not label every class as a state machine or frame-driven brain.
- A spawner uses spawn-loop, validation, spawn/recycle and event regions.
- An ability executor uses ability-progress and ability-start/end regions.
- A repository uses definition-loading and assembly regions.
- A pure data class may only need data and constructor regions.

## Documentation

- Only methods may use a `/// <summary>` comment.
- Use exactly one short Chinese sentence describing the direct effect of calling the method.
- Do not use `///` comments on classes, fields, or properties.
- Do not add `/// <param>` or `/// <returns>` sections unless the user explicitly asks for full API documentation.
- Example: `/// <summary>将 EnemyAnimator 挂载到角色 Animator 上。</summary>`
- Use normal comments only for non-obvious sequencing or invariants.

## Refactoring

- Preserve existing runtime behavior during organization-only changes.
- Do not move user changes into unrelated files or remove existing defensive checks without explicit approval.
- Keep generated data flow explicit: source table, global index, shared runtime definition, per-instance runtime state.
