# Frame 9 | 塔罗抽牌细化

## 全局说明
同一页面连续状态：22 张牌固定在中下方；已抽牌与四张展示固定在中上方。

---

## 状态 A | 引导对白

**同一对白框逐句出现**（4 句）:
1. 「嗯……我听见一些了。」
2. 「不过，它说得很轻。」
3. 「来，先选一张。」
4. 「看看哪一张，会先回应你。」

**画面**:
- 腓腓头像在左侧（圆形占位）
- 对白框在右侧
- 22 张牌背在中下方，扇形排列

---

## 状态 B | 腓腓与对白淡出

**画面**:
- 腓腓头像淡出（灰色，半透明）
- 对白框显示最后一句：「看看哪一张，会先回应你。」（灰色文字）

**技术说明**:
- 对白框与腓腓同步淡出 0.35-0.4s
- 22 张牌保持中下方原位

---

## 状态 C | 第一次抽牌

**画面**:
- 一张牌翻开显示「应龙（战车）」，位于中上方第 1 位
- 向上箭头指示移动方向
- 22 张牌保持在中下方

**技术说明**:
- 所选牌原位翻开 → 上移到中上方第 1 位
- 应龙轻微放大 105% + 淡光 + 停顿约 0.6s；不增加对白

---

## 状态 D | 继续选择

**画面**:
- 「应龙（战车）」保持在中上方第 1 位
- 剩余牌仍在中下方原位可点击

**技术说明**:
- 第 1 张保持在中上方；剩余牌仍在中下方原位可点击
- 不加"情绪牌"标签，也不增加引导对白

---

## 状态 E | 第二至第四张

**画面**（示例：抽到第 3 张时）:
1. 应龙（战车）— 已选中，高亮
2. 日月轮转（命运之轮）
3. 常羲·星月祈愿（星星）

**技术说明**:
- 第 2、3、4 张使用同一普通翻牌 + 上移动效，依次填入中上方
- 后三张顺序随机；此画面仅示例抽到第 3 张时的状态

---

## 状态 F | 四张完整展示

**画面**（四张牌横排在中上方）:
1. **应龙（战车）** — 主牌，高亮边框
2. **日月轮转（命运之轮）**
3. **常羲·星月祈愿（星星）**
4. **蟾蜍精魄（月亮）**

**技术说明**:
- 剩余 18 张牌淡出 0.3s；四张结果保持在中上方完整展示 1.2-1.5s
- 不加对白，不显示功能标签

---

## 状态 G | Fade 进入教学

**画面**:
- 四张牌保持显示（略微变暗）
- 整页渐变至黑场

**技术说明**:
- 整页 Fade 至黑场 0.5s
- 教学关加载完成后由黑场淡入 0.5s

---

## 协作需求｜塔罗抽牌

### 美术
- 统一牌背 ×1
- 山海经塔罗牌面 ×4
- 单张尺寸
- 主牌淡光边框（可选）
- 沿用现有梦境背景
- 不制作卡牌动画资源

### 动效
- 腓腓 + 对白框同步淡出
- 22 张牌中下方错落展开
- 选中牌原位翻开后上移
- 主牌轻放大/发光/停顿
- 剩余牌淡出 + 整页 Fade

### 音效
- 牌阵出现：轻微气流/纸牌声
- 主牌翻开：稍明确的共鸣
- 辅牌翻开：轻量普通翻牌声
- Fade：短促柔和转场
- 不做夸张魔法高潮

### 程序
- 同一页面维护所有抽牌状态
- 第一次任意选择固定返回应龙
- 后三张从三张牌中随机排序
- 防止重复点击
- 维护 1/4-4/4 状态
- 四张完成后触发 Fade

### 关卡
- 接收主牌：地图输入
- 接收后三张：情绪关卡输入
- Fade 完成后加载教学关
- 教学内部玩法另见关卡文档
- 本流程不规定机关和路线

---

## 技术实现要点

### 状态机设计
```csharp
public enum TarotState
{
    GuideDialogue,      // 状态 A：引导对白（4 句）
    FadeOut,            // 状态 B：腓腓与对白淡出
    FirstDraw,          // 状态 C：第一次抽牌（固定应龙）
    ContinueSelect,     // 状态 D：等待继续选择
    SecondToFourth,     // 状态 E：第 2-4 张
    FullDisplay,        // 状态 F：四张完整展示
    FadeToLevel         // 状态 G：Fade 进入教学关
}
```

### 卡牌数据
```csharp
[CreateAssetMenu(menuName = "Game/TarotCard")]
public class TarotCardData : ScriptableObject
{
    public string cardName;        // "应龙"
    public string tarotName;       // "战车"
    public Sprite cardFace;
    public bool isMainCard;        // 应龙 = true
}
```

**四张牌配置**:
| 序号 | 中文名 | 塔罗对应 | 类型 |
|-----|-------|---------|------|
| 1 | 应龙 | 战车 | 主牌（固定第一张） |
| 2 | 日月轮转 | 命运之轮 | 辅牌 |
| 3 | 常羲·星月祈愿 | 星星 | 辅牌 |
| 4 | 蟾蜍精魄 | 月亮 | 辅牌 |

### 抽牌逻辑
```csharp
public class TarotDrawController : MonoBehaviour
{
    private List<TarotCardData> allCards;      // 22 张牌背
    private List<TarotCardData> drawnCards;    // 已抽出的牌
    private bool isProcessing;                 // 防止重复点击
    
    public void OnCardClicked(int cardIndex)
    {
        if (isProcessing) return;
        isProcessing = true;
        
        TarotCardData card;
        if (drawnCards.Count == 0)
        {
            // 第一次：无论点哪张，固定返回应龙
            card = mainCard; // 应龙（战车）
        }
        else
        {
            // 后三张：从剩余三张中随机
            card = GetRandomFromRemaining();
        }
        
        StartCoroutine(PlayDrawAnimation(cardIndex, card));
    }
    
    private IEnumerator PlayDrawAnimation(int index, TarotCardData card)
    {
        // 1. 原位翻开
        yield return FlipCard(index, card);
        
        // 2. 上移到中上方对应位置
        yield return MoveToTopSlot(index, drawnCards.Count);
        
        // 3. 主牌特殊处理
        if (card.isMainCard)
        {
            yield return ScaleAndGlow(1.05f, 0.6f);
        }
        
        drawnCards.Add(card);
        isProcessing = false;
        
        // 4. 检查是否完成
        if (drawnCards.Count >= 4)
        {
            yield return new WaitForSeconds(1.35f); // 1.2-1.5s
            StartCoroutine(FadeOutRemainingAndTransition());
        }
    }
}
```

### 布局参数
- **中下方牌阵**：22 张牌扇形排列，中心 X = 屏幕中心，Y = 屏幕高度 × 0.25
- **中上方展示区**：4 个槽位横排，Y = 屏幕高度 × 0.7
- **牌尺寸**：宽 120px，高 200px（需与美术确认）
- **扇形角度**：约 ±15°（22 张牌）

### 输出接口
```csharp
// Fade 完成后传递给关卡系统
public class TarotResult
{
    public TarotCardData mainCard;           // 应龙 → 地图输入
    public List<TarotCardData> emotionCards; // 后三张 → 情绪关卡输入
}

// 存储到 GameContext 或 DialogueProgressService
GameContext.TarotResult = result;
SceneTransitionManager.LoadScene("TutorialLevel");
```
