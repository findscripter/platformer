using UnityEngine;
using UnityEngine.UI;

public class PlayerGuideMainBubbleVisual : MonoBehaviour
{
    public const string VisualChildName = "bubble_visual";

    [SerializeField] private RectTransform scaleTarget;

    public RectTransform ScaleTarget
    {
        get
        {
            if (scaleTarget != null)
                return scaleTarget;

            Transform visual = transform.Find(VisualChildName);
            if (visual != null)
                scaleTarget = visual as RectTransform;

            return scaleTarget != null ? scaleTarget : transform as RectTransform;
        }
    }

    private void Awake()
    {
        EnsureStructure(gameObject);
        _ = ScaleTarget;
    }

    public static PlayerGuideMainBubbleVisual EnsureStructure(GameObject mainBubbleRoot)
    {
        if (mainBubbleRoot == null)
            return null;

        PlayerGuideMainBubbleVisual visual = mainBubbleRoot.GetComponent<PlayerGuideMainBubbleVisual>();
        if (visual == null)
            visual = mainBubbleRoot.AddComponent<PlayerGuideMainBubbleVisual>();

        if (mainBubbleRoot.transform.Find(VisualChildName) != null)
            return visual;

        GameObject visualObject = new GameObject(VisualChildName, typeof(RectTransform));
        visualObject.transform.SetParent(mainBubbleRoot.transform, false);
        StretchRect(visualObject.GetComponent<RectTransform>());

        Image rootImage = mainBubbleRoot.GetComponent<Image>();
        if (rootImage != null)
        {
            Image visualImage = visualObject.AddComponent<Image>();
            visualImage.sprite = rootImage.sprite;
            visualImage.color = rootImage.color;
            visualImage.material = rootImage.material;
            visualImage.raycastTarget = rootImage.raycastTarget;
            visualImage.maskable = rootImage.maskable;
            visualImage.type = rootImage.type;
            visualImage.preserveAspect = rootImage.preserveAspect;

            #if UNITY_EDITOR
            DestroyImmediate(rootImage);
            #else
            Destroy(rootImage);
            #endif
        }

        var childrenToMove = new Transform[mainBubbleRoot.transform.childCount];
        for (int i = 0; i < childrenToMove.Length; i++)
            childrenToMove[i] = mainBubbleRoot.transform.GetChild(i);

        foreach (Transform child in childrenToMove)
        {
            if (child.name == VisualChildName)
                continue;

            child.SetParent(visualObject.transform, true);
        }

        Button button = mainBubbleRoot.GetComponent<Button>();
        Image targetImage = visualObject.GetComponent<Image>();
        if (button != null && targetImage != null)
            button.targetGraphic = targetImage;

        visual.scaleTarget = visualObject.GetComponent<RectTransform>();

        RectTransform rootRect = mainBubbleRoot.GetComponent<RectTransform>();
        if (rootRect != null)
            rootRect.localScale = Vector3.one;

        return visual;
    }

    private static void StretchRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.anchoredPosition = Vector2.zero;
    }
}
