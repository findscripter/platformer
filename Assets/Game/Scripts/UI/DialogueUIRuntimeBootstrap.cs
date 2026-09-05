using UnityEngine;

[RequireComponent(typeof(DialogueManager))]
public class DialogueUIRuntimeBootstrap : MonoBehaviour
{
    private void Awake()
    {
        if (GetComponent<DialogueUIBootstrap>() != null)
            return;

        Transform uiRoot = FindUiRoot();
        if (uiRoot == null)
            return;

        Transform existingCanvas = uiRoot.Find(DialogueUIBuilder.CanvasName);
        if (IsAlive(existingCanvas) && existingCanvas.Find(DialogueUIBuilder.PanelName) != null)
            return;

        DialogueManager dialogueManager = GetComponent<DialogueManager>();
        DialogueUIBuildResult buildResult = DialogueUIBuilder.Build(
            uiRoot,
            IsAlive(existingCanvas) ? existingCanvas : null);

        DialogueUIArtIntegration.ApplyArt(buildResult.PanelRoot);

        var bootstrap = EnsureComponent<DialogueUIBootstrap>(gameObject);
        bootstrap.Configure(
            dialogueManager,
            buildResult.DialogueUI,
            buildResult.ChoiceUI,
            buildResult.InputUI);
    }

    private Transform FindUiRoot()
    {
        var bootRoot = GetComponentInParent<BootPersistentRoot>();
        if (bootRoot != null)
        {
            Transform uiRoot = bootRoot.transform.Find("UI");
            if (IsAlive(uiRoot))
                return uiRoot;
        }

        Transform currentRoot = transform.root;
        if (!IsAlive(currentRoot))
            return null;

        Transform fallbackUi = currentRoot.Find("UI");
        return IsAlive(fallbackUi) ? fallbackUi : null;
    }

    private static bool IsAlive(Object unityObject)
    {
        return unityObject != null;
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }
}
