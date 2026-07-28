#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class BootPlayModeRedirect
{
    private const string BootScenePath = "Assets/Game/Scenes/Boot.unity";

    static BootPlayModeRedirect()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            var bootScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScenePath);
            if (bootScene == null)
            {
                return;
            }

            var activeScene = EditorSceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(activeScene.path) && activeScene.path != BootScenePath)
            {
                BootSession.EntrySceneName = activeScene.name;
                EditorPrefs.SetString(BootSession.EntrySceneNameKey, activeScene.name);
            }

            EditorSceneManager.playModeStartScene = bootScene;
            return;
        }

        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorSceneManager.playModeStartScene = null;
            BootSession.EntrySceneName = null;
        }
    }
}
#endif
