using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 腓腓是 UI Image。待机序列 27 帧里带捧爪/抬头/扫尾姿势，
/// 用切帧代替缺失的走/捧 Animator 状态。
/// </summary>
public class FeifeiCharacterView : MonoBehaviour
{
    [SerializeField] private GameObject feifeiCharacter;
    [SerializeField] private Animator feifeiAnimator;
    [Tooltip("腓腓待机图中两爪位置，原点左下；按实际 Sprite 绘制范围缩放。")]
    [SerializeField] private Vector2 coreHoldNormalized = new Vector2(0.14f, 0.36f);

    private Image image;
    private Sprite[] frames;
    private int poseFrom;
    private int poseTo;
    private float fps = 10f;
    private float elapsed;
    private int frameIndex;
    private bool walking;
    private RectTransform coreHoldAnchor;
    private float poseTiltZ;
    private Vector2 restAnchored;
    private Vector2 offerShift;

    public RectTransform CoreHoldAnchor
    {
        get
        {
            EnsureImage();
            UpdateCoreHoldAnchor();
            return coreHoldAnchor;
        }
    }

    private void Awake()
    {
        EnsureImage();
        if (feifeiAnimator != null)
            feifeiAnimator.enabled = false;
        LoadFrames();
        SetPose(0, 26, 10f);
    }

    private void LateUpdate()
    {
        if (coreHoldAnchor != null)
            UpdateCoreHoldAnchor();
    }

    private void UpdateCoreHoldAnchor()
    {
        var rect = feifeiCharacter != null ? feifeiCharacter.transform as RectTransform : null;
        if (rect == null)
            return;
        if (coreHoldAnchor == null)
        {
            var anchor = new GameObject("DreamCoreHold", typeof(RectTransform));
            anchor.transform.SetParent(rect, false);
            coreHoldAnchor = anchor.GetComponent<RectTransform>();
            coreHoldAnchor.anchorMin = coreHoldAnchor.anchorMax = new Vector2(0.5f, 0.5f);
            coreHoldAnchor.pivot = new Vector2(0.5f, 0.5f);
        }

        Vector2 drawnSize = rect.rect.size;
        if (image != null && image.sprite != null && image.preserveAspect)
        {
            Vector2 spriteSize = image.sprite.rect.size;
            float fit = Mathf.Min(drawnSize.x / spriteSize.x, drawnSize.y / spriteSize.y);
            drawnSize = spriteSize * fit;
        }
        coreHoldAnchor.sizeDelta = drawnSize;
        coreHoldAnchor.anchoredPosition = Vector2.Scale(coreHoldNormalized - new Vector2(0.5f, 0.5f), drawnSize);
    }

    /// <summary>走到双爪能接住目标的位置，避免捧起时梦核横向跳到身体另一侧。</summary>
    public Vector3 GetPickupPosition(Transform core)
    {
        RectTransform anchor = CoreHoldAnchor;
        if (feifeiCharacter == null || anchor == null || core == null)
            return Vector3.zero;
        Transform character = feifeiCharacter.transform;
        Vector3 delta = core.position - anchor.position;
        if (character.parent != null)
            delta = character.parent.InverseTransformVector(delta);
        return character.localPosition + delta;
    }

    private void Update()
    {
        if (image == null || frames == null || frames.Length == 0)
            return;

        int from = Mathf.Clamp(poseFrom, 0, frames.Length - 1);
        int to = Mathf.Clamp(poseTo, from, frames.Length - 1);
        int count = to - from + 1;
        if (count <= 0)
            return;

        elapsed += Time.deltaTime * fps;
        int offset = ((int)elapsed) % count;
        frameIndex = from + offset;
        if (frames[frameIndex] != null)
            image.sprite = frames[frameIndex];
    }

    public void SetActive(bool active)
    {
        if (feifeiCharacter != null)
            feifeiCharacter.SetActive(active);

        if (active)
        {
            EnsureImage();
            ApplySize(240f);
            if (feifeiAnimator != null)
                feifeiAnimator.enabled = false;
            SetPose(0, 26, 10f);
        }
    }

    public void SetLocalPosition(Vector3 position)
    {
        if (feifeiCharacter != null)
            feifeiCharacter.transform.localPosition = position;
    }

    /// <summary>第一次相遇：中偏右、对白条上方，和左侧玩家对看。</summary>
    public void SetMeetingLayout()
    {
        SetActive(true);
        ApplyAnchored(new Vector2(0.58f, 0.54f), 300f);
    }

    /// <summary>写梦陪伴位：问话框左下外侧，不压按钮。</summary>
    public void SetCompanionLayout()
    {
        SetActive(true);
        ApplyAnchored(new Vector2(0.07f, 0.105f), 180f);
    }

    private void ApplyAnchored(Vector2 anchor, float size)
    {
        var rect = feifeiCharacter != null ? feifeiCharacter.transform as RectTransform : null;
        if (rect == null)
            return;

        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        restAnchored = Vector2.zero;
        offerShift = Vector2.zero;
        poseTiltZ = 0f;
        rect.anchoredPosition = restAnchored;
        rect.sizeDelta = new Vector2(size, size);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private void ApplySize(float size)
    {
        var rect = feifeiCharacter != null ? feifeiCharacter.transform as RectTransform : null;
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(size, size);
            rect.localScale = Vector3.one;
        }
    }

    public IEnumerator MoveLocalPosition(Vector3 from, Vector3 to, float duration)
    {
        if (feifeiCharacter == null)
            yield break;

        walking = true;
        SetPose(0, 26, 14f);
        float elapsedMove = 0f;
        while (elapsedMove < duration)
        {
            float t = EaseOutCubic(elapsedMove / duration);
            Vector3 pos = Vector3.Lerp(from, to, t);
            pos.y += Mathf.Sin(elapsedMove * 10f) * 8f;
            feifeiCharacter.transform.localPosition = pos;
            elapsedMove += Time.deltaTime;
            yield return null;
        }

        feifeiCharacter.transform.localPosition = to;
        walking = false;
        SetPose(0, 26, 10f);
    }

    public void SetAnimatorSpeed(float speed)
    {
        fps = Mathf.Lerp(8f, 14f, Mathf.Clamp01(speed));
    }

    public void SafeSetBool(string paramName, bool value)
    {
        if (paramName == "IsWalking")
        {
            walking = value;
            SetPose(0, 26, value ? 14f : 10f);
        }
    }

    public void SafeSetTrigger(string paramName)
    {
        if (paramName == "PickUpCore")
            SetPose(9, 16, 8f);
    }

    public void SafePlayState(string stateName)
    {
        PlayMeetingBeat(stateName);
    }

    /// <summary>
    /// Frame 6 八拍：待机图几乎同一姿势，用倾角、递出位移和切帧区间读出看核/看玩家/轻抚。
    /// </summary>
    public void PlayMeetingBeat(string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName))
            return;

        switch (stateName)
        {
            case "CatchCore":
            case "HoldCore":
                SetPose(0, 7, 7f);
                SetBeatTransform(8f, Vector2.zero);
                break;
            case "LookDown":
                SetPose(0, 4, 5f);
                SetBeatTransform(14f, Vector2.zero);
                break;
            case "TouchCore":
                SetPose(16, 18, 6f);
                SetBeatTransform(10f, Vector2.zero);
                break;
            case "LookAtPlayer":
            case "ThinkPose":
            case "Smile":
                SetPose(19, 23, 7f);
                SetBeatTransform(-8f, new Vector2(-10f, 6f));
                break;
            case "QuietHold":
                SetPose(8, 15, 5f);
                SetBeatTransform(6f, Vector2.zero);
                break;
            case "OfferCore":
                SetPose(16, 20, 6f);
                SetBeatTransform(-4f, new Vector2(-72f, 16f));
                break;
            case "Sad":
                SetPose(0, 7, 6f);
                SetBeatTransform(10f, Vector2.zero);
                break;
            default:
                SetPose(0, 26, 10f);
                SetBeatTransform(0f, Vector2.zero);
                break;
        }
    }

    public IEnumerator PlayCatchBob(float duration)
    {
        var rect = feifeiCharacter != null ? feifeiCharacter.transform as RectTransform : null;
        if (rect == null)
            yield break;

        Vector2 origin = restAnchored + offerShift;
        Vector2 dip = origin + new Vector2(0f, -16f);
        float safe = duration > 0f ? duration : 0.35f;
        float elapsedBob = 0f;
        while (elapsedBob < safe)
        {
            float t = elapsedBob / safe;
            float wave = Mathf.Sin(t * Mathf.PI);
            rect.anchoredPosition = Vector2.Lerp(origin, dip, wave);
            elapsedBob += Time.deltaTime;
            yield return null;
        }

        rect.anchoredPosition = origin;
    }

    public IEnumerator PlayQuietTail(float duration)
    {
        var rect = feifeiCharacter != null ? feifeiCharacter.transform as RectTransform : null;
        if (rect == null)
            yield break;

        SetPose(8, 15, 5f);
        float safeDuration = duration > 0f ? duration : 1f;
        float elapsedSweep = 0f;
        while (elapsedSweep < safeDuration)
        {
            float t = elapsedSweep / safeDuration;
            float swing = Mathf.Sin(t * Mathf.PI) * 6f;
            rect.localRotation = Quaternion.Euler(0f, 0f, poseTiltZ + swing);
            elapsedSweep += Time.deltaTime;
            yield return null;
        }

        ApplyBeatTransform();
    }

    public IEnumerator PlayTailSweep(float duration)
    {
        if (feifeiCharacter == null)
            yield break;

        SetPose(19, 26, 12f);
        Transform target = feifeiCharacter.transform;
        float elapsedSweep = 0f;
        float safeDuration = duration > 0f ? duration : 1f;
        while (elapsedSweep < safeDuration)
        {
            float t = elapsedSweep / safeDuration;
            float swing = Mathf.Sin(t * Mathf.PI * 2f) * 8f;
            target.localRotation = Quaternion.Euler(0f, 0f, poseTiltZ + swing);
            elapsedSweep += Time.deltaTime;
            yield return null;
        }

        ApplyBeatTransform();
        SetPose(9, 16, 8f);
    }

    private void SetBeatTransform(float tiltZ, Vector2 shift)
    {
        poseTiltZ = tiltZ;
        offerShift = shift;
        ApplyBeatTransform();
    }

    private void ApplyBeatTransform()
    {
        var rect = feifeiCharacter != null ? feifeiCharacter.transform as RectTransform : null;
        if (rect == null)
            return;
        rect.localRotation = Quaternion.Euler(0f, 0f, poseTiltZ);
        rect.anchoredPosition = restAnchored + offerShift;
    }

    public IEnumerator FadeAlpha(float from, float to, float duration)
    {
        if (feifeiCharacter == null)
            yield break;

        var group = feifeiCharacter.GetComponent<CanvasGroup>();
        if (group == null)
            group = feifeiCharacter.AddComponent<CanvasGroup>();

        group.alpha = from;
        if (duration <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        float fadeElapsed = 0f;
        while (fadeElapsed < duration)
        {
            group.alpha = Mathf.Lerp(from, to, fadeElapsed / duration);
            fadeElapsed += Time.deltaTime;
            yield return null;
        }

        group.alpha = to;
    }

    private void SetPose(int from, int to, float poseFps)
    {
        poseFrom = from;
        poseTo = to;
        fps = poseFps;
        elapsed = 0f;
        if (image != null && frames != null && from >= 0 && from < frames.Length && frames[from] != null)
            image.sprite = frames[from];
    }

    private void EnsureImage()
    {
        if (feifeiCharacter == null)
            return;

        image = feifeiCharacter.GetComponent<Image>();
        if (image != null)
        {
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.type = Image.Type.Simple;
            image.overrideSprite = null;
            image.color = Color.white;
        }
    }

    private void LoadFrames()
    {
        frames = GuideArt.LoadFeifeiFrames();
        if (image != null && frames != null && frames.Length > 0 && frames[0] != null)
            image.sprite = frames[0];
    }

    private static float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }
}
