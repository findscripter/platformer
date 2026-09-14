using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 开场场景雾：铺在画布背景层上的氛围雾，不是单独的 UI 切图块。
/// 点击后整层散开。
/// </summary>
public static class GuideFx
{
    public const string SceneMistRootName = "SceneMist";

    public static Sprite Mist => GuideArt.LoadPath("Assets/Game/Art/Effects/Mist FX.png");
    public static Sprite Mist2 => GuideArt.LoadPath("Assets/Game/Art/Effects/Mist FX2.png");
    public static Sprite SceneMist => GuideArt.LoadPath("Assets/Game/Art/source/FX-Mist.png");
    public static Sprite BgFx => LargestSprite("Assets/Game/Art/source/BG01FX.png");

    public static Transform EnsureSceneMist(Transform canvasRoot, int siblingIndex)
    {
        if (canvasRoot == null)
            return null;

        DestroyNamed(canvasRoot, SceneMistRootName);
        Transform background = canvasRoot.Find("BackgroundCanvasGroup");
        if (background != null)
            DestroyNamed(background, SceneMistRootName);

        var root = new GameObject(SceneMistRootName, typeof(RectTransform), typeof(CanvasGroup));
        root.transform.SetParent(canvasRoot, false);
        int index = Mathf.Clamp(siblingIndex, 0, canvasRoot.childCount - 1);
        root.transform.SetSiblingIndex(index);
        GuideUiLayout.Stretch(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

        var group = root.GetComponent<CanvasGroup>();
        group.alpha = 1f;
        group.blocksRaycasts = false;
        group.interactable = false;
        group.ignoreParentGroups = true;

        Sprite veil = BgFx != null ? BgFx : Mist;
        // 用背景雾贴图铺氛围，不用 Mist 切图拉成椭圆卡片。
        AddCover(root.transform, "Veil", veil, 0.08f, 0.08f, 0.1f);
        AddBank(root.transform, "Low", veil, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.15f),
            new Vector2(2400f, 1100f), new Vector2(0f, -30f), 0.22f, 0f, false, 0.1f, 0.04f);
        AddBank(root.transform, "Drift", veil, new Vector2(0.62f, 0.18f), new Vector2(0.5f, 0.4f),
            new Vector2(2100f, 980f), new Vector2(60f, 10f), 0.1f, 4f, true, 0.16f, 0.12f);
        return root.transform;
    }

    public static IEnumerator Drift(Transform mist, float speed)
    {
        if (mist == null)
            yield break;

        var rect = mist as RectTransform;
        Vector2 origin = rect != null ? rect.anchoredPosition : Vector2.zero;
        float baseZ = mist.localEulerAngles.z;
        Vector3 baseScale = mist.localScale;
        float phase = Random.Range(0f, Mathf.PI * 2f);

        while (mist != null)
        {
            phase += Time.deltaTime * speed;
            if (rect != null)
                rect.anchoredPosition = origin + new Vector2(Mathf.Sin(phase) * 28f, Mathf.Cos(phase * 0.6f) * 10f);
            mist.localRotation = Quaternion.Euler(0f, 0f, baseZ + Mathf.Sin(phase * 0.4f) * 1.2f);
            mist.localScale = baseScale * (1f + Mathf.Sin(phase * 0.55f) * 0.025f);
            yield return null;
        }
    }

    public static IEnumerator DissipateScene(Transform canvasRoot, float duration)
    {
        if (canvasRoot == null)
            yield break;

        Transform mist = canvasRoot.Find(SceneMistRootName);
        if (mist == null)
            yield break;

        var group = mist.GetComponent<CanvasGroup>();
        float start = group != null ? group.alpha : 1f;
        Vector3 from = mist.localScale;
        Vector3 to = from * 1.18f;
        duration = Mathf.Max(0.05f, duration);
        float elapsed = 0f;
        while (elapsed < duration && mist != null)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            mist.localScale = Vector3.Lerp(from, to, t);
            if (group != null)
                group.alpha = Mathf.Lerp(start, 0f, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (mist != null)
            mist.gameObject.SetActive(false);
    }

    private static void AddCover(Transform parent, string name, Sprite sprite, float alpha, float fadeX, float fadeY)
    {
        Image image = CreateLayer(parent, name, sprite, fadeX, fadeY);
        if (image == null)
            return;

        GuideUiLayout.Stretch(image.rectTransform, Vector2.zero, Vector2.one);
        image.preserveAspect = false;
        image.color = new Color(0.78f, 0.82f, 0.9f, alpha);
    }

    private static void AddBank(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 pivot,
        Vector2 size, Vector2 position, float alpha, float z, bool flip, float fadeX, float fadeY)
    {
        Image image = CreateLayer(parent, name, sprite, fadeX, fadeY);
        if (image == null)
            return;

        var rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localScale = new Vector3(flip ? -1f : 1f, 1f, 1f);
        rect.localRotation = Quaternion.Euler(0f, 0f, z);
        image.preserveAspect = false;
        image.color = new Color(0.8f, 0.84f, 0.9f, alpha);
    }

    private static Image CreateLayer(Transform parent, string name, Sprite sprite, float fadeX, float fadeY)
    {
        if (parent == null || sprite == null)
            return null;

        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);

        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.useSpriteMesh = false;
        image.raycastTarget = false;
        image.maskable = false;
        image.material = MistMaterial(fadeX, fadeY);
        var canvasGroup = go.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = go.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        return image;
    }

    private static Material MistMaterial(float fadeX, float fadeY)
    {
        Material source = Resources.Load<Material>("GuideMistSoft");
        Shader shader = source != null ? source.shader : Shader.Find("Game/UI/MistSoft");
        if (shader == null)
            return source;

        var material = new Material(shader);
        material.SetFloat("_EdgeFadeX", fadeX);
        material.SetFloat("_EdgeFadeY", fadeY);
        return material;
    }

    private static Sprite LargestSprite(string assetPath)
    {
        Sprite loaded = GuideArt.LoadPath(assetPath);
#if UNITY_EDITOR
        Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
        Sprite best = loaded;
        float bestArea = loaded != null ? loaded.rect.width * loaded.rect.height : 0f;
        for (int i = 0; i < assets.Length; i++)
        {
            var sprite = assets[i] as Sprite;
            if (sprite == null)
                continue;
            float area = sprite.rect.width * sprite.rect.height;
            if (area <= bestArea)
                continue;
            best = sprite;
            bestArea = area;
        }
        if (best != null)
            return best;
#endif
        return loaded;
    }

    private static void DestroyNamed(Transform parent, string name)
    {
        if (parent == null)
            return;
        Transform existing = parent.Find(name);
        if (existing == null)
            return;
        existing.name = "_old_" + name;
        Object.Destroy(existing.gameObject);
    }
}
