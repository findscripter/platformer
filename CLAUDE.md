# CLAUDE.md

本文件为 Claude Code (claude.ai/code) 在此仓库中工作时提供指导。

## 项目概述

Unity 6 (6000.5.0f1) 2D 平台跳跃游戏，采用状态机架构、对话系统、交互框架和存档/读档功能。

## 当前工程速览（2026-09-06 静态核对）

- 游戏方向：梦境叙事 + 平台跳跃 + 山海经塔罗改变关卡规则。
- 代码流程：主菜单 → 梦泡/腓腓演出 → 写梦及至多一次补问 → 22 抽 4 → A/B 区域的 T1–T4 → 通关回响与梦笺 → 结算。
- Build Settings 中有 `Boot`、`MainMenu`、`PlayerGuide`、`Loading`、`Gameplay` 五个场景。A/B 是 `Gameplay` 中动态生成的两个区域，并非两个 Unity Scene；回响由 `EchoSpaceController` 动态创建 UI。
- 当前 `PlayerGuide.unity` 挂载的是 `Assets/Game/Scripts/PlayerGuide/Controllers/PlayerGuideController.cs`，通过 Models / Views / Controllers 拆分流程。旧 `PlayerGuideFlowController` 和 `PlayerGuideFlowControllerV2` 仍在源码中，但未在五个正式场景和现有 Prefab 中找到挂载引用。
- 关卡入口：`GameplaySceneBridge.Apply()` → `DreamremainsLevelBootstrap.Build()`；平台、敌人、检查点、塔罗点和镜头区域数据位于 `Assets/Game/Scripts/Level/DreamremainsLevelData.cs`。
- 塔罗入口：`TarotDrawController` 抽牌；`TarotCatalog` 定义 22 张牌及正逆位 E00–E18；`TarotResultData` 保存本局激活、隐藏路、覆写顺序、9 个 Zone 快照和折叠充能；`TarotEffectApplier` 与各机关组件应用效果。
- 本地需求入口：`Dreamremains_关卡程序施工说明_v4.md`、`Dreamremains_关卡路线与机制施工图_v4.html`、塔罗规则 XLSX；引导参考在 `design/figma_storyboard_build_spec.md` 与 `design/figma_frames/`。这些设计资料被 `.gitignore` 排除，远端不能仅靠 README 还原需求。
- 需求存在版本差异：旧分镜写首张固定应龙，当前代码为四张随机且正逆位独立随机。读旧需求时先对照当前规则表，不能把旧稿中的待开发条目直接当作现状。

### 已识别缺口与核验边界

- `GuideArt.Load()` 与 `DreamremainsLevelBootstrap.LoadArt()` 的美术读取仅存在于 `UNITY_EDITOR` 分支，Player 构建中返回 null；动态关卡与回响空间的相关美术需改为运行时可用的资源引用后再验收打包画面。
- `DreamInputValidator` 用长度、正则和关键词判定补问；`DreamNoteWriter.Compose()` 用固定模板拼接梦境与牌名，尚不是 AI 内容生成。
- `GameSaveData` 当前仅有场景、出生点 ID 和对话数据；未包含梦境文字、塔罗运行状态和收集品数据。局内死亡保留塔罗状态不等于退出后可恢复整局。
- 未发现项目自己的 NUnit / UnityTest 测试用例；`Assets/Game/Scripts/Debug/MainRouteWalker.cs` 是编辑器内的主线行走诊断工具，不能据其存在认定已完成通关验收。
- 本次核对为代码、场景配置、历史日志和已有截图检查，未启动 Unity、未新建构建或执行完整实机流程。工作区已有多处未提交修改，应保留。

## 核心架构

### 本机 MCP（2026-09-06 已连通）
- Unity 插件为 `com.coplaydev.unity-mcp`，使用本地 HTTP：`http://127.0.0.1:8080/mcp`。
- Codex 用户配置：`C:/Users/13185/.codex/config.toml` 的 `mcp_servers.unityMCP`；项目 `.mcp.json` 同步提供 `unityMCP`，供读取该格式的客户端使用。
- Unity EditorPrefs 已启用 `MCPForUnity.AutoStartOnLoad`，uvx 路径为 `C:/Users/13185/.local/bin/uvx.exe`，服务器来源固定为 `mcpforunityserver==10.2.1b20260905165837`。服务由 Unity 插件在后台启动，无需手动保持终端。
- 连通性验证：实例 `platformer@a47b5e5ca288891b`，`manage_scene/get_active` 返回 `MainMenu`，`read_console` 错误查询为 0 条；这是 MCP 连接检查，不是游戏完整流程验收。
- Codex 新增 MCP 配置后，若当前任务的工具列表尚未出现 Unity，重启 Codex 以重新加载连接。
- Figma：Codex 原有 `figma-mcp` 配置的 API key 经官方 API 返回 `Token has expired`；尚需用户更新授权。项目中的 `@vkhanhqui/figma-mcp-go` 是另一套桌面插件桥接方式，不应与 API key 模式混为一谈。不要把密钥写入项目文件或聊天。

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
- **资源管线**: Unity Asset Import Pipeline + Resources / 序列化资源引用（当前 manifest 未声明 Addressables）

## 引擎版本参考

@docs/engine-reference/unity/VERSION.md

## 开发笔记

- 玩家物理使用 `Rigidbody2D`，带自定义地面检测和土狼时间/跳跃缓冲
- 动画状态由 `PlayerAnimationStateController` 控制（某些动画期间锁定移动）
- 收集品通过 `CollectibleTracker` 单例跟踪
- 关卡目标触发状态变更到 `LevelClear`，`SpikeTrap`/危险物设置 `PlayerDead`
- 对话期间的场景转换使用 `GameLoop.LoadSceneForDialogue()` 的挂起/恢复流程
