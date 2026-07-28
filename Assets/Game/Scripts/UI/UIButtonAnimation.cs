using UnityEngine;
using UnityEngine.UI;

public static class UIButtonAnimation
{
    public const string ControllerResourcePath = "UI/UIButton";

    private static RuntimeAnimatorController _cachedController;

    public static void Apply(Button button)
    {
        if (button == null)
        {
            return;
        }

        RuntimeAnimatorController controller = GetController();
        if (controller == null)
        {
            return;
        }

        button.transition = Selectable.Transition.Animation;

        Animator animator = button.GetComponent<Animator>();
        if (animator == null)
        {
            animator = button.gameObject.AddComponent<Animator>();
        }

        animator.runtimeAnimatorController = controller;
    }

    public static RuntimeAnimatorController GetController()
    {
        if (_cachedController == null)
        {
            _cachedController = Resources.Load<RuntimeAnimatorController>(ControllerResourcePath);
        }

        return _cachedController;
    }
}
