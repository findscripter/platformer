using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class UIButtonAnimationUtility
{
    public const string ControllerAssetPath = "Assets/Game/Resources/UI/UIButton.controller";
    private const string MigrationEditorPrefKey = "UIButtonAnimationUtility.Migration.v1";
    private static bool _autoApplyScheduled;

    [InitializeOnLoadMethod]
    private static void RegisterAutoApply()
    {
        EditorApplication.hierarchyChanged += ScheduleAutoApplyForNewButtons;
        ObjectChangeEvents.changesPublished += OnObjectChangesPublished;
        ApplyMigrationOnLoad();
    }

    private static void OnObjectChangesPublished(ref ObjectChangeEventStream stream)
    {
        for (var index = 0; index < stream.length; index++)
        {
            var changeKind = stream.GetEventType(index);
            if (changeKind is ObjectChangeKind.CreateGameObjectHierarchy
                or ObjectChangeKind.ChangeGameObjectStructure
                or ObjectChangeKind.ChangeGameObjectOrComponentProperties)
            {
                ScheduleAutoApplyForNewButtons();
                return;
            }
        }
    }

    private static void ScheduleAutoApplyForNewButtons()
    {
        if (Application.isPlaying || _autoApplyScheduled)
        {
            return;
        }

        _autoApplyScheduled = true;
        EditorApplication.delayCall += AutoApplyForNewButtons;
    }

    private static void AutoApplyForNewButtons()
    {
        _autoApplyScheduled = false;

        if (Application.isPlaying || !File.Exists(ControllerAssetPath))
        {
            return;
        }

        var updatedCount = 0;
        foreach (var button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include))
        {
            if (!IsConfigured(button))
            {
                Apply(button);
                updatedCount++;
            }
        }

        if (updatedCount > 0)
        {
            Debug.Log($"Auto-applied UI button hover animation to {updatedCount} button(s).");
        }
    }

    private static void ApplyMigrationOnLoad()
    {
        if (EditorPrefs.GetBool(MigrationEditorPrefKey, false))
        {
            return;
        }

        EditorApplication.delayCall += () =>
        {
            if (EditorPrefs.GetBool(MigrationEditorPrefKey, false))
            {
                return;
            }

            if (!File.Exists(ControllerAssetPath))
            {
                return;
            }

            ApplyToAllExisting();
            EditorPrefs.SetBool(MigrationEditorPrefKey, true);
        };
    }

    public static void Apply(Button button)
    {
        if (button == null || IsConfigured(button))
        {
            return;
        }

        RuntimeAnimatorController controller =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerAssetPath);
        if (controller == null)
        {
            Debug.LogWarning($"Missing UI button animator: {ControllerAssetPath}");
            return;
        }

        button.transition = Selectable.Transition.Animation;

        Animator animator = button.GetComponent<Animator>();
        if (animator == null)
        {
            animator = button.gameObject.AddComponent<Animator>();
        }

        animator.runtimeAnimatorController = controller;
        EditorUtility.SetDirty(button);

        if (button.gameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(button.gameObject.scene);
        }
    }

    private static bool IsConfigured(Button button)
    {
        if (button.transition != Selectable.Transition.Animation)
        {
            return false;
        }

        if (!button.TryGetComponent(out Animator animator) || animator.runtimeAnimatorController == null)
        {
            return false;
        }

        RuntimeAnimatorController expected =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerAssetPath);
        return expected != null && animator.runtimeAnimatorController == expected;
    }

    [MenuItem("Tools/UI/Apply Button Hover Animation To All")]
    public static void ApplyToAllExisting()
    {
        var updatedCount = 0;

        foreach (var scenePath in FindAssetPaths("t:Scene", "Assets/Game"))
        {
            updatedCount += ApplyToScene(scenePath);
        }

        foreach (var prefabPath in FindAssetPaths("t:Prefab", "Assets/Game"))
        {
            updatedCount += ApplyToPrefab(prefabPath);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Applied UI button hover animation to {updatedCount} buttons.");
    }

    public static void ApplyToAllExistingBatch()
    {
        try
        {
            ApplyToAllExisting();
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
            return;
        }

        EditorApplication.Exit(0);
    }

    private static IEnumerable<string> FindAssetPaths(string filter, string rootFolder)
    {
        foreach (var guid in AssetDatabase.FindAssets(filter, new[] { rootFolder }))
        {
            yield return AssetDatabase.GUIDToAssetPath(guid);
        }
    }

    private static int ApplyToScene(string scenePath)
    {
        if (!File.Exists(scenePath))
        {
            return 0;
        }

        var activeScene = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        var updatedCount = ApplyToButtons(scene.GetRootGameObjects());

        if (updatedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        EditorSceneManager.CloseScene(scene, true);

        if (activeScene.IsValid() && activeScene.isLoaded)
        {
            SceneManager.SetActiveScene(activeScene);
        }

        return updatedCount;
    }

    private static int ApplyToPrefab(string prefabPath)
    {
        if (!File.Exists(prefabPath))
        {
            return 0;
        }

        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var updatedCount = ApplyToButtons(new[] { root });
            if (updatedCount > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }

            return updatedCount;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static int ApplyToButtons(IEnumerable<GameObject> roots)
    {
        var updatedCount = 0;

        foreach (var root in roots)
        {
            if (root == null)
            {
                continue;
            }

            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                if (!IsConfigured(button))
                {
                    Apply(button);
                }

                updatedCount++;
            }
        }

        return updatedCount;
    }
}
