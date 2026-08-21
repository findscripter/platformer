# 梦境游戏脚本结构

**自动生成时间**: 2026-08-21  
**项目**: 腾讯GameJam - 梦境拾荒者的旅程

---

## 目录结构

```
Assets/Scripts/
├── Dialogue/           # 对话系统
│   ├── Nodes/          # 节点类型
│   │   ├── DialogueNode.cs         (基类)
│   │   ├── TextNode.cs             (文本节点)
│   │   ├── InputNode.cs            (输入节点)
│   │   ├── ChoiceNode.cs           (选择节点)
│   │   └── EventNode.cs            (事件节点)
│   ├── DialogueManager.cs          (对话管理器)
│   ├── DialogueUI.cs               (UI控制器)
│   └── DialogueGraph.cs            (对话图)
│
├── Frame/              # Frame流程控制
│   ├── FrameController.cs          (Frame状态机)
│   ├── DreamBubbleInteraction.cs   (Frame 1-2: 梦泡交互)
│   ├── DreamEchoPlayer.cs          (Frame 3: 梦境残响)
│   ├── FeifeiEntrance.cs           (Frame 5: 腓腓入场)
│   ├── FirstMeetingDialogue.cs     (Frame 7: 第一次相遇)
│   ├── DreamInputSystem.cs         (Frame 8: 写梦输入)
│   ├── TarotCardSystem.cs          (Frame 9: 塔罗抽牌)
│   └── LevelEndResponse.cs         (Frame 10: 关卡回响)
│
├── StateMachine/       # 状态机扩展
│   ├── PlayerGuideState.cs         (新手引导状态)
│   └── DialogueState.cs            (对话状态)
│
└── UI/                 # UI组件
    ├── DreamInputPanel.cs          (写梦输入框)
    ├── TarotCardUI.cs              (塔罗牌UI)
    └── DialoguePanel.cs            (对话框)
```

---

## 核心系统

### 1. 对话系统 (Dialogue/)

**设计理念**: ScriptableObject + 节点图驱动

#### 节点类型

| 节点类型 | 用途 | 关键方法 |
|---------|------|---------|
| **TextNode** | 显示文本对话 | Execute() - 打字机效果 |
| **InputNode** | 玩家输入 | GetInput() - 返回字符串 |
| **ChoiceNode** | 多选项分支 | GetChoice() - 返回索引 |
| **EventNode** | 触发Unity事件 | InvokeEvent() - 演出控制 |

#### 使用示例

```csharp
// 创建对话图
var graph = ScriptableObject.CreateInstance<DialogueGraph>();
graph.StartNode = frame7_Meeting;

// 启动对话
DialogueManager.Instance.StartDialogue(graph);
```

### 2. Frame控制器 (Frame/)

**10个Frame流程管理**

```csharp
public enum FrameType
{
    Frame1_DreamSpace,      // 梦境空间生成
    Frame2_MainBubble,      // 主梦泡可点击
    Frame3_DreamEcho,       // 梦境残响
    Frame4_CoreAppear,      // 梦核出现
    Frame5_FeifeiEnter,     // 腓腓入场
    Frame6_FirstLook,       // 接住梦核
    Frame7_FirstMeeting,    // 第一次相遇
    Frame8_WriteDream,      // 写梦输入
    Frame9_TarotDraw,       // 塔罗抽牌
    Frame10_LevelEnd        // 关卡回响
}
```

#### Frame生命周期

```csharp
// FrameController 自动调用
void EnterFrame(FrameType frame);
void UpdateFrame();
void ExitFrame();
```

### 3. 状态机扩展 (StateMachine/)

**新增两个状态**

- **PlayerGuideState**: Frame 1,2,4,5,9 - 玩家引导和交互
- **DialogueState**: Frame 3,6,7,8,10 - 对话和输入

#### 状态切换

```csharp
// 自动切换
FrameController.OnFrameChanged += (oldFrame, newFrame) => {
    if (IsDialogueFrame(newFrame)) {
        stateMachine.TransitionTo<DialogueState>();
    } else {
        stateMachine.TransitionTo<PlayerGuideState>();
    }
};
```

---

## Frame详细说明

### Frame 1-2: 梦泡交互

**脚本**: `DreamBubbleInteraction.cs`

**流程**:
1. 自动生成5-8个梦泡
2. 2-3秒后解锁主梦泡点击
3. 3秒无操作触发弱引导（发光+音效）
4. 点击主梦泡进入Frame 3

**关键方法**:
```csharp
void SpawnDreamBubbles();          // 生成梦泡
void EnableMainBubbleInteraction(); // 解锁交互
void TriggerWeakGuide();           // 弱引导
```

### Frame 3: 梦境残响

**脚本**: `DreamEchoPlayer.cs`

**演出内容**:
- 文本: "我一直在找回家的路……"
- 音效: 风声 + 模糊剪影
- 时长: 2-3秒

**关键方法**:
```csharp
IEnumerator PlayDreamEcho();       // 播放残响
```

### Frame 8: 写梦输入

**脚本**: `DreamInputSystem.cs`

**交互流程**:
1. 显示统一问话框
2. 玩家输入梦境内容
3. 提交给AI分析
4. **如果信息不足** → 显示补问
5. **如果信息足够** → 直接进入Frame 9

**关键方法**:
```csharp
string GetDreamInput();                    // 获取输入
bool ValidateDreamInfo(string input);      // 验证完整度
void ShowFollowUpQuestion(string question); // 显示补问
```

### Frame 9: 塔罗抽牌

**脚本**: `TarotCardSystem.cs`

**规则**:
- 22张牌背固定在中下方
- 第一张**必须固定翻出"应龙（战车）"**
- 后三张随机但不重复
- 四张展示后Fade进入教学关

**关键方法**:
```csharp
void InitializeTarotDeck();        // 初始化22张牌
Card DrawFirstCard();              // 固定返回应龙
Card[] DrawRandomCards(int count); // 抽取后三张
void OnAllCardsDrawn();            // 完成后跳转
```

---

## 数据结构

### 对话节点数据 (Data/Dialogue/)

**ScriptableObject资源**:
- `Frame3_梦境残响.asset`
- `Frame7_第一次相遇.asset`
- `Frame8_写梦输入.asset`
- `Frame10_关卡回响.asset`

**创建方式**:
```
右键 → Create → Dialogue → Text Node
```

### Frame配置数据 (Data/Frame/)

**FrameConfig.asset**: 每个Frame的时间、触发条件、跳转逻辑

---

## 开发优先级

### P0 (核心功能 - 第一版Demo必须)
- [x] 目录结构创建
- [ ] DialogueNode 基类
- [ ] FrameController
- [ ] Frame 1-2 梦泡交互
- [ ] Frame 7 第一次相遇对话
- [ ] Frame 9 塔罗抽牌
- [ ] PlayerGuideState
- [ ] DialogueState

### P1 (演出增强 - 第二版迭代)
- [ ] Frame 3-5 梦境残响 + 腓腓入场
- [ ] Frame 8 写梦输入 + AI补问
- [ ] 打字机效果
- [ ] Fade过渡

### P2 (收尾打磨 - 最终版)
- [ ] Frame 10 关卡回响
- [ ] 音效集成
- [ ] 动画优化
- [ ] UI美化

---

## 调试技巧

### Frame跳转测试

```csharp
// Inspector中手动跳转
FrameController.Instance.JumpToFrame(FrameType.Frame9_TarotDraw);
```

### 对话系统调试

```csharp
// 启用调试日志
DialogueManager.Instance.EnableDebugLog = true;
```

### 查看当前状态

```csharp
// Console输出
Debug.Log($"当前Frame: {FrameController.CurrentFrame}");
Debug.Log($"当前State: {stateMachine.CurrentState}");
```

---

## 常见问题

### Q1: Frame切换后UI没有更新？
**A**: 检查FrameController的UnityEvent是否正确订阅。

### Q2: 对话节点Next引用丢失？
**A**: ScriptableObject不能互相引用场景对象，确保都是Asset文件。

### Q3: 塔罗牌第一张没有固定翻出应龙？
**A**: 检查TarotCardSystem.DrawFirstCard()的硬编码逻辑。

### Q4: Frame 8补问逻辑没有触发？
**A**: 后端AI接口需要返回`needFollowUp`字段。

---

## 下一步开发

1. **等待Workflow完成** - 所有代码将自动生成
2. **导入美术资源** - 运行 `GameJam/Import Art Assets`
3. **创建PlayerGuide场景** - 按照场景配置文档
4. **配置对话数据** - 创建ScriptableObject资产
5. **测试完整流程** - Frame 1 → Frame 10

---

**实时进度**: 使用 `/workflows` 查看工作流进度  
**问题反馈**: 随时询问，AI会持续支持
