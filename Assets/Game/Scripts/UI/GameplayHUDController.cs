using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏内 HUD（血条 + 收集品计数）。运行时自建，避免手工拼 YAML 层级。
///
/// 数据源：
/// - 血量：<see cref="PlayerController.CurrentHealth"/> / <see cref="PlayerController.MaxHealth"/>
///   （首次出现时建好血格，之后每帧只改填充颜色，不反复重建）。
/// - 收集品：<see cref="CollectibleTracker.CountChanged"/> 事件，显示「已得/总数」。
///
/// 挂在 Canvas_HUD 上。Canvas_HUD 是 PersistentRoot 里唯一的空壳 Canvas
/// （HealthBar/EnergyBar/SkillIcons/CollectibleCounter 都是无 UI 组件的空 Transform），
/// 由其父级 <see cref="UIManager.ShowGameplayUI"/> 激活，并在激活时补挂本组件。
///
/// 设计稿风格：黑水墨梦境。左上角只显示血格；梦核图标和「梦核碎片」计数在右上角。
/// </summary>
public class GameplayHUDController : MonoBehaviour
{
    [Header("图标与背景精灵（可空，缺省用纯色 Image 兜底）")]
    [SerializeField] private Sprite healthIconSprite;      // 虹彩梦滴 S01/26
    [SerializeField] private Sprite collectIconSprite;     // 逗号标点 Dialogues/4
    [SerializeField] private Sprite panelSprite;           // 发光圆片 circle_sprite

    private const string CollectIconGuid = "50089d2a43f97f1478dc32c428473d8a";    // Dialogues/4
    private const string PanelSpriteGuid = "e79b4d6c863b4124e9f6b264f6d371cb"; // circle_sprite

    private TMP_FontAsset cachedFont;

    private RectTransform healthBarPanel;
    private RectTransform healthFillMask;
    private RectTransform[] healthSegments = new RectTransform[0];
    private Image[] healthSegmentImages = new Image[0];
    private Image[] healthSlotImages = new Image[0];
    private TMP_Text healthText;
    private TMP_Text collectText;

    private int lastMaxHealth = -1;
    private int lastHealth = -1;
    private int lastCollectCount = -1;
    private int lastCollectSpawned = -1;

    private PlayerController player;
    private CollectibleTracker tracker;
    private float nextBindingAttempt;

    private void Awake()
    {
        GuideUiLayout.ConfigureCanvas(this);
        CacheFont();
        BuildHud();
    }

    private void Update()
    {
        ResolveBindings();
        UpdateHealth();
        UpdateCollectible();
    }

    private void ResolveBindings()
    {
        if ((player != null && tracker != null) || Time.unscaledTime < nextBindingAttempt)
            return;
        nextBindingAttempt = Time.unscaledTime + 0.5f;
        if (player == null)
        {
            player = Object.FindAnyObjectByType<PlayerController>();
        }

        if (tracker == null)
        {
            tracker = CollectibleTracker.Active != null
                ? CollectibleTracker.Active
                : Object.FindAnyObjectByType<CollectibleTracker>();
        }
    }

    private void CacheFont()
    {
        if (cachedFont != null)
        {
            return;
        }

        cachedFont = GuideUiLayout.LoadFont();
    }

    private Sprite LoadSpriteByGuid(string guid, string fallbackPath)
    {
        if (string.IsNullOrEmpty(guid))
        {
            return LoadSpriteByPath(fallbackPath);
        }

        var loaded = GuideArt.Load(guid);
        if (loaded != null)
        {
            return loaded;
        }
        return LoadSpriteByPath(fallbackPath);
    }

    private Sprite LoadSpriteByPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
#else
        return null; // 发布包由 GuideArt 的美术目录解析，不把 Assets 路径误作 Resources 路径。
#endif
    }

    private void BuildHud()
    {
        // HUD 画布只承载浮动信息，不保留铺满顶部的原型底色。
        var canvasBackground = GetComponent<Image>();
        if (canvasBackground != null)
            canvasBackground.enabled = false;
        // 梦核图标只给右侧碎片计数；左上角血条不再用它，避免当成收集物。
        if (collectIconSprite == null)
            collectIconSprite = GuideArt.DreamCore;
        if (collectIconSprite == null)
            collectIconSprite = LoadSpriteByGuid(CollectIconGuid, "Assets/Game/Art/UI/Dialogues/4.png");

        if (panelSprite == null)
        {
            panelSprite = LoadSpriteByGuid(PanelSpriteGuid, "Assets/Game/Art/UI/circle_sprite.png");
        }

        healthBarPanel = BuildHealthBar();
        collectText = BuildCollectibleCounter();
        BuildControlsHint();
    }

    private RectTransform BuildHealthBar()
    {
        var panelGo = new GameObject("HUD_HealthBar", typeof(RectTransform), typeof(Image));
        panelGo.transform.SetParent(transform, false);
        var panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(28f, -24f);
        panelRect.sizeDelta = new Vector2(320f, 64f);

        var panelImage = panelGo.GetComponent<Image>();
        StylePanel(panelImage);

        healthText = BuildText("HealthText", panelGo.transform,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-16f, 0f), new Vector2(70f, 40f), 26, TextAlignmentOptions.MidlineRight);

        var slotsGo = new GameObject("HealthSlots", typeof(RectTransform));
        slotsGo.transform.SetParent(panelGo.transform, false);
        var slotsRect = slotsGo.GetComponent<RectTransform>();
        slotsRect.anchorMin = new Vector2(0f, 0.5f);
        slotsRect.anchorMax = new Vector2(0f, 0.5f);
        slotsRect.pivot = new Vector2(0f, 0.5f);
        slotsRect.anchoredPosition = new Vector2(16f, 0f);
        slotsRect.sizeDelta = new Vector2(220f, 32f);

        healthFillMask = slotsRect;
        healthSegments = new RectTransform[0];
        healthSegmentImages = new Image[0];
        healthSlotImages = new Image[0];

        return panelRect;
    }

    private TMP_Text BuildCollectibleCounter()
    {
        var panelGo = new GameObject("HUD_CollectibleCounter", typeof(RectTransform), typeof(Image));
        panelGo.transform.SetParent(transform, false);
        var panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-28f, -24f);
        panelRect.sizeDelta = new Vector2(260f, 52f);

        var panelImage = panelGo.GetComponent<Image>();
        StylePanel(panelImage);

        var iconGo = new GameObject("CollectIcon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(panelGo.transform, false);
        var iconRect = iconGo.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(10f, 0f);
        iconRect.sizeDelta = new Vector2(32f, 40f);
        var iconImage = iconGo.GetComponent<Image>();
        iconImage.sprite = collectIconSprite;
        iconImage.color = Color.white;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        return BuildText("CollectText", panelGo.transform,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(22f, 0f), new Vector2(-56f, 0f), 24, TextAlignmentOptions.Center);
    }

    private void BuildControlsHint()
    {
        BuildText("HUD_ControlsHint", transform,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 18f), new Vector2(920f, 36f), 20, TextAlignmentOptions.Center).text =
            "跳跃 Space / W    攻击 J / 鼠标左键    交互 E / F    梦核碎片靠近即拾取";
    }

    private static void StylePanel(Image image)
    {
        image.sprite = GuideArt.DialogueBanner;
        image.type = Image.Type.Sliced;
        image.color = new Color(0.12f, 0.11f, 0.13f, 0.88f);
        image.raycastTarget = false;
    }

    private TMP_Text BuildText(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        var text = go.GetComponent<TMP_Text>();
        text.font = cachedFont;
        text.fontSize = fontSize;
        text.color = new Color(0.98f, 0.95f, 0.86f, 1f);
        text.enableAutoSizing = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.outlineWidth = 0.06f;
        text.outlineColor = new Color(0f, 0f, 0f, 0.8f);
        return text;
    }

    private void UpdateHealth()
    {
        if (player == null)
        {
            return;
        }

        int max = Mathf.Max(1, player.MaxHealth);
        int current = Mathf.Clamp(player.CurrentHealth, 0, max);

        bool maximumChanged = max != lastMaxHealth;
        if (maximumChanged)
        {
            RebuildHealthSegments(max);
            lastMaxHealth = max;
        }

        if (current != lastHealth || maximumChanged)
        {
            ApplyHealth(current);
            lastHealth = current;
            if (healthText != null)
                healthText.text = $"{current}/{max}";
        }
    }

    private void RebuildHealthSegments(int max)
    {
        for (int i = 0; i < healthSegments.Length; i++)
        {
            if (healthSegments[i] != null)
            {
                Destroy(healthSegments[i].gameObject);
            }
        }

        healthSegments = new RectTransform[max];
        healthSegmentImages = new Image[max];
        healthSlotImages = new Image[max];

        float availableWidth = healthFillMask.rect.width;
        float gap = Mathf.Min(8f, availableWidth / (max * 4f));
        float slotWidth = Mathf.Min(28f, (availableWidth - gap * (max - 1)) / max);

        for (int i = 0; i < max; i++)
        {
            float x = slotWidth * 0.5f + i * (slotWidth + gap);

            var slotGo = new GameObject($"Slot_{i}", typeof(RectTransform), typeof(Image));
            slotGo.transform.SetParent(healthFillMask, false);
            var slotRect = slotGo.GetComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0f, 0.5f);
            slotRect.anchorMax = new Vector2(0f, 0.5f);
            slotRect.pivot = new Vector2(0.5f, 0.5f);
            slotRect.anchoredPosition = new Vector2(x, 0f);
            slotRect.sizeDelta = new Vector2(slotWidth, slotWidth);
            var slotImage = slotGo.GetComponent<Image>();
            if (panelSprite != null)
            {
                slotImage.sprite = panelSprite;
            }
            slotImage.color = new Color(0.55f, 0.49f, 0.4f, 0.65f);
            slotImage.preserveAspect = true;
            slotImage.raycastTarget = false;

            var fillGo = new GameObject($"Fill_{i}", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(slotGo.transform, false);
            var fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(3f, 3f);
            fillRect.offsetMax = new Vector2(-3f, -3f);
            var fillImage = fillGo.GetComponent<Image>();
            if (panelSprite != null)
            {
                fillImage.sprite = panelSprite;
            }
            fillImage.color = Color.white;
            fillImage.preserveAspect = true;
            fillImage.raycastTarget = false;

            healthSegments[i] = slotRect;
            healthSegmentImages[i] = fillImage;
            healthSlotImages[i] = slotImage;
        }

        ApplyHealth(Mathf.Clamp(player.CurrentHealth, 0, max));
    }

    private void ApplyHealth(int current)
    {
        for (int i = 0; i < healthSegmentImages.Length; i++)
        {
            if (healthSegmentImages[i] == null)
            {
                continue;
            }

            healthSegmentImages[i].color = i < current
                ? new Color(0.98f, 0.89f, 0.7f, 1f)
                : new Color(0.28f, 0.25f, 0.23f, 0.4f);
        }
    }

    private void UpdateCollectible()
    {
        if (tracker == null)
        {
            return;
        }

        int collected = tracker.AreaACollected;
        int spawned = tracker.AreaASpawned;

        if (collected == lastCollectCount && spawned == lastCollectSpawned)
        {
            return;
        }

        lastCollectCount = collected;
        lastCollectSpawned = spawned;

        if (collectText != null)
        {
            collectText.text = spawned > 0
                ? $"梦核碎片 {collected}/{spawned}"
                : $"梦核碎片 {tracker.CurrentCount}";
        }
    }
}
