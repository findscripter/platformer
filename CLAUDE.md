# CLAUDE.md

本文件为 Claude Code (claude.ai/code) 在此仓库中工作时提供指导。

## 项目概述

Unity 6 (6000.5.0f1) 2D 平台跳跃游戏，采用状态机架构、对话系统、交互框架和存档/读档功能。

## 核心架构

### 启动流程
- **Boot 场景**: 入口点，始终通过 `BootPlayModeRedirect` 编辑器脚本首先加载
- 在编辑器播放模式下，活动场景名称在重定向到 Boot 之前保存到 `BootSession.EntrySceneName`
- Boot 场景实例化 `PersistentRoot`（包含 `GameLoop` + 管理器）并加载入口场景（MainMenu、PlayerGuide 或 Gameplay）
- `GameLoop.Begin()` 用适当的初始状态启动状态机

### 状态机
通过 `GameStateMachine` 进行中心化游戏状态管理，包含以下状态：
- `MainMenu`（主菜单）、`PlayerGuide`（新手引导）、`Loading`（加载中）、`Playing`（游戏中）、`Paused`（暂停）、`PlayerDead`（玩家死亡）、`Respawning`（重生）、`LevelClear`（关卡完成）、`Result`（结算）、`Dialogue`（对话中）
- 所有状态实现 `IGameState` 接口（Enter/Exit/Update/FixedUpdate）
- 状态转换通过 `GameContext.StateMachine.ChangeState()` 进行

### GameContext
依赖注入容器，持有所有运行时引用：
- **管理器**: `InputManager`、`UIManager`、`SceneTransitionManager`、`LevelManager`、`InteractionManager`、`DialogueManager`、`DialogueProgressService`、`SaveSystem`
- **玩家**: `PlayerController`、出生点
- **运行时标志**: `IsLevelLoaded`、`IsPlayerDead`、`IsLevelClear`

### 事件系统
通过 `EventCenter` 实现类型化事件总线：
- 订阅: `EventCenter.Subscribe(GameEvents.DialogueStarted, handler)`
- 发布: `EventCenter.Publish(GameEvents.DialogueStarted, dialogueData)`
- 事件定义在 `GameEvents.cs` 和 `GameEventPayloads.cs` 中

### 对话系统
基于节点的对话系统，使用 ScriptableObject 数据：
- **节点类型**: `TextNode`（文本）、`ChoiceNode`（选择）、`InputNode`（输入）、`EventNode`（事件）、`EndNode`（结束）
- `DialogueManager` 运行对话并触发事件
- `DialogueProgressService` 跟踪进度（标志位、已访问节点、NPC 一次性状态）
- **事件动作**: `GiveItemAction`（给予物品）、`OpenDoorAction`（开门）、`PlayAnimationAction`（播放动画）、`SetDialogueFlagAction`（设置对话标志）、`LoadSceneEventAction`（加载场景）

### 交互框架
- `IInteractable` 接口（NPC、门、开关、场景转换）
- `InteractionManager` 跟踪附近的可交互对象并处理焦点
- 玩家按下交互键 → `InteractionManager.TryInteract()` → 调用 `IInteractable.OnInteract()`

### 存档系统
- 基于 JSON 的存档，保存到 `Application.persistentDataPath/game_save.json`
- `GameSaveData` 包含场景名称、对话进度、收集品状态
- `SaveSystem.Save()` 捕获当前状态；`SaveSystem.Load()` 恢复状态

## Unity 6 API 要求

**查找对象** — 使用 Unity 6 API，不带 `FindObjectsSortMode`：
```csharp
// ✅ 正确
Object.FindFirstObjectByType<T>();
Object.FindObjectsByType<T>(FindObjectsInactive.Include);

// ❌ 错误 — FindObjectsSortMode 已废弃
Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
```

**TextMeshPro** — 使用 `textWrappingMode` 替代废弃的 `enableWordWrapping`：
```csharp
// ✅ 正确
text.textWrappingMode = TextWrappingModes.NoWrap;
text.textWrappingMode = TextWrappingModes.Normal;

// ❌ 错误 — enableWordWrapping 已废弃
text.enableWordWrapping = false;
```

**ShaderLab** — `[Header]` 仅接受单词标识符（无空格、无引号）：
```hlsl
// ✅ 正确
[Header(VerticalFade)]
_FadeTop ("Fade Top", Range(0, 1)) = 1

// ❌ 错误 — 会导致解析错误
[Header(Vertical Fade)]
[Header("Vertical Fade")]
```

不要在 shader Properties 块中使用 `[Tooltip]`（仅限 C#）。

## 编辑器脚本

`Assets/Game/Scripts/Editor/` 文件夹包含用于生成预制体、场景层级和 UI 布局的设置工具。这些脚本在编辑模式下运行并自动配置项目结构。除非有明确指示，否则不要修改它们。

## 依赖包

核心依赖：
- `com.unity.render-pipelines.universal` (URP 17.5.0)
- `com.unity.inputsystem` (1.19.0)
- `com.unity.cinemachine` (3.1.6)
- `com.unity.ugui` (2.5.0) — 包含 TextMeshPro

## 技术栈

- **引擎**: Unity 6000.5.0f1
- **语言**: C#
- **构建系统**: Unity Build Pipeline
- **资源管线**: Unity Asset Import Pipeline + Addressables

## 引擎版本参考

@docs/engine-reference/unity/VERSION.md

## 开发笔记

- 玩家物理使用 `Rigidbody2D`，带自定义地面检测和土狼时间/跳跃缓冲
- 动画状态由 `PlayerAnimationStateController` 控制（某些动画期间锁定移动）
- 收集品通过 `CollectibleTracker` 单例跟踪
- 关卡目标触发状态变更到 `LevelClear`，`SpikeTrap`/危险物设置 `PlayerDead`
- 对话期间的场景转换使用 `GameLoop.LoadSceneForDialogue()` 的挂起/恢复流程
