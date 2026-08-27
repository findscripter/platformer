using System.Collections;
using UnityEngine;

/// <summary>
/// 腓腓角色视图。
/// 忠实复刻 PlayerGuideFlowControllerV2 中 feifeiCharacter / feifeiAnimator 的移动与动画控制逻辑。
/// </summary>
public class FeifeiCharacterView : MonoBehaviour
{
    [SerializeField] private GameObject feifeiCharacter;
    [SerializeField] private Animator feifeiAnimator;

    public void SetActive(bool active)
    {
        if (feifeiCharacter != null)
        {
            feifeiCharacter.SetActive(active);
        }
    }

    public void SetLocalPosition(Vector3 position)
    {
        if (feifeiCharacter != null)
        {
            feifeiCharacter.transform.localPosition = position;
        }
    }

    public IEnumerator MoveLocalPosition(Vector3 from, Vector3 to, float duration)
    {
        if (feifeiCharacter == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = EaseOutCubic(t);
            feifeiCharacter.transform.localPosition = Vector3.Lerp(from, to, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        feifeiCharacter.transform.localPosition = to;
    }

    public void SetAnimatorSpeed(float speed)
    {
        if (feifeiAnimator != null)
        {
            feifeiAnimator.speed = speed;
        }
    }

    public void SafeSetBool(string paramName, bool value)
    {
        if (feifeiAnimator == null) return;

        foreach (var param in feifeiAnimator.parameters)
        {
            if (param.name == paramName && param.type == AnimatorControllerParameterType.Bool)
            {
                feifeiAnimator.SetBool(paramName, value);
                return;
            }
        }

        // Animator 参数不存在，静默跳过（腓腓目前只有 Idle 状态）
    }

    public void SafeSetTrigger(string paramName)
    {
        if (feifeiAnimator == null) return;

        foreach (var param in feifeiAnimator.parameters)
        {
            if (param.name == paramName && param.type == AnimatorControllerParameterType.Trigger)
            {
                feifeiAnimator.SetTrigger(paramName);
                return;
            }
        }

        // Animator 参数不存在，静默跳过（腓腓目前只有 Idle 状态）
    }

    public void SafePlayState(string stateName)
    {
        if (feifeiAnimator == null || string.IsNullOrWhiteSpace(stateName)) return;

        for (int i = 0; i < feifeiAnimator.layerCount; i++)
        {
            if (feifeiAnimator.HasState(i, Animator.StringToHash(stateName)))
            {
                feifeiAnimator.Play(stateName);
                return;
            }
        }

        // Animator 状态不存在，静默跳过（腓腓目前只有 Idle 状态）
    }

    private static float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }
}
