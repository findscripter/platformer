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

        // 创建 V2ControllerRoot
        var v2Root = new GameObject("V2ControllerRoot");
        v2Root.transform.SetParent(canvas, false);
        var rectTransform = v2Root.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;

        // 添加 PlayerGuideFlowControllerV2 组件
        var controller = v2Root.AddComponent<PlayerGuideFlowControllerV2>();

        // === Frame 1-3: 梦境生成与梦泡 ===
        var dreamSpaceParticles = CreateParticleSystem(v2Root.transform, "DreamSpaceParticles");
        var backgroundGroup = CreateCanvasGroup(v2Root.transform, "BackgroundCanvasGroup");
        var mainBubble = CreateMainBubble(v2Root.transform);

        // === Frame 4-5: 梦核与腓腓 ===
        var dreamCore = CreateDreamCore(v2Root.transform);
        var feifei = CreateFeifeiCharacter(v2Root.transform);

        // === Frame 6: 对话系统 ===
        var dialoguePanel = CreateDialoguePanel(v2Root.transform);

        // === Frame 7-8: 输入系统 ===
        var inputPanel = CreateInputPanel(v2Root.transform);

        // === Frame 9: 塔罗系统 ===
        var tarotPanel = CreateTarotPanel(v2Root.transform);

        // === 绑定引用到 Controller（通过反射，因为字段是 SerializeField private）===
        BindFieldsToController(controller,
            dreamSpaceParticles, backgroundGroup, mainBubble,
            dreamCore, feifei, dialoguePanel, inputPanel, tarotPanel
        );

        // 保存场景
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

        Debug.Log("✓ PlayerGuide V2 场景搭建完成！");
        Debug.Log($"  - 控制器位置: {GetHierarchyPath(v2Root.transform)}");
        Debug.Log("  - 请手动关联以下资源:");
        Debug.Log("    1. Frame6DialogueConfig（Inspector 中）");
        Debug.Log("    2. 卡牌 Prefab（TarotDrawController 需要）");
        Debug.Log("    3. 腓腓 Animator（需要角色动画控制器）");
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

    private static CanvasGroup CreateCanvasGroup(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        var group = go.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        // 添加背景图
        var img = go.AddComponent<Image>();
        img.color = new Color(0.05f, 0.05f, 0.15f, 1f);

        Debug.Log($"  ✓ 创建背景组: {name}");
        return group;
    }

    private static GameObject CreateMainBubble(Transform parent)
    {
        var go = new GameObject("MainBubble");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(300, 300);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.8f, 0.9f, 1f, 0.5f);

        var btn = go.AddComponent<Button>();
        btn.interactable = false;

        // 使用 EnsureStructure 自动添加 PlayerGuideMainBubbleVisual 并重构层级
        PlayerGuideMainBubbleVisual.EnsureStructure(go);

        go.SetActive(false);

        Debug.Log($"  ✓ 创建主梦泡（已添加 PlayerGuideMainBubbleVisual）");
        return go;
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
        img.color = new Color(1f, 0.9f, 0.8f, 1f);

        var anim = go.AddComponent<Animator>();

        go.SetActive(false);

        Debug.Log($"  ✓ 创建腓腓角色");
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

        // 卡牌数组容器
        var cardArray = new GameObject("CardArrayContainer");
        cardArray.transform.SetParent(panel.transform, false);
        var arrayRect = cardArray.AddComponent<RectTransform>();
        arrayRect.anchorMin = Vector2.zero;
        arrayRect.anchorMax = Vector2.one;
        arrayRect.sizeDelta = Vector2.zero;

        // 主卡槽
        var mainSlot = new GameObject("MainCardSlot");
        mainSlot.transform.SetParent(panel.transform, false);
        var slotRect = mainSlot.AddComponent<RectTransform>();
        slotRect.anchoredPosition = new Vector2(-300, 100);
        slotRect.sizeDelta = new Vector2(150, 250);
        var slotImg = mainSlot.AddComponent<Image>();
        slotImg.color = new Color(0.3f, 0.3f, 0.4f, 0.8f);

        // 3个情绪卡槽
        for (int i = 0; i < 3; i++)
        {
            var emotionSlot = new GameObject($"EmotionSlot{i + 1}");
            emotionSlot.transform.SetParent(panel.transform, false);
            var eRect = emotionSlot.AddComponent<RectTransform>();
            eRect.anchoredPosition = new Vector2(100 + i * 180, 100);
            eRect.sizeDelta = new Vector2(120, 200);
            var eImg = emotionSlot.AddComponent<Image>();
            eImg.color = new Color(0.2f, 0.2f, 0.3f, 0.6f);
        }

        // 添加 TarotDrawController
        var controller = panel.AddComponent<TarotDrawController>();

        panel.SetActive(false);

        Debug.Log($"  ✓ 创建塔罗面板");
        return panel;
    }

    private static void BindFieldsToController(
        PlayerGuideFlowControllerV2 controller,
        ParticleSystem particles, CanvasGroup bgGroup, GameObject bubble,
        GameObject core, GameObject feifei, GameObject dialoguePanel,
        GameObject inputPanel, GameObject tarotPanel
    )
    {
        var type = typeof(PlayerGuideFlowControllerV2);
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

        type.GetField("dreamSpaceParticles", flags)?.SetValue(controller, particles);
        type.GetField("backgroundCanvasGroup", flags)?.SetValue(controller, bgGroup);
        type.GetField("mainBubbleVisual", flags)?.SetValue(controller, bubble.GetComponent<PlayerGuideMainBubbleVisual>());
        type.GetField("mainBubbleButton", flags)?.SetValue(controller, bubble.GetComponent<Button>());

        type.GetField("dreamCoreObject", flags)?.SetValue(controller, core);
        type.GetField("dreamCoreGlow", flags)?.SetValue(controller, core.transform.Find("Glow")?.GetComponent<Image>());
        type.GetField("feifeiCharacter", flags)?.SetValue(controller, feifei);
        type.GetField("feifeiAnimator", flags)?.SetValue(controller, feifei.GetComponent<Animator>());

        type.GetField("dialoguePanel", flags)?.SetValue(controller, dialoguePanel);
        type.GetField("dialogueSpeakerText", flags)?.SetValue(controller, dialoguePanel.transform.Find("Speaker")?.GetComponent<TMP_Text>());
        type.GetField("dialogueContentText", flags)?.SetValue(controller, dialoguePanel.transform.Find("Content")?.GetComponent<TMP_Text>());
        type.GetField("dialogueCanvasGroup", flags)?.SetValue(controller, dialoguePanel.GetComponent<CanvasGroup>());

        type.GetField("inputPanel", flags)?.SetValue(controller, inputPanel);
        type.GetField("dreamInputField", flags)?.SetValue(controller, inputPanel.transform.Find("InputField")?.GetComponent<TMP_InputField>());
        type.GetField("inputPlaceholderText", flags)?.SetValue(controller, inputPanel.transform.Find("InputField/Placeholder")?.GetComponent<TMP_Text>());
        type.GetField("inputSubmitButton", flags)?.SetValue(controller, inputPanel.transform.Find("SubmitButton")?.GetComponent<Button>());
        type.GetField("inputCanvasGroup", flags)?.SetValue(controller, inputPanel.GetComponent<CanvasGroup>());

        type.GetField("tarotPanel", flags)?.SetValue(controller, tarotPanel);
        type.GetField("tarotController", flags)?.SetValue(controller, tarotPanel.GetComponent<TarotDrawController>());

        EditorUtility.SetDirty(controller);

        Debug.Log("  ✓ 已绑定所有字段引用");
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
