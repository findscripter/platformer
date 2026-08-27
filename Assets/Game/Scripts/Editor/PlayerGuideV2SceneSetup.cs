using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 为 PlayerGuide 场景生成完整的 V2 UI 层级结构
/// </summary>
public static class PlayerGuideV2SceneSetup
{
    [MenuItem("Tools/GameJam/8. Setup PlayerGuide V2 Scene")]
    public static void Setup()
    {
        Debug.Log("=== 开始搭建 PlayerGuide V2 场景 ===");

        // 打开场景
        var scenePath = "Assets/Game/Scenes/PlayerGuide.unity";
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
            scenePath,
            UnityEditor.SceneManagement.OpenSceneMode.Single
        );

        // 找到 UI/Canvas_PlayerGuide
        var roots = scene.GetRootGameObjects();
        GameObject uiRoot = null;
        foreach (var r in roots)
        {
            if (r.name == "UI") { uiRoot = r; break; }
        }

        if (uiRoot == null)
        {
            Debug.LogError("未找到 UI 根对象！");
            return;
        }

        var canvas = uiRoot.transform.Find("Canvas_PlayerGuide");
        if (canvas == null)
        {
            Debug.LogError("未找到 Canvas_PlayerGuide！");
            return;
        }

        // 隐藏旧的 GuidePanel
        var oldPanel = canvas.Find("Background/GuidePanel");
        if (oldPanel != null)
        {
            oldPanel.gameObject.SetActive(false);
            Debug.Log("  ✓ 已隐藏旧版 GuidePanel");
        }

        // 移除旧的 V2ControllerRoot（如果存在，避免场景中残留旧版 PlayerGuideFlowControllerV2 实例）
        var oldV2Root = canvas.Find("V2ControllerRoot");
        if (oldV2Root != null)
        {
            Object.DestroyImmediate(oldV2Root.gameObject);
            Debug.Log("  ✓ 已移除旧版 V2ControllerRoot");
        }

        // 创建 V2ControllerRoot
        var v2Root = new GameObject("V2ControllerRoot");
        v2Root.transform.SetParent(canvas, false);
        var rectTransform = v2Root.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;

        // 添加新 MVC 系统的顶层控制器与共享 View
        var controller = v2Root.AddComponent<PlayerGuideController>();
        var view = v2Root.AddComponent<PlayerGuideView>();
        var smallBubblesView = v2Root.AddComponent<SmallBubblesView>();

        // === Frame 1-3: 梦境生成与梦泡 ===
        var smallBubblePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/PlayerGuide/SmallBubble.prefab");
        if (smallBubblePrefab == null)
        {
            Debug.LogWarning("  ⚠ 未找到 SmallBubble.prefab，小梦泡将无法生成");
        }
        var dreamSpaceParticles = CreateParticleSystem(v2Root.transform, "DreamSpaceParticles");
        var backgroundGO = CreateBackgroundGroup(v2Root.transform, "BackgroundCanvasGroup");
        var mainBubble = CreateMainBubble(v2Root.transform);
        // 容器必须在背景之后创建：UGUI 按子节点顺序渲染，容器排在不透明背景之前会被整块遮住
        var smallBubblesContainer = CreateSmallBubblesContainer(v2Root.transform);

        // === Frame 4-5: 梦核与腓腓 ===
        var dreamCore = CreateDreamCore(v2Root.transform);
        var feifei = CreateFeifeiCharacter(v2Root.transform);

        // === Frame 6: 对话系统 ===
        var dialoguePanel = CreateDialoguePanel(v2Root.transform);

        // === Frame 7-8: 输入系统 ===
        var inputPanel = CreateInputPanel(v2Root.transform);

        // === Frame 9: 塔罗系统 ===
        var tarotPanel = CreateTarotPanel(v2Root.transform);

        // === 绑定引用（通过反射，因为字段是 SerializeField private）===
        BindFieldsToNewMvc(controller, view, smallBubblesView,
            dreamSpaceParticles, backgroundGO, mainBubble,
            dreamCore, feifei, dialoguePanel, inputPanel, tarotPanel,
            smallBubblesContainer, smallBubblePrefab
        );

        // 保存场景
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

        Debug.Log("✓ PlayerGuide V2 场景搭建完成！（新 MVC 系统，已接入美术 Prefab / 精灵 / 动画控制器）");
        Debug.Log($"  - 控制器位置: {GetHierarchyPath(v2Root.transform)}");
    }

    private static ParticleSystem CreateParticleSystem(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime = 3f;
        main.startSpeed = 2f;
        main.startSize = 0.5f;
        main.startColor = new Color(0.6f, 0.8f, 1f, 0.7f);
        main.maxParticles = 100;

        Debug.Log($"  ✓ 创建粒子系统: {name}");
        return ps;
    }

    private static GameObject CreateBackgroundGroup(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        var group = go.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        // 添加背景图（默认使用早期阶段的雪山全景美术图，Frame4 会切换为洞穴遗迹图）
        var img = go.AddComponent<Image>();
        img.color = Color.white;
        img.preserveAspect = true;
        var earlySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/Background/ChatGPT Image 2026年6月24日 13_20_15 (2).png");
        if (earlySprite != null)
        {
            img.sprite = earlySprite;
        }
        else
        {
            Debug.LogWarning("  ⚠ 未找到梦境背景图（雪山全景），回退为占位色块");
            img.color = new Color(0.05f, 0.05f, 0.15f, 1f);
        }

        // 梦境回声音效源
        var audioSource = go.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        go.AddComponent<BackgroundView>();

        Debug.Log($"  ✓ 创建背景组: {name}");
        return go;
    }

    private static GameObject CreateMainBubble(Transform parent)
    {
        GameObject go;
        var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/PlayerGuide/MainBubble.prefab");
        if (prefabAsset != null)
        {
            go = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, parent);
            go.name = "MainBubble";
            var prefabRect = go.GetComponent<RectTransform>();
            prefabRect.anchoredPosition = Vector2.zero;
            Debug.Log("  ✓ 创建主梦泡（使用美术 Prefab: MainBubble.prefab）");
        }
        else
        {
            Debug.LogWarning("  ⚠ 未找到 MainBubble.prefab，回退为占位方案");
            go = new GameObject("MainBubble");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(300, 300);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.8f, 0.9f, 1f, 0.5f);

            go.AddComponent<Button>();

            // 使用 EnsureStructure 自动添加 PlayerGuideMainBubbleVisual 并重构层级
            PlayerGuideMainBubbleVisual.EnsureStructure(go);
        }

        var btn = go.GetComponent<Button>();
        if (btn != null) btn.interactable = false;

        // 创建 EchoText 子节点（Frame 3 残响台词显示）
        var echoTextGO = new GameObject("EchoText");
        echoTextGO.transform.SetParent(go.transform, false);
        var echoRect = echoTextGO.AddComponent<RectTransform>();
        echoRect.anchorMin = Vector2.zero;
        echoRect.anchorMax = Vector2.one;
        echoRect.sizeDelta = new Vector2(-40, -40);  // 内缩 20px 边距
        echoRect.anchoredPosition = Vector2.zero;

        var echoText = echoTextGO.AddComponent<TMP_Text>();
        echoText.text = "";
        echoText.fontSize = 28;
        echoText.color = new Color(1f, 1f, 1f, 0f);  // 初始透明
        echoText.alignment = TextAlignmentOptions.Center;
        echoText.textWrappingMode = TextWrappingModes.Normal;

        // 加载中文字体
        var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Game/Art/source/dialogue_font SDF.asset");
        if (fontAsset != null)
        {
            echoText.font = fontAsset;
        }
        else
        {
            Debug.LogWarning("  ⚠ 未找到 dialogue_font SDF.asset，EchoText 使用默认字体");
        }

        echoTextGO.SetActive(false);

        // 添加 MainBubbleView 并通过反射接线 echoText
        var mainBubbleView = go.AddComponent<MainBubbleView>();
        var mainBubbleViewType = typeof(MainBubbleView);
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        mainBubbleViewType.GetField("echoText", flags)?.SetValue(mainBubbleView, echoText);

        go.SetActive(false);

        return go;
    }

    private static Transform CreateSmallBubblesContainer(Transform parent)
    {
        var go = new GameObject("SmallBubblesContainer");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        Debug.Log("  ✓ 创建小梦泡容器");
        return go.transform;
    }

    private static GameObject CreateDreamCore(Transform parent)
    {
        var go = new GameObject("DreamCore");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(150, 150);

        var img = go.AddComponent<Image>();
        img.color = new Color(1f, 0.8f, 0.6f, 1f);

        // 添加发光子对象
        var glow = new GameObject("Glow");
        glow.transform.SetParent(go.transform, false);
        var glowRect = glow.AddComponent<RectTransform>();
        glowRect.anchorMin = Vector2.zero;
        glowRect.anchorMax = Vector2.one;
        glowRect.sizeDelta = Vector2.zero;
        var glowImg = glow.AddComponent<Image>();
        glowImg.color = new Color(1f, 1f, 0.5f, 0.5f);

        go.AddComponent<DreamCoreView>();

        go.SetActive(false);

        Debug.Log($"  ✓ 创建梦核");
        return go;
    }

    private static GameObject CreateFeifeiCharacter(Transform parent)
    {
        var go = new GameObject("FeifeiCharacter");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(800, 0);
        rect.sizeDelta = new Vector2(200, 400);

        var img = go.AddComponent<Image>();
        img.preserveAspect = true;
        var idleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/Characters/feifei_idle/1.png");
        if (idleSprite != null)
        {
            img.sprite = idleSprite;
            img.color = Color.white;
        }
        else
        {
            Debug.LogWarning("  ⚠ 未找到腓腓待机帧美术资源，回退为占位色块");
            img.color = new Color(1f, 0.9f, 0.8f, 1f);
        }

        var anim = go.AddComponent<Animator>();
        var feifeiController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Game/Animations/PlayerGuide/Feifei.controller");
        if (feifeiController != null)
        {
            anim.runtimeAnimatorController = feifeiController;
        }
        else
        {
            Debug.LogWarning("  ⚠ 未找到 Feifei.controller，腓腓将没有待机动画");
        }

        go.AddComponent<FeifeiCharacterView>();

        go.SetActive(false);

        Debug.Log("  ✓ 创建腓腓角色（已关联美术精灵与 Feifei AnimatorController）");
        return go;
    }

    private static GameObject CreateDialoguePanel(Transform parent)
    {
        var panel = new GameObject("DialoguePanel");
        panel.transform.SetParent(parent, false);
        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.1f, 0.1f);
        rect.anchorMax = new Vector2(0.9f, 0.3f);
        rect.sizeDelta = Vector2.zero;

        var group = panel.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.8f);

        // 说话人
        var speaker = new GameObject("Speaker");
        speaker.transform.SetParent(panel.transform, false);
        var speakerRect = speaker.AddComponent<RectTransform>();
        speakerRect.anchorMin = new Vector2(0, 1);
        speakerRect.anchorMax = new Vector2(0, 1);
        speakerRect.pivot = new Vector2(0, 1);
        speakerRect.anchoredPosition = new Vector2(20, -10);
        speakerRect.sizeDelta = new Vector2(200, 30);
        var speakerText = speaker.AddComponent<TextMeshProUGUI>();
        speakerText.text = "腓腓";
        speakerText.fontSize = 20;
        speakerText.color = Color.yellow;

        // 对话内容
        var content = new GameObject("Content");
        content.transform.SetParent(panel.transform, false);
        var contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 0);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.offsetMin = new Vector2(20, 20);
        contentRect.offsetMax = new Vector2(-20, -50);
        var contentText = content.AddComponent<TextMeshProUGUI>();
        contentText.text = "对话内容";
        contentText.fontSize = 18;
        contentText.color = Color.white;

        panel.AddComponent<DialogueView>();

        panel.SetActive(false);

        Debug.Log($"  ✓ 创建对话面板");
        return panel;
    }

    private static GameObject CreateInputPanel(Transform parent)
    {
        var panel = new GameObject("InputPanel");
        panel.transform.SetParent(parent, false);
        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.2f, 0.3f);
        rect.anchorMax = new Vector2(0.8f, 0.7f);
        rect.sizeDelta = Vector2.zero;

        var group = panel.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.2f, 0.9f);

        // 输入框
        var inputField = new GameObject("InputField");
        inputField.transform.SetParent(panel.transform, false);
        var inputRect = inputField.AddComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.1f, 0.5f);
        inputRect.anchorMax = new Vector2(0.9f, 0.8f);
        inputRect.sizeDelta = Vector2.zero;

        var inputBg = inputField.AddComponent<Image>();
        inputBg.color = Color.white;

        var input = inputField.AddComponent<TMP_InputField>();
        var placeholder = new GameObject("Placeholder");
        placeholder.transform.SetParent(inputField.transform, false);
        var phRect = placeholder.AddComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero;
        phRect.anchorMax = Vector2.one;
        phRect.sizeDelta = Vector2.zero;
        var phText = placeholder.AddComponent<TextMeshProUGUI>();
        phText.text = "写下你的梦境...";
        phText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        phText.fontSize = 16;

        var textArea = new GameObject("Text");
        textArea.transform.SetParent(inputField.transform, false);
        var taRect = textArea.AddComponent<RectTransform>();
        taRect.anchorMin = Vector2.zero;
        taRect.anchorMax = Vector2.one;
        taRect.sizeDelta = Vector2.zero;
        var taText = textArea.AddComponent<TextMeshProUGUI>();
        taText.color = Color.black;
        taText.fontSize = 16;

        input.placeholder = phText;
        input.textComponent = taText;

        // 提交按钮
        var submitBtn = new GameObject("SubmitButton");
        submitBtn.transform.SetParent(panel.transform, false);
        var btnRect = submitBtn.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.4f, 0.2f);
        btnRect.anchorMax = new Vector2(0.6f, 0.35f);
        btnRect.sizeDelta = Vector2.zero;
        var btnImg = submitBtn.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.6f, 1f, 1f);
        var btn = submitBtn.AddComponent<Button>();
        var btnLabel = new GameObject("Label");
        btnLabel.transform.SetParent(submitBtn.transform, false);
        var lblRect = btnLabel.AddComponent<RectTransform>();
        lblRect.anchorMin = Vector2.zero;
        lblRect.anchorMax = Vector2.one;
        lblRect.sizeDelta = Vector2.zero;
        var lblText = btnLabel.AddComponent<TextMeshProUGUI>();
        lblText.text = "确定";
        lblText.alignment = TextAlignmentOptions.Center;
        lblText.color = Color.white;

        panel.AddComponent<DreamInputView>();

        panel.SetActive(false);

        Debug.Log($"  ✓ 创建输入面板");
        return panel;
    }

    private static GameObject CreateTarotPanel(Transform parent)
    {
        var panel = new GameObject("TarotPanel");
        panel.transform.SetParent(parent, false);
        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0.1f, 1f);

        // 卡牌数组容器（同时作为 TarotDrawController.cardParent）
        var cardArray = new GameObject("CardArrayContainer");
        cardArray.transform.SetParent(panel.transform, false);
        var arrayRect = cardArray.AddComponent<RectTransform>();
        arrayRect.anchorMin = Vector2.zero;
        arrayRect.anchorMax = Vector2.one;
        arrayRect.sizeDelta = Vector2.zero;

        // 主卡槽（槽位框 + 卡面 Image 子对象）
        var mainSlot = new GameObject("MainCardSlot");
        mainSlot.transform.SetParent(panel.transform, false);
        var slotRect = mainSlot.AddComponent<RectTransform>();
        slotRect.anchoredPosition = new Vector2(-300, 100);
        slotRect.sizeDelta = new Vector2(150, 250);
        var slotImg = mainSlot.AddComponent<Image>();
        slotImg.color = new Color(0.3f, 0.3f, 0.4f, 0.8f);

        var mainCardFace = new GameObject("CardFace");
        mainCardFace.transform.SetParent(mainSlot.transform, false);
        var mainCardFaceRect = mainCardFace.AddComponent<RectTransform>();
        mainCardFaceRect.anchorMin = Vector2.zero;
        mainCardFaceRect.anchorMax = Vector2.one;
        mainCardFaceRect.sizeDelta = Vector2.zero;
        var mainCardImage = mainCardFace.AddComponent<Image>();
        mainCardImage.preserveAspect = true;
        mainCardImage.enabled = false;

        // 3个情绪卡槽
        var emotionSlots = new Transform[3];
        for (int i = 0; i < 3; i++)
        {
            var emotionSlot = new GameObject($"EmotionSlot{i + 1}");
            emotionSlot.transform.SetParent(panel.transform, false);
            var eRect = emotionSlot.AddComponent<RectTransform>();
            eRect.anchoredPosition = new Vector2(100 + i * 180, 100);
            eRect.sizeDelta = new Vector2(120, 200);
            var eImg = emotionSlot.AddComponent<Image>();
            eImg.color = new Color(0.2f, 0.2f, 0.3f, 0.6f);
            emotionSlots[i] = emotionSlot.transform;
        }

        // 塔罗流程内嵌对话面板（State A/B/D 引导对话使用）
        var tarotDialoguePanel = CreateTarotDialoguePanel(panel.transform);

        // 添加 TarotDrawController 并绑定其内部字段（美术 Prefab + 塔罗牌数据）
        var tarotController = panel.AddComponent<TarotDrawController>();
        BindTarotDrawController(tarotController, cardArray, mainSlot, mainCardImage, emotionSlots, tarotDialoguePanel);

        panel.AddComponent<TarotView>();

        panel.SetActive(false);

        Debug.Log("  ✓ 创建塔罗面板（已关联卡牌 Prefab 与 4 张塔罗牌数据）");
        return panel;
    }

    private static GameObject CreateTarotDialoguePanel(Transform parent)
    {
        var panel = new GameObject("TarotDialoguePanel");
        panel.transform.SetParent(parent, false);
        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.1f, 0.1f);
        rect.anchorMax = new Vector2(0.9f, 0.3f);
        rect.sizeDelta = Vector2.zero;

        var group = panel.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.8f);

        var speaker = new GameObject("Speaker");
        speaker.transform.SetParent(panel.transform, false);
        var speakerRect = speaker.AddComponent<RectTransform>();
        speakerRect.anchorMin = new Vector2(0, 1);
        speakerRect.anchorMax = new Vector2(0, 1);
        speakerRect.pivot = new Vector2(0, 1);
        speakerRect.anchoredPosition = new Vector2(20, -10);
        speakerRect.sizeDelta = new Vector2(200, 30);
        var speakerText = speaker.AddComponent<TextMeshProUGUI>();
        speakerText.text = "腓腓";
        speakerText.fontSize = 20;
        speakerText.color = Color.yellow;

        var content = new GameObject("Content");
        content.transform.SetParent(panel.transform, false);
        var contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 0);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.offsetMin = new Vector2(20, 20);
        contentRect.offsetMax = new Vector2(-20, -50);
        var contentText = content.AddComponent<TextMeshProUGUI>();
        contentText.text = "对话内容";
        contentText.fontSize = 18;
        contentText.color = Color.white;

        panel.SetActive(false);

        Debug.Log("    · 创建塔罗内嵌对话面板");
        return panel;
    }

    private static void BindTarotDrawController(
        TarotDrawController controller, GameObject cardArrayContainer,
        GameObject mainCardSlot, Image mainCardImage, Transform[] emotionCardSlots,
        GameObject dialoguePanel
    )
    {
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var type = typeof(TarotDrawController);

        var cardPrefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/PlayerGuide/TarotCardPrefab.prefab");
        if (cardPrefabAsset == null)
        {
            Debug.LogWarning("  ⚠ 未找到 TarotCardPrefab.prefab，塔罗卡牌阵列将无法生成");
        }

        const string cardsFolder = "Assets/Game/Resources/PlayerGuide/TarotCards/";
        var mainCard = AssetDatabase.LoadAssetAtPath<TarotCardData>(cardsFolder + "Card_YingLong.asset");
        var emotionCard1 = AssetDatabase.LoadAssetAtPath<TarotCardData>(cardsFolder + "Card_ChanChuJingPo.asset");
        var emotionCard2 = AssetDatabase.LoadAssetAtPath<TarotCardData>(cardsFolder + "Card_ChangXi.asset");
        var emotionCard3 = AssetDatabase.LoadAssetAtPath<TarotCardData>(cardsFolder + "Card_RiYueLunZhuan.asset");
        var fixedCards = new[] { mainCard, emotionCard1, emotionCard2, emotionCard3 };
        if (mainCard == null || emotionCard1 == null || emotionCard2 == null || emotionCard3 == null)
        {
            Debug.LogWarning("  ⚠ 部分塔罗牌 ScriptableObject 资源未找到，请检查 " + cardsFolder);
        }

        type.GetField("fixedCards", flags)?.SetValue(controller, fixedCards);
        type.GetField("dialoguePanel", flags)?.SetValue(controller, dialoguePanel);
        type.GetField("dialogueSpeakerText", flags)?.SetValue(controller, dialoguePanel.transform.Find("Speaker")?.GetComponent<TMP_Text>());
        type.GetField("dialogueContentText", flags)?.SetValue(controller, dialoguePanel.transform.Find("Content")?.GetComponent<TMP_Text>());
        type.GetField("dialogueCanvasGroup", flags)?.SetValue(controller, dialoguePanel.GetComponent<CanvasGroup>());
        type.GetField("cardArrayContainer", flags)?.SetValue(controller, cardArrayContainer);
        type.GetField("cardPrefab", flags)?.SetValue(controller, cardPrefabAsset);
        type.GetField("cardParent", flags)?.SetValue(controller, cardArrayContainer.transform);
        type.GetField("mainCardSlot", flags)?.SetValue(controller, mainCardSlot);
        type.GetField("mainCardImage", flags)?.SetValue(controller, mainCardImage);
        type.GetField("emotionCardSlots", flags)?.SetValue(controller, emotionCardSlots);

        EditorUtility.SetDirty(controller);
        Debug.Log("  ✓ 已绑定 TarotDrawController 内部字段（美术卡牌 Prefab + 4 张塔罗牌数据）");
    }

    private static void BindFieldsToNewMvc(
        PlayerGuideController controller, PlayerGuideView view, SmallBubblesView smallBubblesView,
        ParticleSystem particles, GameObject backgroundGO, GameObject bubble,
        GameObject core, GameObject feifei, GameObject dialoguePanel,
        GameObject inputPanel, GameObject tarotPanel,
        Transform smallBubblesContainer, GameObject smallBubblePrefab
    )
    {
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

        var bgGroup = backgroundGO.GetComponent<CanvasGroup>();
        var bgAudio = backgroundGO.GetComponent<AudioSource>();
        var bgImage = backgroundGO.GetComponent<Image>();
        var bubbleVisual = bubble.GetComponent<PlayerGuideMainBubbleVisual>();
        var bubbleButton = bubble.GetComponent<Button>();
        var coreGlow = core.transform.Find("Glow")?.GetComponent<Image>();
        var feifeiAnimator = feifei.GetComponent<Animator>();
        var dialogueSpeakerText = dialoguePanel.transform.Find("Speaker")?.GetComponent<TMP_Text>();
        var dialogueContentText = dialoguePanel.transform.Find("Content")?.GetComponent<TMP_Text>();
        var dialogueCanvasGroup = dialoguePanel.GetComponent<CanvasGroup>();
        var dreamInputField = inputPanel.transform.Find("InputField")?.GetComponent<TMP_InputField>();
        var inputPlaceholderText = inputPanel.transform.Find("InputField/Placeholder")?.GetComponent<TMP_Text>();
        var inputSubmitButton = inputPanel.transform.Find("SubmitButton")?.GetComponent<Button>();
        var inputCanvasGroup = inputPanel.GetComponent<CanvasGroup>();
        var tarotController = tarotPanel.GetComponent<TarotDrawController>();

        var dialogueView = dialoguePanel.GetComponent<DialogueView>();
        var dreamInputView = inputPanel.GetComponent<DreamInputView>();
        var tarotView = tarotPanel.GetComponent<TarotView>();
        var feifeiView = feifei.GetComponent<FeifeiCharacterView>();
        var dreamCoreView = core.GetComponent<DreamCoreView>();
        var mainBubbleView = bubble.GetComponent<MainBubbleView>();
        var backgroundView = backgroundGO.GetComponent<BackgroundView>();

        // --- PlayerGuideController 的 9 个 View 引用 ---
        var controllerType = typeof(PlayerGuideController);
        controllerType.GetField("view", flags)?.SetValue(controller, view);
        controllerType.GetField("dialogueView", flags)?.SetValue(controller, dialogueView);
        controllerType.GetField("dreamInputView", flags)?.SetValue(controller, dreamInputView);
        controllerType.GetField("tarotView", flags)?.SetValue(controller, tarotView);
        controllerType.GetField("feifeiView", flags)?.SetValue(controller, feifeiView);
        controllerType.GetField("dreamCoreView", flags)?.SetValue(controller, dreamCoreView);
        controllerType.GetField("smallBubblesView", flags)?.SetValue(controller, smallBubblesView);
        controllerType.GetField("mainBubbleView", flags)?.SetValue(controller, mainBubbleView);
        controllerType.GetField("backgroundView", flags)?.SetValue(controller, backgroundView);

        // --- PlayerGuideView 自身字段（共享配置）---
        var viewType = typeof(PlayerGuideView);
        viewType.GetField("smallBubblesContainer", flags)?.SetValue(view, smallBubblesContainer);
        viewType.GetField("smallBubblePrefab", flags)?.SetValue(view, smallBubblePrefab);
        viewType.GetField("dreamSpaceParticles", flags)?.SetValue(view, particles);
        viewType.GetField("backgroundCanvasGroup", flags)?.SetValue(view, bgGroup);
        viewType.GetField("mainBubbleVisual", flags)?.SetValue(view, bubbleVisual);
        viewType.GetField("mainBubbleButton", flags)?.SetValue(view, bubbleButton);
        viewType.GetField("dreamEchoAudioSource", flags)?.SetValue(view, bgAudio);
        var earlyBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/Background/ChatGPT Image 2026年6月24日 13_20_15 (2).png");
        var lateBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/Background/ChatGPT Image 2026年6月22日 21_47_05.png");
        viewType.GetField("dreamBackgroundSpriteEarly", flags)?.SetValue(view, earlyBgSprite);
        viewType.GetField("dreamBackgroundSpriteLate", flags)?.SetValue(view, lateBgSprite);
        viewType.GetField("dreamCoreObject", flags)?.SetValue(view, core);
        viewType.GetField("dreamCoreGlow", flags)?.SetValue(view, coreGlow);
        viewType.GetField("feifeiCharacter", flags)?.SetValue(view, feifei);
        viewType.GetField("feifeiAnimator", flags)?.SetValue(view, feifeiAnimator);
        var frame6Config = AssetDatabase.LoadAssetAtPath<Frame6DialogueConfig>("Assets/Game/Resources/PlayerGuide/Frame6DialogueConfig.asset");
        if (frame6Config == null)
        {
            Debug.LogWarning("  ⚠ 未找到 Frame6DialogueConfig.asset，请手动在 PlayerGuideView Inspector 中关联");
        }
        viewType.GetField("frame6Config", flags)?.SetValue(view, frame6Config);
        viewType.GetField("dialoguePanel", flags)?.SetValue(view, dialoguePanel);
        viewType.GetField("dialogueSpeakerText", flags)?.SetValue(view, dialogueSpeakerText);
        viewType.GetField("dialogueContentText", flags)?.SetValue(view, dialogueContentText);
        viewType.GetField("dialogueCanvasGroup", flags)?.SetValue(view, dialogueCanvasGroup);
        viewType.GetField("inputPanel", flags)?.SetValue(view, inputPanel);
        viewType.GetField("dreamInputField", flags)?.SetValue(view, dreamInputField);
        viewType.GetField("inputPlaceholderText", flags)?.SetValue(view, inputPlaceholderText);
        viewType.GetField("inputSubmitButton", flags)?.SetValue(view, inputSubmitButton);
        viewType.GetField("inputCanvasGroup", flags)?.SetValue(view, inputCanvasGroup);
        viewType.GetField("tarotPanel", flags)?.SetValue(view, tarotPanel);
        viewType.GetField("tarotController", flags)?.SetValue(view, tarotController);

        // --- 各个 View 组件自身字段 ---
        var dialogueViewType = typeof(DialogueView);
        dialogueViewType.GetField("dialoguePanel", flags)?.SetValue(dialogueView, dialoguePanel);
        dialogueViewType.GetField("speakerText", flags)?.SetValue(dialogueView, dialogueSpeakerText);
        dialogueViewType.GetField("contentText", flags)?.SetValue(dialogueView, dialogueContentText);
        dialogueViewType.GetField("canvasGroup", flags)?.SetValue(dialogueView, dialogueCanvasGroup);

        var dreamInputViewType = typeof(DreamInputView);
        dreamInputViewType.GetField("inputPanel", flags)?.SetValue(dreamInputView, inputPanel);
        dreamInputViewType.GetField("dreamInputField", flags)?.SetValue(dreamInputView, dreamInputField);
        dreamInputViewType.GetField("placeholderText", flags)?.SetValue(dreamInputView, inputPlaceholderText);
        dreamInputViewType.GetField("submitButton", flags)?.SetValue(dreamInputView, inputSubmitButton);
        dreamInputViewType.GetField("canvasGroup", flags)?.SetValue(dreamInputView, inputCanvasGroup);

        var tarotViewType = typeof(TarotView);
        tarotViewType.GetField("tarotPanel", flags)?.SetValue(tarotView, tarotPanel);
        tarotViewType.GetField("tarotController", flags)?.SetValue(tarotView, tarotController);

        var feifeiViewType = typeof(FeifeiCharacterView);
        feifeiViewType.GetField("feifeiCharacter", flags)?.SetValue(feifeiView, feifei);
        feifeiViewType.GetField("feifeiAnimator", flags)?.SetValue(feifeiView, feifeiAnimator);

        var dreamCoreViewType = typeof(DreamCoreView);
        dreamCoreViewType.GetField("dreamCoreObject", flags)?.SetValue(dreamCoreView, core);
        dreamCoreViewType.GetField("dreamCoreGlow", flags)?.SetValue(dreamCoreView, coreGlow);

        var mainBubbleViewType = typeof(MainBubbleView);
        mainBubbleViewType.GetField("mainBubbleVisual", flags)?.SetValue(mainBubbleView, bubbleVisual);
        mainBubbleViewType.GetField("mainBubbleButton", flags)?.SetValue(mainBubbleView, bubbleButton);

        var backgroundViewType = typeof(BackgroundView);
        backgroundViewType.GetField("backgroundCanvasGroup", flags)?.SetValue(backgroundView, bgGroup);
        backgroundViewType.GetField("dreamEchoAudioSource", flags)?.SetValue(backgroundView, bgAudio);
        backgroundViewType.GetField("backgroundImage", flags)?.SetValue(backgroundView, bgImage);

        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(view);
        EditorUtility.SetDirty(dialogueView);
        EditorUtility.SetDirty(dreamInputView);
        EditorUtility.SetDirty(tarotView);
        EditorUtility.SetDirty(feifeiView);
        EditorUtility.SetDirty(dreamCoreView);
        EditorUtility.SetDirty(mainBubbleView);
        EditorUtility.SetDirty(backgroundView);

        Debug.Log("  ✓ 已绑定所有字段引用（新 MVC 系统）");
    }

    private static string GetHierarchyPath(Transform t)
    {
        var path = t.name;
        var parent = t.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }
}
