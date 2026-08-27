using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 对话 UI 美术资源接入层 — 为 DialogueUIBuilder 生成的 UI 替换美术素材并修复中文字体。
///
/// 接入清单：
/// - Dialogue.png (1675×340 金角饰横幅) → DialoguePanel 背景（Sliced 9-patch）
/// - Dialogues/3.png (白色发光圆点) → HintIcon 点击提示符
/// - dialogue_font.otf (legacy Font) → 所有 UI.Text 组件
/// - dialogue_font_TMP.asset (可选 TMP 升级) → 优先使用 TMP 时的字体
///
/// 调用时机：DialogueUIBuilder.Build() 之后、DialogueUIController.Bind() 之前。
/// </summary>
public static class DialogueUIArtIntegration
{
    private const string DialogueBannerGuid = "2033fe5a018fd9b45a37ec8c64c15755";  // Dialogue.png
    private const string HintDotGuid = "41e0bc7119da43045b92e5f75de4d944";         // Dialogues/3.png
    private const string LegacyFontGuid = "aa708105bbd00994b8d6fe124993541f";      // dialogue_font.otf
    private const string TmpFontGuid = "7e794ed8003821b4dac6b110719e0135";         // dialogue_font_TMP.asset

    private static Font cachedLegacyFont;
    private static TMP_FontAsset cachedTmpFont;

    /// <summary>
    /// 为已生成的对话面板接入美术资源并修复中文字体。
    /// </summary>
    /// <param name="panelRoot">DialogueUIBuilder 返回的 PanelRoot</param>
    public static void ApplyArt(GameObject panelRoot)
    {
        if (panelRoot == null)
        {
            Debug.LogWarning("[DialogueUIArtIntegration] panelRoot is null, skipping art integration.");
            return;
        }

        CacheFonts();
        ApplyDialogueBannerBackground(panelRoot);
        ApplyHintDotIcon(panelRoot);
        FixAllFonts(panelRoot);
    }

    private static void CacheFonts()
    {
        if (cachedLegacyFont == null)
        {
            cachedLegacyFont = LoadAssetByGuid<Font>(LegacyFontGuid, "Assets/Game/Art/source/dialogue_font.otf");
            if (cachedLegacyFont == null)
            {
                Debug.LogWarning("[DialogueUIArtIntegration] Failed to load dialogue_font.otf, Chinese text will fail to render.");
            }
        }

        if (cachedTmpFont == null)
        {
            cachedTmpFont = LoadAssetByGuid<TMP_FontAsset>(TmpFontGuid, "Assets/Game/Art/source/dialogue_font_TMP.asset");
        }
    }

    private static void FixAllFonts(GameObject root)
    {
        if (cachedLegacyFont == null)
        {
            return;
        }

        var allTexts = root.GetComponentsInChildren<Text>(true);
        foreach (var t in allTexts)
        {
            t.font = cachedLegacyFont;
        }

        if (cachedTmpFont != null)
        {
            var allTmpTexts = root.GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in allTmpTexts)
            {
                t.font = cachedTmpFont;
            }
        }
    }

    private static void ApplyDialogueBannerBackground(GameObject panelRoot)
    {
        Image panelImage = panelRoot.GetComponent<Image>();
        if (panelImage == null)
        {
            Debug.LogWarning("[DialogueUIArtIntegration] DialoguePanel has no Image component.");
            return;
        }

        Sprite banner = LoadSpriteByGuid(DialogueBannerGuid, "Assets/Game/Art/source/Dialogue.png");
        if (banner == null)
        {
            Debug.LogWarning("[DialogueUIArtIntegration] Failed to load Dialogue.png, keeping default background.");
            return;
        }

        panelImage.sprite = banner;
        panelImage.type = Image.Type.Sliced;
        panelImage.color = Color.white;
    }

    private static void ApplyHintDotIcon(GameObject panelRoot)
    {
        Transform hintTextTransform = panelRoot.transform.Find("HintText");
        if (hintTextTransform == null)
        {
            return;
        }

        Transform existingIcon = hintTextTransform.Find("HintDot");
        if (existingIcon != null)
        {
            return;
        }

        Sprite dotSprite = LoadSpriteByGuid(HintDotGuid, "Assets/Game/Art/UI/Dialogues/3.png");
        if (dotSprite == null)
        {
            return;
        }

        var iconGo = new GameObject("HintDot", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(hintTextTransform, false);

        RectTransform iconRect = iconGo.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(1f, 0.5f);
        iconRect.anchoredPosition = new Vector2(-8f, 0f);
        iconRect.sizeDelta = new Vector2(12f, 12f);

        Image iconImage = iconGo.GetComponent<Image>();
        iconImage.sprite = dotSprite;
        iconImage.color = Color.white;
        iconImage.raycastTarget = false;

        StartPulseAnimation(iconImage);
    }

    private static void StartPulseAnimation(Image icon)
    {
        if (icon == null)
        {
            return;
        }

        var animator = icon.gameObject.AddComponent<DialogueHintDotAnimator>();
        animator.StartPulse();
    }

    private static T LoadAssetByGuid<T>(string guid, string fallbackPath) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(guid))
        {
            return LoadAssetByPath<T>(fallbackPath);
        }

#if UNITY_EDITOR
        var loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(
            UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
        if (loaded != null)
        {
            return loaded;
        }
#endif
        return LoadAssetByPath<T>(fallbackPath);
    }

    private static T LoadAssetByPath<T>(string path) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
#else
        string resourcePath = path.Replace("Assets/Resources/", "").Replace(System.IO.Path.GetExtension(path), "");
        return Resources.Load<T>(resourcePath);
#endif
    }

    private static Sprite LoadSpriteByGuid(string guid, string fallbackPath)
    {
        return LoadAssetByGuid<Sprite>(guid, fallbackPath);
    }

    private static Sprite LoadSpriteByPath(string path)
    {
        return LoadAssetByPath<Sprite>(path);
    }
}

/// <summary>
/// 简单的脉冲动画器 — 让 hint 提示点缓慢呼吸闪烁。
/// </summary>
public class DialogueHintDotAnimator : MonoBehaviour
{
    private Image targetImage;
    private float time;
    private bool isAnimating;

    public void StartPulse()
    {
        targetImage = GetComponent<Image>();
        if (targetImage == null)
        {
            return;
        }

        isAnimating = true;
        time = 0f;
    }

    public void StopPulse()
    {
        isAnimating = false;
        if (targetImage != null)
        {
            targetImage.color = Color.white;
        }
    }

    private void Update()
    {
        if (!isAnimating || targetImage == null)
        {
            return;
        }

        time += Time.deltaTime;
        float alpha = 0.5f + 0.5f * Mathf.Sin(time * 2f);
        Color c = targetImage.color;
        c.a = alpha;
        targetImage.color = c;
    }

    private void OnDestroy()
    {
        StopPulse();
    }
}
