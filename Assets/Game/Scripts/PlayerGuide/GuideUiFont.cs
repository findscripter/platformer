using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using System.Reflection;
#endif

/// <summary>目录字体只作为模板；一轮 Play 共用独立的动态字体、材质和图集。</summary>
public static class GuideUiFont
{
    public const string FontAssetGuid = "7e794ed8003821b4dac6b110719e0135";

    private static TMP_FontAsset sourceFont;
    private static TMP_FontAsset runtimeFont;
    private static bool sessionActive;
    private static bool sessionEnding;
    private static bool rebinding;
    private static bool creationFailed;
    private static GameObject persistentAnchor;

    private sealed class TextBinding
    {
        public TMP_Text text;
        public Material sourceMaterial;
        public Material sourceInstanceMaterial;
        public string originalText;
        public TMP_InputField input;
        public string originalInput;
        public bool materialApplied;
    }

    private static readonly Dictionary<TMP_Text, TextBinding> Bindings = new Dictionary<TMP_Text, TextBinding>();
    private static readonly Dictionary<Material, Material> Materials = new Dictionary<Material, Material>();
    private static readonly List<GameObject> Roots = new List<GameObject>();
    private static readonly List<TMP_Text> Texts = new List<TMP_Text>();
    private static readonly List<TMP_Text> DeadTexts = new List<TMP_Text>();
#if UNITY_EDITOR
    // TMP 的 fontSharedMaterial setter 不清理这个缓存；否则下一轮设置描边会复用旧 atlas。
    // 只在编辑器无重载会话中交换缓存，不触发 SerializedObject 的 OnValidate / 场景标脏。
    private static readonly FieldInfo InstanceMaterialField = typeof(TMP_Text).GetField("m_fontMaterial", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly HashSet<Material> TextInstanceMaterials = new HashSet<Material>();
#endif

    /// <summary>生成目录后失效模板缓存；使用中的会话字体留到 Stop 再释放。</summary>
    public static void ClearCache()
    {
        if (sessionActive || sessionEnding)
            return;
        sourceFont = null;
        creationFailed = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitializeSession()
    {
        // 关闭 Domain Reload 时，编辑器桥可能已经在 ExitingEditMode 完成绑定。
        if (!sessionActive)
            ClearCache();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void BindBeforeSceneLoad()
    {
        BeginPlaySession();
        RebindLoadedTexts();
    }

    private static TMP_FontAsset LoadSource()
    {
        if (sourceFont != null)
            return sourceFont;
        var catalog = Resources.Load<RuntimeArtCatalog>(RuntimeArtCatalog.ResourcePath);
        if (catalog != null)
            sourceFont = catalog.UiFont;
#if UNITY_EDITOR
        if (sourceFont == null)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(FontAssetGuid);
            if (!string.IsNullOrEmpty(path))
                sourceFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        }
#endif
        return sourceFont;
    }

    public static TMP_FontAsset Load()
    {
        TMP_FontAsset template = LoadSource();
        if (template == null)
            return TMP_Settings.defaultFontAsset;
        if (!sessionActive && !sessionEnding && !Application.isPlaying)
            return template;
        if (runtimeFont != null || creationFailed)
            return runtimeFont;

        if (template.sourceFontFile == null)
        {
            creationFailed = true;
            Debug.LogError("[GuideUiFont] 中文字体缺少源字体，无法创建独立运行时图集。");
            return null;
        }

        runtimeFont = TMP_FontAsset.CreateFontAsset(template.sourceFontFile,
            Mathf.Max(1, Mathf.RoundToInt(template.faceInfo.pointSize)), template.atlasPadding,
            template.atlasRenderMode, template.atlasWidth, template.atlasHeight,
            AtlasPopulationMode.Dynamic, template.isMultiAtlasTexturesEnabled);
        if (runtimeFont == null)
        {
            creationFailed = true;
            Debug.LogError("[GuideUiFont] 创建运行时中文字体失败，请检查源字体 Include Font Data。");
            return null;
        }

        runtimeFont.name = template.name + " (Runtime)";
        runtimeFont.hashCode = TMP_TextUtilities.GetHashCode(runtimeFont.name);
        runtimeFont.hideFlags = HideFlags.DontSave;
        runtimeFont.faceInfo = template.faceInfo;
        runtimeFont.normalStyle = template.normalStyle;
        runtimeFont.normalSpacingOffset = template.normalSpacingOffset;
        runtimeFont.boldStyle = template.boldStyle;
        runtimeFont.boldSpacing = template.boldSpacing;
        runtimeFont.italicStyle = template.italicStyle;
        runtimeFont.tabSize = template.tabSize;
        runtimeFont.getFontFeatures = template.getFontFeatures;
        runtimeFont.fallbackFontAssetTable = new List<TMP_FontAsset>();
        if (template.fallbackFontAssetTable != null)
            foreach (TMP_FontAsset fallback in template.fallbackFontAssetTable)
                runtimeFont.fallbackFontAssetTable.Add(fallback == template ? runtimeFont : fallback);
        for (int i = 0; i < template.fontWeightTable.Length && i < runtimeFont.fontWeightTable.Length; i++)
        {
            TMP_FontWeightPair pair = template.fontWeightTable[i];
            if (pair.regularTypeface == template) pair.regularTypeface = runtimeFont;
            if (pair.italicTypeface == template) pair.italicTypeface = runtimeFont;
            runtimeFont.fontWeightTable[i] = pair;
        }

        // 复制风格参数后重新指定私有图集，不能复用模板材质的 _MainTex。
        if (template.material != null)
        {
            runtimeFont.material.shader = template.material.shader;
            runtimeFont.material.CopyPropertiesFromMaterial(template.material);
        }
        runtimeFont.material.name = runtimeFont.name + " Material";
        runtimeFont.material.hideFlags = HideFlags.DontSave;
        runtimeFont.material.mainTexture = runtimeFont.atlasTexture;
        runtimeFont.atlasTexture.hideFlags = HideFlags.DontSave;
        runtimeFont.atlasTexture.name = runtimeFont.name + " Atlas";
        TMP_ResourceManager.AddFontAsset(runtimeFont);
        return runtimeFont;
    }

    public static void Apply(TMP_Text text)
    {
        if (text == null)
            return;
        TMP_FontAsset font = Load();
        if (font != null)
            text.font = font;
    }

    /// <summary>编辑器可在 Play 开始前调用，以便先替换不重载场景中的持久字体。</summary>
    public static void BeginPlaySession()
    {
        if (sessionEnding)
            return;
        sessionActive = true;
        _ = Load();
        Canvas.preWillRenderCanvases -= RebindLoadedTexts;
        Canvas.preWillRenderCanvases += RebindLoadedTexts;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Application.quitting -= EndPlaySession;
        Application.quitting += EndPlaySession;

        if (Application.isPlaying && persistentAnchor == null)
        {
            persistentAnchor = new GameObject("GuideUiFont Session") { hideFlags = HideFlags.HideInHierarchy };
            Object.DontDestroyOnLoad(persistentAnchor);
        }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RebindLoadedTexts();
    }

    /// <summary>在 TMP 的 willRenderCanvases 排版前处理新建/重新启用的文字，无每帧数组分配。</summary>
    public static void RebindLoadedTexts()
    {
        if (!sessionActive || sessionEnding || rebinding || LoadSource() == null || Load() == null)
            return;
        rebinding = true;
        try
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
                RebindScene(SceneManager.GetSceneAt(i));
            if (persistentAnchor != null)
                RebindScene(persistentAnchor.scene);

            DeadTexts.Clear();
            foreach (TMP_Text text in Bindings.Keys)
                if (text == null) DeadTexts.Add(text);
            foreach (TMP_Text text in DeadTexts)
                Bindings.Remove(text);
        }
        finally
        {
            rebinding = false;
        }
    }

    private static void RebindScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;
        Roots.Clear();
        scene.GetRootGameObjects(Roots);
        foreach (GameObject root in Roots)
        {
            if (root == null)
                continue;
            Texts.Clear();
            root.GetComponentsInChildren(true, Texts);
            foreach (TMP_Text text in Texts)
            {
                // 严格按对象身份匹配；menu_font 和其他字体不参与替换或恢复。
                if (text == null || (text.font != sourceFont && text.font != runtimeFont))
                    continue;
                bool firstBinding = !Bindings.TryGetValue(text, out TextBinding binding);
                if (firstBinding)
                {
                    var input = text.GetComponentInParent<TMP_InputField>(true);
                    binding = new TextBinding
                    {
                        text = text,
                        sourceMaterial = text.font == sourceFont ? text.fontSharedMaterial : sourceFont.material,
                        originalText = text.text,
                        input = input != null && input.textComponent == text ? input : null,
                        originalInput = input != null ? input.text : null,
                        materialApplied = text.font == runtimeFont && UsesRuntimeAtlas(text.fontSharedMaterial)
                    };
                    Bindings.Add(text, binding);
                }

                if (text.font == sourceFont)
                {
                    text.font = runtimeFont;
#if UNITY_EDITOR
                    Material previous = ExchangeInstanceMaterial(text, null);
                    if (UsesRuntimeAtlas(previous))
                        TextInstanceMaterials.Add(previous);
                    else if (firstBinding)
                        binding.sourceInstanceMaterial = previous;
#endif
                    binding.materialApplied = false;
                }
                // fontSharedMaterial 可用于隐藏文字，不使用依赖 Awake 的 outlineWidth 等 setter。
                if (!binding.materialApplied && binding.sourceMaterial != null)
                {
                    Material material = GetRuntimeMaterial(binding.sourceMaterial);
                    if (text.fontSharedMaterial != material)
                        text.fontSharedMaterial = material;
                    binding.materialApplied = true;
                }
            }
        }
    }

    private static bool UsesRuntimeAtlas(Material material)
    {
        if (material == null || runtimeFont == null)
            return false;
        foreach (Texture2D atlas in runtimeFont.atlasTextures)
            if (atlas != null && material.mainTexture == atlas)
                return true;
        return false;
    }

    private static Material GetRuntimeMaterial(Material original)
    {
        if (original == sourceFont.material)
            return runtimeFont.material;
        if (!Materials.TryGetValue(original, out Material material) || material == null)
        {
            material = new Material(original)
            {
                name = original.name + " (Runtime)",
                hideFlags = HideFlags.DontSave,
                mainTexture = runtimeFont.atlasTexture
            };
            Materials[original] = material;
        }
        return material;
    }

    /// <summary>Stop 开始后保留字体直到场景对象退出，避免 OnDisable 时引用失效。</summary>
    public static void SuspendPlaySession()
    {
        sessionEnding = true;
        Unsubscribe();
    }

    public static void EndPlaySession()
    {
        EndPlaySession(true);
    }

    /// <summary>热重载时只恢复字体，不重置正在游玩的文字和输入。</summary>
    public static void EndPlaySession(bool restoreText)
    {
        SuspendPlaySession();
        foreach (TextBinding binding in Bindings.Values)
        {
            TMP_Text text = binding.text;
            if (text == null || sourceFont == null || (text.font != runtimeFont && text.font != sourceFont))
                continue;
#if UNITY_EDITOR
            Material previous = ExchangeInstanceMaterial(text, binding.sourceInstanceMaterial);
            if (UsesRuntimeAtlas(previous))
                TextInstanceMaterials.Add(previous);
#endif
            // Scene Reload 关闭时，输入值也恢复，避免编辑器绘制上轮的新汉字到源图集。
            if (restoreText && binding.input != null)
                binding.input.SetTextWithoutNotify(binding.originalInput);
            if (restoreText)
                text.text = binding.originalText;
            text.font = sourceFont;
            if (binding.sourceMaterial != null)
                text.fontSharedMaterial = binding.sourceMaterial;
        }
        Bindings.Clear();
#if UNITY_EDITOR
        foreach (Material material in TextInstanceMaterials)
            DestroyOwned(material);
        TextInstanceMaterials.Clear();
#endif
        foreach (Material material in Materials.Values)
            DestroyOwned(material);
        Materials.Clear();

        if (runtimeFont != null)
        {
            TMP_ResourceManager.RemoveFontAsset(runtimeFont);
            // TMP 的移除按 family/style 删索引，重新登记源字体，避免影响其编辑器预览。
            if (sourceFont != null)
            {
                TMP_ResourceManager.RemoveFontAsset(sourceFont);
                TMP_ResourceManager.AddFontAsset(sourceFont);
            }
            // 本版本 TMP_FontAsset.OnDestroy 自行释放其所有 atlas 和默认材质。
            DestroyOwned(runtimeFont);
        }
        DestroyOwned(persistentAnchor);
        persistentAnchor = null;
        runtimeFont = null;
        sourceFont = null;
        sessionActive = false;
        sessionEnding = false;
        creationFailed = false;
        Roots.Clear();
        Texts.Clear();
        DeadTexts.Clear();
    }

    private static void Unsubscribe()
    {
        Canvas.preWillRenderCanvases -= RebindLoadedTexts;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Application.quitting -= EndPlaySession;
    }

#if UNITY_EDITOR
    private static Material ExchangeInstanceMaterial(TMP_Text text, Material replacement)
    {
        if (InstanceMaterialField == null)
            return null;
        var previous = InstanceMaterialField.GetValue(text) as Material;
        InstanceMaterialField.SetValue(text, replacement);
        return previous;
    }
#endif

    private static void DestroyOwned(Object value)
    {
        if (value == null)
            return;
#if UNITY_EDITOR
        Object.DestroyImmediate(value);
#else
        Object.Destroy(value);
#endif
    }
}
