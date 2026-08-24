# 协作需求 | 塔罗抽牌

完整的美术、动效、音效、程序、关卡需求清单。

---

## 美术

- [ ] 统一牌背 ×1
- [ ] 山海经塔罗牌面 ×4
- [ ] 单张尺寸
- [ ] 主牌淡光边框（可选）
- [ ] 沿用现有梦境背景
- [ ] 不制作卡牌动画资源

**具体牌面**:
1. 应龙（战车）— 主牌
2. 日月轮转（命运之轮）
3. 常羲·星月祈愿（星星）
4. 蟾蜍精魄（月亮）

**美术规格**:
- 牌背尺寸：建议 512×853px（3:5 比例）
- 牌面尺寸：与牌背一致
- 格式：PNG with Alpha
- 主牌边框：可选发光效果（Shader 实现或单独 sprite）

---

## 动效

- [ ] 腓腓 + 对白框同步淡出
- [ ] 22 张牌中下方错落展开
- [ ] 选中牌原位翻开后上移
- [ ] 主牌轻放大/发光/停顿
- [ ] 剩余牌淡出 + 整页 Fade

**时长参考**:
- 腓腓淡出：0.35-0.4s
- 牌阵展开：0.8-1.0s（从中心向两侧扇形展开）
- 翻牌动画：0.3s（3D 旋转 Y 轴 180°）
- 上移至展示区：0.4s（ease out）
- 主牌放大：1.05× scale + 发光 0.6s
- 剩余牌淡出：0.3s
- 整页 Fade：0.5s

**技术实现**:
- 翻牌：Animator 状态机或 DOTween RotateY
- 上移：DOTween MoveY + SetEase(Ease.OutCubic)
- 发光：Sprite Renderer Material 的 Emission 参数动画
- Fade：Canvas Group Alpha 渐变

---

## 音效

- [ ] 牌阵出现：轻微气流/纸牌声
- [ ] 主牌翻开：稍明确的共鸣
- [ ] 辅牌翻开：轻量普通翻牌声
- [ ] Fade：短促柔和转场
- [ ] 不做夸张魔法高潮

**音效规格**:
- 牌阵出现：轻柔的 whoosh + 纸牌摩擦，音量 -12dB，时长 0.8s
- 主牌翻开：低频共鸣 + 轻微钟声，音量 -8dB，时长 0.5s
- 辅牌翻开：普通翻牌声，音量 -15dB，时长 0.3s
- Fade：白噪音衰减，音量 -18dB，时长 0.5s

**音效资源**:
| 文件名 | 用途 | 格式 | 时长 |
|-------|-----|------|------|
| card_array_appear.wav | 牌阵展开 | WAV 44.1kHz | 0.8s |
| card_flip_main.wav | 主牌翻开 | WAV | 0.5s |
| card_flip_normal.wav | 普通翻牌 | WAV | 0.3s |
| fade_transition.wav | 场景淡出 | WAV | 0.5s |

---

## 程序

- [ ] 同一页面维护所有抽牌状态
- [ ] 第一次任意选择固定返回应龙
- [ ] 后三张从三张牌中随机排序
- [ ] 防止重复点击
- [ ] 维护 1/4-4/4 状态
- [ ] 四张完成后触发 Fade

**核心逻辑**:
```csharp
// 1. 状态机管理
public enum TarotState
{
    GuideDialogue,      // A: 引导对白（4 句）
    FadeOut,            // B: 腓腓淡出
    FirstDraw,          // C: 第一次抽牌（固定应龙）
    ContinueSelect,     // D: 等待继续选择
    SecondToFourth,     // E: 第 2-4 张
    FullDisplay,        // F: 四张完整展示
    FadeToLevel         // G: Fade 进入教学关
}

// 2. 抽牌规则
if (drawnCount == 0)
{
    selectedCard = mainCard; // 固定返回应龙
}
else
{
    // 从剩余三张中随机抽取
    selectedCard = Random.Range(remainingCards);
}

// 3. 防止重复点击
private bool isProcessing = false;
public void OnCardClicked(int index)
{
    if (isProcessing || drawnCount >= 4) return;
    isProcessing = true;
    StartCoroutine(DrawCard(index));
}

// 4. 四张完成触发
if (drawnCards.Count >= 4)
{
    yield return new WaitForSeconds(1.35f);
    TransitionToLevel();
}
```

**UI 交互**:
- 22 张牌背作为 Button 组件，点击触发 `OnCardClicked(index)`
- 已抽出的牌禁用点击（`interactable = false`）
- 中上方展示区用 4 个 Image 组件（初始 alpha = 0）

**数据传递**:
```csharp
public class TarotResult
{
    public string mainCardId;           // "应龙"
    public List<string> emotionCardIds; // ["日月轮转", "常羲", "蟾蜍精魄"]
}

// 存储到 GameContext
GameContext.Instance.TarotResult = result;

// 传递给关卡系统
LevelManager.Instance.LoadTutorialLevel(result);
```

---

## 关卡

- [ ] 接收主牌：地图输入
- [ ] 接收后三张：情绪关卡输入
- [ ] Fade 完成后加载教学关
- [ ] 教学内部玩法另见关卡文档
- [ ] 本流程不规定机关和路线

**关卡输入接口**:
```csharp
public class TutorialLevelConfig : ScriptableObject
{
    // 主牌影响地图主题
    public Dictionary<string, LevelTheme> mainCardThemes = new()
    {
        {"应龙", new LevelTheme { 
            tileset = "战车地形", 
            obstacles = new[] {"石柱", "战车残骸"},
            music = "战鼓主题"
        }},
        // 其他主牌...
    };
    
    // 辅牌影响情绪关卡
    public Dictionary<string, EmotionChallenge> emotionCards = new()
    {
        {"日月轮转", new EmotionChallenge { 
            type = "时间反转",
            duration = 30f
        }},
        {"常羲", new EmotionChallenge { 
            type = "星光引导",
            duration = 20f
        }},
        {"蟾蜍精魄", new EmotionChallenge { 
            type = "幻境迷雾",
            duration = 25f
        }},
    };
}

// 加载教学关时应用配置
public void LoadTutorialLevel(TarotResult result)
{
    var theme = config.mainCardThemes[result.mainCardId];
    ApplyLevelTheme(theme);
    
    foreach (var cardId in result.emotionCardIds)
    {
        var challenge = config.emotionCards[cardId];
        RegisterEmotionChallenge(challenge);
    }
    
    SceneTransitionManager.LoadScene("TutorialLevel");
}
```

**关卡设计约束**:
- 本流程（Frame 9）只负责抽牌交互和结果传递
- 教学关的具体机关、路线、敌人配置由关卡策划文档定义
- 情绪关卡的触发时机、持续时间、视觉效果由关卡系统决定
- 主牌的地图影响仅限于美术风格（tileset、背景、音乐），不改变核心玩法

**数据持久化**:
```csharp
// 存储到存档系统（用于跳过引导后回顾）
public class GameSaveData
{
    // ... 其他字段
    
    [System.Serializable]
    public class TarotHistory
    {
        public string mainCard;
        public List<string> emotionCards;
        public long timestamp; // Unix timestamp
    }
    
    public TarotHistory playerTarot; // 玩家的塔罗抽牌结果
}

// Frame 9 完成后保存
SaveSystem.Instance.CurrentSave.playerTarot = new TarotHistory
{
    mainCard = result.mainCardId,
    emotionCards = result.emotionCardIds,
    timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
};
SaveSystem.Instance.Save();
```

---

## 补充说明

### 22 张牌的其他 18 张
虽然玩家只会抽到固定的 4 张（应龙 + 3 张辅牌），但牌阵展示需要 22 张牌背：
- **方案 A**：22 张都用同一个牌背 sprite（推荐，节省资源）
- **方案 B**：22 张牌背略有差异（边缘纹理、颜色微调），增加视觉丰富度

**其他 18 张是否需要设计牌面？**
- 不需要。玩家永远不会抽到它们，无需制作牌面。
- 如果未来扩展允许玩家二周目抽不同的牌，再补充牌面资源。

### 状态 A 对白的显示方式
4 句对白可以选择：
1. **逐句显示**：第 1 句显示 → 玩家点击 → 第 2 句替换 → ...（推荐，与 Frame 6 保持一致）
2. **累积显示**：第 1 句 → 点击 → 第 1+2 句 → 点击 → 1+2+3 句（类似聊天记录）

建议用**逐句显示**，与 Frame 6 的对话体验一致。

### 卡牌排列的扇形参数
- **中心点**：屏幕中心底部（X = Screen.width/2, Y = Screen.height * 0.25）
- **扇形半径**：400-500 像素（根据屏幕分辨率调整）
- **角度范围**：-30° 到 +30°（22 张牌，每张间隔约 2.7°）
- **卡牌重叠**：每张牌向右侧偏移 80% 宽度（形成叠牌效果）

```csharp
float angleStep = 60f / (cardCount - 1); // 60° / 21 = 2.857°
float startAngle = -30f;

for (int i = 0; i < cardCount; i++)
{
    float angle = startAngle + i * angleStep;
    float radian = angle * Mathf.Deg2Rad;
    
    Vector3 pos = new Vector3(
        centerX + Mathf.Sin(radian) * radius,
        centerY + Mathf.Cos(radian) * radius * 0.3f, // Y 轴压缩，形成弧形
        0
    );
    
    cards[i].transform.position = pos;
    cards[i].transform.rotation = Quaternion.Euler(0, 0, -angle);
}
```

### 防止卡死的容错逻辑
如果玩家在状态 D（等待继续选择）长时间不操作：
- **方案 A**：无超时，等待玩家主动点击（推荐，尊重玩家节奏）
- **方案 B**：60 秒后自动随机抽满 4 张
- **方案 C**：30 秒后显示提示文字"点击任意牌继续"

建议用**方案 A**，新手引导不应强制时间压力。
