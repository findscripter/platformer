using UnityEditor;

/// <summary>仅操作内存中的字体绑定；不保存、导入资源，也不修改场景文件。</summary>
[InitializeOnLoad]
public static class GuideUiFontPlayModeBridge
{
    static GuideUiFontPlayModeBridge()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        AssemblyReloadEvents.beforeAssemblyReload += BeforeAssemblyReload;
        EditorApplication.quitting += GuideUiFont.EndPlaySession;
        if (EditorApplication.isPlaying)
            EditorApplication.delayCall += ResumeAfterAssemblyReload;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        switch (state)
        {
            case PlayModeStateChange.ExitingEditMode:
            case PlayModeStateChange.EnteredPlayMode:
                GuideUiFont.BeginPlaySession();
                GuideUiFont.RebindLoadedTexts();
                break;
            case PlayModeStateChange.ExitingPlayMode:
                GuideUiFont.SuspendPlaySession();
                break;
            case PlayModeStateChange.EnteredEditMode:
                GuideUiFont.EndPlaySession();
                break;
        }
    }

    private static void BeforeAssemblyReload()
    {
        GuideUiFont.EndPlaySession(!EditorApplication.isPlaying);
    }

    private static void ResumeAfterAssemblyReload()
    {
        if (!EditorApplication.isPlaying)
            return;
        GuideUiFont.BeginPlaySession();
        GuideUiFont.RebindLoadedTexts();
    }
}
