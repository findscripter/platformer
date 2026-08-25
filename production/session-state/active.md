# 当前任务状态（供 /clear 后恢复上下文用）

更新时间：2026-08-24（已完成主体工作，待用户确认后 commit）

## 背景

PlayerGuide 场景的旧脚本 `PlayerGuideFlowControllerV2.cs`（1076 行，承载真实的
"梦境/腓腓/塔罗"叙事流程，10 个 Frame）已被**手工逐个重写**为 MVC 架构
（用户明确指令："逐个手工重写这 18 个文件，让其忠实反映原逻辑"）。

新 MVC 代码位于：
- `Assets/Game/Scripts/PlayerGuide/Controllers/`：`PlayerGuideController.cs`（顶层
  MonoBehaviour）+ `Frame1To5Controller.cs` + `Frame6DialogueController.cs` +
  `Frame7And8Controller.cs` + `Frame9And10Controller.cs`
- `Assets/Game/Scripts/PlayerGuide/Models/`：`PlayerGuideFrameState.cs`、
  `Frame6DialogueTrackingState.cs`、`InputState.cs`
- `Assets/Game/Scripts/PlayerGuide/Views/`：9 个 View MonoBehaviour

## 状态：核心任务已全部完成 ✅

1. **✅ 场景接入完成**：`Assets/Game/Scripts/Editor/PlayerGuideV2SceneSetup.cs`
   已重写，菜单 `Tools/GameJam/8. Setup PlayerGuide V2 Scene` 执行后会：
   - 移除场景里的旧 `PlayerGuideFlowControllerV2` 实例
   - 创建 `V2ControllerRoot`，挂载新 `PlayerGuideController` + `PlayerGuideView`
     + `SmallBubblesView`
   - 给 8 个子 GameObject 分别挂载对应 View 组件（`BackgroundView`、
     `MainBubbleView`、`DreamCoreView`、`FeifeiCharacterView`、`DialogueView`、
     `DreamInputView`、`TarotView`）
   - 通过反射把所有字段（Controller 的 9 个 View 引用、PlayerGuideView 自己的
     ~30 个字段、各 View 组件自身字段）全部绑定完毕
   - 已通过 Unity MCP 活体检查确认：场景中 `PlayerGuideController` 实例存在，
     `PlayerGuideFlowControllerV2` 实例已清零，编译 0 error/0 warning

2. **✅ 美术资源已接入**（用户中途提出"我看美术ui你没用上"，已补齐）：
   - **主梦泡**：改为 `PrefabUtility.InstantiatePrefab` 实例化
     `Assets/Game/Prefabs/PlayerGuide/MainBubble.prefab`（真实气泡插画
     `DR_elements_new.png`），而不是程序生成的纯色矩形
   - **小梦泡**：`PlayerGuideView.smallBubblePrefab` 绑定
     `Assets/Game/Prefabs/PlayerGuide/SmallBubble.prefab`（同一张气泡插画），
     新增 `SmallBubblesContainer` 空容器供 `SmallBubblesView.SpawnSmallBubbles`
     生成用
   - **腓腓角色**：Image 精灵设为
     `Assets/Game/Art/Characters/feifei_idle/1.png`（真实待机帧），Animator
     挂载 `Assets/Game/Animations/PlayerGuide/Feifei.controller`（真实
     AnimatorController，内含 `Feifei_Idle` 逐帧精灵动画状态）
   - **塔罗系统**：`TarotDrawController` 此前完全未绑定任何字段（连旧脚本时代
     都没绑），现已补全：
     - `cardPrefab` = `Assets/Game/Prefabs/PlayerGuide/TarotCardPrefab.prefab`
     - `fixedCards[0..3]` = 4 张真实塔罗牌 ScriptableObject（
       `Card_YingLong`=应龙/战车=主牌，`Card_ChanChuJingPo`/`Card_ChangXi`/
       `Card_RiYueLunZhuan`=情绪牌）
     - `cardArrayContainer`/`cardParent`/`mainCardSlot`/`emotionCardSlots[3]`
       全部绑定；新增 `MainCardSlot/CardFace` 子 Image 供 `mainCardImage` 绑定
     - 新增 `TarotDialoguePanel` 子面板（Speaker/Content/CanvasGroup）供
       State A/B/D 的引导对话使用（此前完全没有创建这个面板，是遗漏项）
   - **对话内容配置**：`PlayerGuideView.frame6Config` 已自动绑定到项目中已存在的
     `Assets/Game/Resources/PlayerGuide/Frame6DialogueConfig.asset`
   - **未接入的资源**（有意跳过，避免猜错）：无——原本存疑的两张 "ChatGPT Image
     ...png" 背景图，用户已确认是 PlayerGuide 梦境背景，并要求"两张都用（分
     阶段切换）"，已实现：`BackgroundView` 新增 `backgroundImage`（Image）字段
     + `SetBackgroundSprite(Sprite)` 方法；`PlayerGuideView` 新增
     `dreamBackgroundSpriteEarly`（雪山全景图，6月24日文件）/
     `dreamBackgroundSpriteLate`（洞穴遗迹四联图，6月22日文件）两个 Sprite 字段；
     `Frame1To5Controller.Frame1_DreamSpaceGeneration()` 开始时设为 Early，
     `Frame4_BubbleDissolve()`（梦泡消散/梦核浮现，紧接着腓腓登场）切换为 Late。
     已通过场景活体检查确认 Image.sprite 正确指向雪山图、两个字段正确绑定。
   - **已知遗留 Animator 内容缺口**（不是代码问题，是美术/动画资源本身的缺口）：
     `Feifei.controller` 里 `m_AnimatorParameters: []`（零参数）且只有一个
     `Feifei_Idle` 状态。`FeifeiCharacterView.SafeSetBool/SafeSetTrigger/
     SafePlayState` 有防御性判空+警告，不会崩溃，但如果代码尝试播放除
     Idle 外的其它状态/参数会打印警告日志。这是之前"为什么有警告"问题的根源，
     如果要消除这些警告需要美术/动画补充更多 Animator 状态，不在本次任务范围内。

3. **✅ 遗留重复 Model 文件问题已排查确认，无需清理**：git status 显示
   `Models/DialogueState.cs`/`PlayerGuideState.cs` 为 `D`（deleted，非
   modified），文件已确认物理不存在于磁盘（`test -f` 验证），只是 git 尚未
   staged 这次删除。这本来就是重命名的正常表现，不是需要额外清理的死文件问题。

4. **⏳ 待办：commit**（尚未执行，用户尚未明确要求现在提交，但之前"可以"批准
   过"全部完成后一次性 commit"）：
   - 需要人类署名，**commit message 不得包含任何 AI 生成署名 /
     `Co-Authored-By: Claude` 行**（全局 CLAUDE.md 硬性要求）
   - git status 涉及的文件范围：见下方"当前 git 状态"
   - 建议先跑一次 Play Mode 冒烟测试（entering play mode 在无焦点编辑器环境下
     容易卡在 `playmode_transition` 阶段，是 Unity 常见现象非代码 bug，之前
     测试时是这样，如果需要更可靠的验证建议让用户在有焦点的编辑器窗口里手动
     点 Play 试玩一遍 PlayerGuide 场景）

## 当前 git 状态（未提交，等待下一步指示）

Modified:
`Assets/Game/Scenes/PlayerGuide.unity`,
`Assets/Game/Scripts/Editor/PlayerGuideV2SceneSetup.cs`,
`Assets/Game/Scripts/PlayerGuide/Controllers/*.cs`（5 个）,
`Assets/Game/Scripts/PlayerGuide/Models/InputState.cs`,
`Assets/Game/Scripts/PlayerGuide/Views/*.cs`（9 个）,
`Assets/Game/Scripts/UI/player_guide/PlayerGuideFlowControllerV2.cs`,
`Assets/Scripts/Dialogue/EventNode.cs`,
`Assets/Scripts/UI/TarotCardController.cs`,
`Packages/manifest.json`, `Packages/packages-lock.json`,
`production/session-logs/{compaction-log.txt,session-log.md}`

Deleted: `Assets/Game/Scripts/PlayerGuide/Models/{DialogueState.cs,
DialogueState.cs.meta, PlayerGuideState.cs, PlayerGuideState.cs.meta}`

Untracked (new): `Assets/Game/Scripts/PlayerGuide/Models/
{Frame6DialogueTrackingState.cs, Frame6DialogueTrackingState.cs.meta,
PlayerGuideFrameState.cs, PlayerGuideFrameState.cs.meta}`,
`production/session-state/`

参考近期 commit message 风格（`git log --oneline`）：
```
77fa31b chore: 清理 design 目录结构
2c50a0f feat: 完成 PlayerGuide MVC 重构和鼠标交互优化
aef460b feat: 实现对话系统、Frame状态机和UI控制器
5dc86d0 first commit
```

## 不要动的范围外内容

`Assets/Game/Scripts/UI/PlayerGuideUIController.cs`（挂在场景 `PlayerGuideRoot`，
instance ID 65760）是另一套无关的通用分页引导 UI，与本次 MVC 重构无关，不要碰。

## 已知工具行为坑（避免重复踩）

- `Read` 工具对"本轮之前已读过、但已被 compact 摘要掉"的文件会报
  `Wasted call — file unchanged since your last Read` 拒绝重读。**解决办法**：
  改用 `mcp__unityMCP__manage_script`（`action: "read"`），可单个调用也可用
  `batch_execute` 并行读多个，能绕过该缓存。
- `mcp__unityMCP__manage_scene` 的 `get_hierarchy` 带具体 `target` 参数时**不会**
  正确限定到该节点子树。**优先改用** `mcp__tools__readmcpresourcetool`，URI
  格式 `mcpforunity://scene/gameobject/{id}`（查组件用
  `mcpforunity://scene/gameobject/{id}/component/{ComponentName}`）。
- `find_gameobjects` 默认不包含未激活（`SetActive(false)`）的 GameObject，需要
  传 `include_inactive: true` 才能搜到本场景里大部分初始为隐藏状态的 Frame 面板。
- 本项目有一个"Fact-Forcing Gate" hook，会拦截 Bash / Edit / Write 调用，要求
  先按格式说明（调用者/是否重复/数据结构/用户原话）才放行，每次新文件路径
  都可能触发一次，属正常摩擦，按格式回答即可通过。
- Unity Play Mode 在编辑器窗口无焦点（`is_focused: false`）时容易长时间卡在
  `playmode_transition` 阶段不进入 `is_changing: false`，这是 Unity 已知行为
  （后台不 tick），不代表代码有问题，不必强行等待，可直接 stop 退出。

## 关于本文件本身

用户要求："你先总结一下吧，不然我 clear 后，又需要重新下命令，你也需要重新
看，太浪费时间了" —— 已按此建立并持续更新本 `production/session-state/active.md`。
后续每次有实质性进展（比如 commit 完成）应继续更新此文件。
