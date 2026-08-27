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
/// 设计稿风格：黑水墨梦境。血条用虹彩梦滴 (S01/26) 作图标，血格/底条用发光描边圆片
/// (UI/circle_sprite)，收集品用白色发光逗号标点 (UI/Dialogues/4)。
/// </summary>
public class GameplayHUDController : MonoBehaviour
{
    [Header("图标与背景精灵（可空，缺省用纯色 Image 兜底）")]
    [SerializeField] private Sprite healthIconSprite;      // 虹彩梦滴 S01/26
    [SerializeField] private Sprite collectIconSprite;     // 逗号标点 Dialogues/4
    [SerializeField] private Sprite panelSprite;           // 发光圆片 circle_sprite

    private const string FontAssetGuid = "7e794ed8003821b4dac6b110719e0135";
    private const string HealthIconGuid = "d506798f8970fb645bec879362217bd6";   // S01/26
    private const string CollectIconGuid = "";                                  // 运行时按路径兜底
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

    private PlayerController player;
    private CollectibleTracker tracker;

    private void Awake()
    {
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
        if (player == null)
        {
            player = Object.FindAnyObjectByType<PlayerController>();
        }

        if (tracker == null)
        {
            tracker = Object.FindAnyObjectByType<CollectibleTracker>();
        }
    }

    private void CacheFont()
    {
        if (cachedFont != null)
        {
            return;
        }

        TMP_FontAsset asset = null;
#if UNITY_EDITOR
        asset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            UnityEditor.AssetDatabase.GUIDToAssetPath(FontAssetGuid));
#else
        asset = Resources.Load<TMP_FontAsset>("Game/Art/source/dialogue_font_TMP");
#endif
        cachedFont = asset != null ? asset : TMP_Settings.defaultFontAsset;
    }

    private Sprite LoadSpriteByGuid(string guid, string fallbackPath)
    {
        if (string.IsNullOrEmpty(guid))
        {
            return LoadSpriteByPath(fallbackPath);
        }

#if UNITY_EDITOR
        var loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
            UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
        if (loaded != null)
        {
            return loaded;
        }
#endif
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
        return Resources.Load<Sprite>(path.Substring("Assets/".Length).Replace(".png", ""));
#endif
    }

    private void BuildHud()
    {
        if (healthIconSprite == null)
        {
            healthIconSprite = LoadSpriteByGuid(HealthIconGuid, "Assets/Game/Art/Scenes/S01/26.png");
        }

        if (collectIconSprite == null)
        {
            collectIconSprite = LoadSpriteByGuid(CollectIconGuid, "Assets/Game/Art/UI/Dialogues/4.png");
        }

        if (panelSprite == null)
        {
            panelSprite = LoadSpriteByGuid(PanelSpriteGuid, "Assets/Game/Art/UI/circle_sprite.png");
        }

        healthBarPanel = BuildHealthBar();
        collectText = BuildCollectibleCounter();
    }

    private RectTransform BuildHealthBar()
    {
        var panelGo = new GameObject("HUD_HealthBar", typeof(RectTransform), typeof(Image));
        panelGo.transform.SetParent(transform, false);
        var panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(24f, -20f);
        panelRect.sizeDelta = new Vector2(280f, 52f);

        var panelImage = panelGo.GetComponent<Image>();
        if (panelSprite != null)
        {
            panelImage.sprite = panelSprite;
        }
        panelImage.color = new Color(0.05f, 0.08f, 0.12f, 0.55f);

        var iconGo = new GameObject("HealthIcon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(panelGo.transform, false);
        var iconRect = iconGo.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(8f, 0f);
        iconRect.sizeDelta = new Vector2(34f, 34f);
        var iconImage = iconGo.GetComponent<Image>();
        iconImage.sprite = healthIconSprite;
        iconImage.color = Color.white;
        iconImage.preserveAspect = true;

        healthText = BuildText("HealthText", panelGo.transform,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(-8f, 0f), new Vector2(0f, 18f), 14, TextAlignmentOptions.TopRight);

        var slotsGo = new GameObject("HealthSlots", typeof(RectTransform));
        slotsGo.transform.SetParent(panelGo.transform, false);
        var slotsRect = slotsGo.GetComponent<RectTransform>();
        slotsRect.anchorMin = new Vector2(0.46f, 0f);
        slotsRect.anchorMax = new Vector2(1f, 0f);
        slotsRect.pivot = new Vector2(0f, 0f);
        slotsRect.anchoredPosition = new Vector2(0f, 10f);
        slotsRect.sizeDelta = new Vector2(0f, 26f);

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
        panelRect.anchoredPosition = new Vector2(-24f, -20f);
        panelRect.sizeDelta = new Vector2(150f, 40f);

        var panelImage = panelGo.GetComponent<Image>();
        if (panelSprite != null)
        {
            panelImage.sprite = panelSprite;
        }
        panelImage.color = new Color(0.05f, 0.08f, 0.12f, 0.55f);

        var iconGo = new GameObject("CollectIcon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(panelGo.transform, false);
        var iconRect = iconGo.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(10f, 0f);
        iconRect.sizeDelta = new Vector2(26f, 26f);
        var iconImage = iconGo.GetComponent<Image>();
        iconImage.sprite = collectIconSprite;
        iconImage.color = Color.white;
        iconImage.preserveAspect = true;

        return BuildText("CollectText", panelGo.transform,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 0f), new Vector2(0f, 0f), 20, TextAlignmentOptions.Center);
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
        text.color = Color.white;
        text.alignment = alignment;
        text.raycastTarget = false;
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

        if (max != lastMaxHealth)
        {
            RebuildHealthSegments(max);
            lastMaxHealth = max;
        }

        if (current != lastHealth)
        {
            ApplyHealth(current);
            lastHealth = current;
        }

        if (healthText != null)
        {
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

        float slotWidth = 26f;
        float gap = 4f;
        float totalWidth = max * slotWidth + (max - 1) * gap;

        float startX = -totalWidth * 0.5f;

        for (int i = 0; i < max; i++)
        {
            float x = startX + i * (slotWidth + gap) + slotWidth * 0.5f;

            var slotGo = new GameObject($"Slot_{i}", typeof(RectTransform), typeof(Image));
            slotGo.transform.SetParent(healthFillMask, false);
            var slotRect = slotGo.GetComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0.5f, 0.5f);
            slotRect.anchorMax = new Vector2(0.5f, 0.5f);
            slotRect.pivot = new Vector2(0.5f, 0.5f);
            slotRect.anchoredPosition = new Vector2(x, 0f);
            slotRect.sizeDelta = new Vector2(slotWidth, slotWidth);
            var slotImage = slotGo.GetComponent<Image>();
            if (panelSprite != null)
            {
                slotImage.sprite = panelSprite;
            }
            slotImage.color = new Color(0.2f, 0.25f, 0.3f, 0.8f);

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
                ? new Color(0.62f, 0.9f, 1f, 1f)
                : new Color(0.25f, 0.3f, 0.35f, 0.55f);
        }
    }

    private void UpdateCollectible()
    {
        if (tracker == null)
        {
            return;
        }

        int count = tracker.CurrentCount;

        if (count == lastCollectCount)
        {
            return;
        }

        lastCollectCount = count;

        if (collectText != null)
        {
            // 项目无「目标总数」字段，CollectibleTracker.TotalValue 是已收集价值之和，
            // 与 CurrentCount 等价，故这里只展示已收集数量，避免假的分母。
            collectText.text = $"梦滴 {count}";
        }
    }
}
