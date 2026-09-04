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

    private Image image;
    private Sprite[] frames;
    private int poseFrom;
    private int poseTo;
    private float fps = 10f;
    private float elapsed;
    private int frameIndex;
    private bool walking;

    private void Awake()
    {
        EnsureImage();
        LoadFrames();
        SetPose(0, 26, 10f);
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

    /// <summary>第一次相遇：居中偏上，给底部对白条留空。</summary>
    public void SetMeetingLayout()
    {
        SetActive(true);
        ApplyAnchored(new Vector2(0.52f, 0.58f), 240f);
    }

    /// <summary>写梦陪伴位：问话框左下外侧，不压输入区。</summary>
    public void SetCompanionLayout()
    {
        SetActive(true);
        ApplyAnchored(new Vector2(0.12f, 0.22f), 150f);
    }

    private void ApplyAnchored(Vector2 anchor, float size)
    {
        var rect = feifeiCharacter != null ? feifeiCharacter.transform as RectTransform : null;
        if (rect == null)
            return;

        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(size, size);
    }

    private void ApplySize(float size)
    {
        var rect = feifeiCharacter != null ? feifeiCharacter.transform as RectTransform : null;
        if (rect != null)
            rect.sizeDelta = new Vector2(size, size);
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
        if (string.IsNullOrWhiteSpace(stateName))
            return;

        switch (stateName)
        {
            case "HoldCore":
                SetPose(9, 16, 8f);
                break;
            case "LookDown":
                SetPose(0, 8, 8f);
                break;
            case "ThinkPose":
            case "LookAtPlayer":
            case "Smile":
                SetPose(17, 24, 9f);
                break;
            case "Sad":
                SetPose(0, 7, 6f);
                break;
            default:
                SetPose(0, 26, 10f);
                break;
        }
    }

    public IEnumerator PlayTailSweep(float duration)
    {
        if (feifeiCharacter == null)
            yield break;

        SetPose(19, 26, 12f);
        Transform target = feifeiCharacter.transform;
        Quaternion origin = target.localRotation;
        float elapsedSweep = 0f;
        float safeDuration = duration > 0f ? duration : 1f;
        while (elapsedSweep < safeDuration)
        {
            float t = elapsedSweep / safeDuration;
            float swing = Mathf.Sin(t * Mathf.PI * 2f) * 8f;
            target.localRotation = origin * Quaternion.Euler(0f, 0f, swing);
            elapsedSweep += Time.deltaTime;
            yield return null;
        }

        target.localRotation = origin;
        SetPose(9, 16, 8f);
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
