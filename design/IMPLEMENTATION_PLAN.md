# 新手引导流程实现计划

## 架构设计

### 现有组件
- **PlayerGuideState**: 已有，但为空壳，需要扩展
- **PlayerGuideFlowController**: 已有 4 步流程（Bubble → FeifeiDialogue → Input → Gacha）
- **GameLoop**: 管理状态机和场景切换
- **DialogueManager**: 对话系统（已有，可复用）
- **SequentialTextConfig**: 顺序文本配置（已有，可用于对话）

### 新增组件（需要创建）

#### 1. 扩展的流程状态枚举
```csharp
// 在 PlayerGuideFlowController 中扩展 GuideStep
private enum GuideStep
{
    // Frame 1: 梦境空间生成
    DreamSpaceGeneration,
    
    // Frame 2: 主梦泡可点击
    MainBubbleAppear,
    
    // Frame 3: 梦境残响播放
    DreamEchoPlay,
    
    // Frame 4: 梦泡消散留下梦核
    BubbleDissolve,
    
    // Frame 5: 腓腓入场捧梦核
    FeifeiEnterWithCore,
    
    // Frame 6: 第一次相遇对白（6-1 至 6-8）
    FirstMeetingDialogue,
    
    // Frame 7: 输入梦
    DreamInput,
    
    // Frame 8: 写梦与必要补问（双状态）
    WriteDreamAndFollowUp,
    
    // Frame 9: 塔罗抽牌（状态 A-G）
    TarotDrawing,
    
    // Frame 10: 关卡结束与梦核回应
    CoreResponseAndEnd
}
```

#### 2. Frame 6 对话系统
- **Frame6DialogueConfig.asset**: ScriptableObject，包含 8 段对话
- **Frame6DialogueController.cs**: 管理 6-1 至 6-8 的逐段播放

#### 3. Frame 8 输入验证器
- **DreamInputValidator.cs**: 语义判断逻辑（关键词匹配 + 长度检查）
- **Frame8StateController.cs**: 管理状态 A/B 切换

#### 4. Frame 9 塔罗系统
- **TarotCardData.cs**: ScriptableObject，卡牌数据
- **TarotDrawController.cs**: 管理 7 个状态（A-G）
- **TarotCard.cs**: 单张卡牌的 UI 组件
- **TarotResultData.cs**: 抽牌结果数据结构

#### 5. 动画控制器
- **DreamCoreAnimator.cs**: 梦核的光效动画
- **FeifeiAnimator.cs**: 腓腓角色动画（入场、捧核、对话表情）
- **BubbleDissolveEffect.cs**: 梦泡消散粒子效果

---

## 实现步骤

### Phase 1: 核心框架扩展（2 小时）

#### 1.1 扩展 PlayerGuideFlowController
- 将现有 4 步重构为 10 步流程
- 添加状态机管理（Enter/Update/Exit）
- 添加进度保存

#### 1.2 创建 Frame 配置数据
```
Assets/Data/PlayerGuide/
├── Frame6DialogueConfig.asset
├── Frame8FollowUpRules.asset
├── TarotCards/
│   ├── Card_Yinglong.asset
│   ├── Card_RiYueLunZhuan.asset
│   ├── Card_ChangXi.asset
│   └── Card_ChanChuJingPo.asset
└── AnimationTimings.asset
```

---

### Phase 2: Frame 1-5 自动播放序列（1.5 小时）

#### 2.1 Frame 1: 梦境空间生成
```csharp
private IEnumerator Frame1_DreamSpaceGeneration()
{
    // 粒子系统从中心扩散
    dreamSpaceParticles.Play();
    
    // 背景淡入
    yield return StartCoroutine(FadeInBackground(3.5f));
    
    // 播放环境音
    AudioManager.PlaySFX("dream_space_generate");
    
    yield return new WaitForSeconds(3.5f);
    
    // 进入 Frame 2
    TransitionToStep(GuideStep.MainBubbleAppear);
}
```

#### 2.2 Frame 2: 主梦泡可点击
```csharp
private IEnumerator Frame2_MainBubbleAppear()
{
    // 梦泡从中心生成
    mainBubbleVisual.gameObject.SetActive(true);
    yield return StartCoroutine(mainBubbleVisual.PlayAppearAnimation(2.0f));
    
    // 启用点击交互
    mainBubbleButton.interactable = true;
    mainBubbleButton.onClick.AddListener(OnMainBubbleClicked);
    
    // 等待玩家点击（不自动跳转）
}

private void OnMainBubbleClicked()
{
    mainBubbleButton.interactable = false;
    TransitionToStep(GuideStep.DreamEchoPlay);
}
```

#### 2.3 Frame 3: 梦境残响播放
```csharp
private IEnumerator Frame3_DreamEchoPlay()
{
    // 播放音频残响
    AudioManager.PlaySFX("dream_echo", volume: 0.7f);
    
    // 梦泡轻微震动
    yield return StartCoroutine(mainBubbleVisual.PlayVibrationEffect(2.5f));
    
    yield return new WaitForSeconds(2.5f);
    
    TransitionToStep(GuideStep.BubbleDissolve);
}
```

#### 2.4 Frame 4: 梦泡消散留下梦核
```csharp
private IEnumerator Frame4_BubbleDissolve()
{
    // 梦泡消散动画
    yield return StartCoroutine(mainBubbleVisual.PlayDissolveAnimation(2.5f));
    
    // 梦核显现
    dreamCore.gameObject.SetActive(true);
    yield return StartCoroutine(dreamCore.PlayRevealAnimation(1.5f));
    
    // 播放音效
    AudioManager.PlaySFX("bubble_dissolve");
    
    yield return new WaitForSeconds(1.0f);
    
    TransitionToStep(GuideStep.FeifeiEnterWithCore);
}
```

#### 2.5 Frame 5: 腓腓入场捧梦核
```csharp
private IEnumerator Frame5_FeifeiEnterWithCore()
{
    // 腓腓从右侧进入
    feifeiCharacter.gameObject.SetActive(true);
    yield return StartCoroutine(feifeiCharacter.PlayWalkInAnimation(
        startPos: new Vector3(10f, 0f, 0f),
        endPos: new Vector3(0f, 0f, 0f),
        duration: 3.0f
    ));
    
    // 播放脚步声
    AudioManager.PlaySFX("feifei_footsteps", loop: true);
    
    // 捧起梦核动作
    yield return StartCoroutine(feifeiCharacter.PlayPickUpCoreAnimation(1.5f));
    AudioManager.StopSFX("feifei_footsteps");
    AudioManager.PlaySFX("core_pickup");
    
    yield return new WaitForSeconds(1.0f);
    
    TransitionToStep(GuideStep.FirstMeetingDialogue);
}
```

---

### Phase 3: Frame 6 对话系统（2 小时）

#### 3.1 创建 Frame6DialogueController
```csharp
public class Frame6DialogueController : MonoBehaviour
{
    [SerializeField] private SequentialTextConfig dialogueConfig;
    [SerializeField] private DialogueUIController dialogueUI;
    [SerializeField] private FeifeiAnimator feifeiAnimator;
    [SerializeField] private DreamCoreAnimator coreAnimator;
    
    private int currentDialogueIndex = 0;
    
    public IEnumerator PlayAllDialogues()
    {
        for (int i = 0; i < 8; i++)
        {
            yield return PlayDialogue(i);
        }
    }
    
    private IEnumerator PlayDialogue(int index)
    {
        string text = dialogueConfig.lines[index];
        
        // 播放对应的角色动画
        PlayFeifeiAnimation(index);
        PlayCoreAnimation(index);
        
        // 显示对话
        dialogueUI.ShowDialogue("腓腓", text);
        
        // 等待玩家点击（或自动播放）
        yield return WaitForDialogueAdvance(GetDialogueDuration(index));
    }
    
    private void PlayFeifeiAnimation(int index)
    {
        switch (index)
        {
            case 0: // 6-1 接住梦核
                feifeiAnimator.PlayAnimation("HoldCore");
                break;
            case 1: // 6-2 低头看梦核
                feifeiAnimator.PlayAnimation("LookDown");
                break;
            // ... 其他 6 个
        }
    }
    
    private void PlayCoreAnimation(int index)
    {
        switch (index)
        {
            case 0: // 微弱蓝紫光
                coreAnimator.SetGlowIntensity(0.3f);
                break;
            case 2: // 一圈涟漪
                coreAnimator.PlayRippleEffect();
                break;
            // ... 其他
        }
    }
}
```

#### 3.2 Frame 6 配置数据
```csharp
// Assets/Data/PlayerGuide/Frame6DialogueConfig.asset
lines[0] = "……你也听见了吗？"
lines[1] = "已经很久，没有人停下来听它们说话了。"
lines[2] = "它刚刚……还在很多话没说完。"
lines[3] = "每一个梦，好像都想告诉我主人一些事情。"
lines[4] = "只是很多人嫌来以后，就忘记回头听了。"
lines[5] = "如果你还记得。"
lines[6] = "我们一起去看看，它到底想告诉你什么。"
lines[7] = "" // 无对白，只有 Fade
```

---

### Phase 4: Frame 7/8 输入系统（1.5 小时）

#### 4.1 Frame 7: 输入梦
```csharp
private IEnumerator Frame7_DreamInput()
{
    // 显示输入框
    inputField.gameObject.SetActive(true);
    inputField.text = "";
    inputField.placeholder.GetComponent<TMP_Text>().text = "写下你的梦境...";
    
    // 播放输入框弹出动画
    yield return StartCoroutine(AnimateInputFieldAppear(0.3f));
    
    // 聚焦输入框
    inputField.ActivateInputField();
    
    // 等待玩家输入并提交
    submitButton.onClick.AddListener(OnDreamInputSubmitted);
}

private void OnDreamInputSubmitted()
{
    string userInput = inputField.text.Trim();
    
    // 存储到 GameContext
    GameContext.Instance.PlayerDreamInput = userInput;
    
    // 进入 Frame 8（状态 A）
    TransitionToStep(GuideStep.WriteDreamAndFollowUp);
}
```

#### 4.2 Frame 8: 写梦与必要补问
```csharp
public class DreamInputValidator
{
    private static readonly string[] VAGUE_KEYWORDS = new[]
    {
        "不知道", "随便", "无所谓", "没想好", "算了", 
        "不想说", "随意", "都行", "懒得", "不清楚"
    };
    
    public static bool NeedsFollowUp(string input)
    {
        // 规则 1: 长度过短
        if (input.Length < 5)
            return true;
        
        // 规则 2: 包含模糊关键词
        foreach (var keyword in VAGUE_KEYWORDS)
        {
            if (input.Contains(keyword))
                return true;
        }
        
        // 规则 3: 全是符号或数字
        if (System.Text.RegularExpressions.Regex.IsMatch(input, @"^[\d\W]+$"))
            return true;
        
        return false;
    }
}

private IEnumerator Frame8_WriteDreamAndFollowUp()
{
    string userInput = GameContext.Instance.PlayerDreamInput;
    
    if (DreamInputValidator.NeedsFollowUp(userInput))
    {
        // 状态 B: 腓腓补问
        yield return PlayFollowUpDialogue();
        
        // 等待玩家重新输入
        inputField.text = "";
        inputField.ActivateInputField();
        
        bool isWaitingForInput = true;
        submitButton.onClick.RemoveAllListeners();
        submitButton.onClick.AddListener(() => {
            userInput = inputField.text.Trim();
            GameContext.Instance.PlayerDreamInput = userInput;
            isWaitingForInput = false;
        });
        
        yield return new WaitUntil(() => !isWaitingForInput);
    }
    
    // 隐藏输入框
    yield return StartCoroutine(AnimateInputFieldDisappear(0.3f));
    
    // 进入 Frame 9
    TransitionToStep(GuideStep.TarotDrawing);
}

private IEnumerator PlayFollowUpDialogue()
{
    dialogueUI.ShowDialogue("腓腓", "能再多说一点吗？");
    yield return new WaitForSeconds(2.0f);
}
```

---

### Phase 5: Frame 9 塔罗抽牌系统（3 小时）

#### 5.1 数据结构
```csharp
[CreateAssetMenu(menuName = "Game/TarotCard")]
public class TarotCardData : ScriptableObject
{
    public string cardName;        // "应龙"
    public string tarotName;       // "战车"
    public Sprite cardFace;
    public Sprite cardBack;
    public bool isMainCard;        // 应龙 = true
}

public class TarotResultData
{
    public TarotCardData mainCard;
    public List<TarotCardData> emotionCards = new();
}
```

#### 5.2 TarotDrawController
```csharp
public class TarotDrawController : MonoBehaviour
{
    private enum TarotState
    {
        GuideDialogue,      // A: 引导对白
        FadeOut,            // B: 腓腓淡出
        FirstDraw,          // C: 第一次抽牌
        ContinueSelect,     // D: 等待继续选择
        SecondToFourth,     // E: 第 2-4 张
        FullDisplay,        // F: 四张完整展示
        FadeToLevel         // G: Fade 进入教学关
    }
    
    [SerializeField] private TarotCardData[] allCards; // 4 张固定牌
    [SerializeField] private TarotCard[] cardSlots;    // 22 张牌背位置
    [SerializeField] private Transform[] displaySlots; // 4 个展示区位置
    [SerializeField] private DialogueUIController dialogueUI;
    [SerializeField] private Image feifeiAvatar;
    
    private TarotState currentState;
    private List<TarotCardData> drawnCards = new();
    private bool isProcessing;
    
    public IEnumerator StartTarotFlow()
    {
        yield return StateA_GuideDialogue();
        yield return StateB_FadeOut();
        yield return StateC_FirstDraw();
        // C → D → E → F → G 通过点击触发
    }
    
    private IEnumerator StateA_GuideDialogue()
    {
        string[] lines = new[]
        {
            "嗯……我听见一些了。",
            "不过，它说得很轻。",
            "来，先选一张。",
            "看看哪一张，会先回应你。"
        };
        
        foreach (var line in lines)
        {
            dialogueUI.ShowDialogue("腓腓", line);
            yield return new WaitForSeconds(2.5f);
        }
        
        currentState = TarotState.FadeOut;
    }
    
    private IEnumerator StateB_FadeOut()
    {
        // 腓腓头像 + 对白框淡出
        yield return StartCoroutine(FadeOutUI(feifeiAvatar, 0.4f));
        yield return StartCoroutine(FadeOutUI(dialogueUI.GetComponent<CanvasGroup>(), 0.4f));
        
        currentState = TarotState.FirstDraw;
        
        // 启用 22 张牌的点击
        EnableCardSelection(true);
    }
    
    public void OnCardClicked(int cardIndex)
    {
        if (isProcessing || currentState == TarotState.FadeToLevel)
            return;
        
        StartCoroutine(ProcessCardDraw(cardIndex));
    }
    
    private IEnumerator ProcessCardDraw(int cardIndex)
    {
        isProcessing = true;
        
        TarotCardData card;
        if (drawnCards.Count == 0)
        {
            // 第一次：固定返回应龙
            card = allCards[0]; // 应龙
        }
        else
        {
            // 后三张：随机从剩余三张中选
            var remaining = allCards.Skip(1).ToList();
            remaining.RemoveAll(c => drawnCards.Contains(c));
            card = remaining[Random.Range(0, remaining.Count)];
        }
        
        // 动画：原位翻牌
        yield return cardSlots[cardIndex].FlipToFace(card.cardFace, 0.3f);
        
        // 动画：上移到展示区
        yield return cardSlots[cardIndex].MoveToSlot(displaySlots[drawnCards.Count], 0.4f);
        
        // 主牌特殊效果
        if (card.isMainCard)
        {
            yield return cardSlots[cardIndex].PlayGlowEffect(1.05f, 0.6f);
        }
        
        drawnCards.Add(card);
        
        // 检查是否完成
        if (drawnCards.Count >= 4)
        {
            yield return new WaitForSeconds(1.35f);
            yield return StateF_FullDisplay();
            yield return StateG_FadeToLevel();
        }
        else
        {
            currentState = TarotState.ContinueSelect;
        }
        
        isProcessing = false;
    }
    
    private IEnumerator StateF_FullDisplay()
    {
        // 剩余 18 张牌淡出
        EnableCardSelection(false);
        yield return StartCoroutine(FadeOutRemainingCards(0.3f));
        
        currentState = TarotState.FullDisplay;
    }
    
    private IEnumerator StateG_FadeToLevel()
    {
        // 保存结果
        var result = new TarotResultData
        {
            mainCard = drawnCards[0],
            emotionCards = drawnCards.Skip(1).ToList()
        };
        GameContext.Instance.TarotResult = result;
        
        // Fade 到黑
        yield return StartCoroutine(SceneTransitionManager.FadeOut(0.5f));
        
        // 加载教学关
        GameLoop.Instance.ContinueToGameplay();
        
        currentState = TarotState.FadeToLevel;
    }
}
```

---

### Phase 6: Frame 10 结束演出（0.5 小时）

```csharp
private IEnumerator Frame10_CoreResponseAndEnd()
{
    // 梦核发光增强
    yield return StartCoroutine(dreamCore.PlayResponseAnimation(2.0f));
    
    // 播放共鸣音效
    AudioManager.PlaySFX("core_response");
    
    yield return new WaitForSeconds(2.0f);
    
    // Fade 到黑
    yield return StartCoroutine(SceneTransitionManager.FadeOut(1.0f));
    
    // 加载教学关（与 Frame 9 共用逻辑）
    GameLoop.Instance.ContinueToGameplay();
}
```

---

## 数据持久化

```csharp
// 在 GameContext 中添加字段
public class GameContext
{
    // ... 现有字段
    
    public string PlayerDreamInput { get; set; }
    public TarotResultData TarotResult { get; set; }
}

// 在 SaveSystem 中保存
public class GameSaveData
{
    // ... 现有字段
    
    [System.Serializable]
    public class PlayerGuideProgress
    {
        public bool hasCompleted;
        public string dreamInput;
        public string mainCardId;
        public List<string> emotionCardIds;
    }
    
    public PlayerGuideProgress guideProgress = new();
}
```

---

## 时间估算

| 阶段 | 任务 | 预计时长 |
|-----|-----|---------|
| Phase 1 | 核心框架扩展 | 2h |
| Phase 2 | Frame 1-5 自动播放 | 1.5h |
| Phase 3 | Frame 6 对话系统 | 2h |
| Phase 4 | Frame 7/8 输入系统 | 1.5h |
| Phase 5 | Frame 9 塔罗系统 | 3h |
| Phase 6 | Frame 10 结束演出 | 0.5h |
| **总计** |  | **10.5h** |

加上测试调试 + 集成：**12-14 小时**（约 1.5-2 个工作日）

---

## 下一步

我现在开始实现 Phase 1（核心框架扩展），创建以下文件：
1. 扩展后的 `PlayerGuideFlowController.cs`
2. `Frame6DialogueConfig.asset` 的配置类
3. `TarotCardData.cs` + `TarotResultData.cs`
4. `DreamInputValidator.cs`

**确认后开始编码？**
