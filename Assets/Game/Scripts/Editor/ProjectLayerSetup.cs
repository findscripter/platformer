using System;
using UnityEditor;
using UnityEngine;

public static class ProjectLayerSetup
{
    private static readonly string[] SortingLayerNames =
    {
        GameLayers.BackgroundSorting,
        GameLayers.MidgroundSorting,
        GameLayers.InteractiveSorting,
        GameLayers.ForegroundSorting,
        GameLayers.UiSorting
    };

    private static readonly string[] PhysicsLayerNames =
    {
        GameLayers.Ground,
        GameLayers.Platform,
        GameLayers.Player,
        GameLayers.Enemy,
        GameLayers.Trigger,
        GameLayers.Collectible,
        GameLayers.Mechanism
    };

    [MenuItem("Tools/Project Structure/Setup Project Layers")]
    public static void SetupFromMenu()
    {
        Setup();
        Debug.Log("Project sorting layers and physics layers configured.");
    }

    public static void SetupBatch()
    {
        try
        {
            Setup();
        }
        catch (Exception exception)
        {
            Debug.LogError($"Project layer setup failed: {exception}");
            EditorApplication.Exit(1);
            return;
        }

        EditorApplication.Exit(0);
    }

    public static void Setup()
    {
        EnsureSortingLayers();
        EnsurePhysicsLayers();
        ConfigurePhysics2DCollisionMatrix();
        AssetDatabase.SaveAssets();
    }

    private static void EnsureSortingLayers()
    {
        var tagManager = GetTagManager();
        var sortingLayers = tagManager.FindProperty("m_SortingLayers");

        foreach (var layerName in SortingLayerNames)
        {
            if (HasSortingLayer(sortingLayers, layerName))
            {
                continue;
            }

            sortingLayers.InsertArrayElementAtIndex(sortingLayers.arraySize);
            var entry = sortingLayers.GetArrayElementAtIndex(sortingLayers.arraySize - 1);
            entry.FindPropertyRelative("name").stringValue = layerName;
            entry.FindPropertyRelative("uniqueID").intValue = GenerateSortingLayerId(layerName);
            entry.FindPropertyRelative("locked").intValue = 0;
        }

        tagManager.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsurePhysicsLayers()
    {
        var tagManager = GetTagManager();
        var layers = tagManager.FindProperty("layers");

        for (var index = 0; index < PhysicsLayerNames.Length; index++)
        {
            var layerName = PhysicsLayerNames[index];
            var layerIndex = 6 + index;
            var layerProperty = layers.GetArrayElementAtIndex(layerIndex);

            if (!string.IsNullOrEmpty(layerProperty.stringValue) &&
                layerProperty.stringValue != layerName)
            {
                throw new InvalidOperationException(
                    $"Physics layer slot {layerIndex} is already used by '{layerProperty.stringValue}', expected '{layerName}'.");
            }

            layerProperty.stringValue = layerName;
        }

        tagManager.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigurePhysics2DCollisionMatrix()
    {
        for (var layerA = 0; layerA < 32; layerA++)
        {
            for (var layerB = 0; layerB < 32; layerB++)
            {
                Physics2D.IgnoreLayerCollision(layerA, layerB, false);
            }
        }

        var ground = LayerMask.NameToLayer(GameLayers.Ground);
        var platform = LayerMask.NameToLayer(GameLayers.Platform);
        var player = LayerMask.NameToLayer(GameLayers.Player);
        var enemy = LayerMask.NameToLayer(GameLayers.Enemy);
        var trigger = LayerMask.NameToLayer(GameLayers.Trigger);
        var collectible = LayerMask.NameToLayer(GameLayers.Collectible);
        var mechanism = LayerMask.NameToLayer(GameLayers.Mechanism);

        IgnorePair(trigger, ground);
        IgnorePair(trigger, platform);
        IgnorePair(trigger, mechanism);
        IgnorePair(trigger, collectible);
        IgnorePair(trigger, trigger);

        IgnorePair(collectible, ground);
        IgnorePair(collectible, platform);
        IgnorePair(collectible, enemy);
        IgnorePair(collectible, mechanism);
        IgnorePair(collectible, trigger);
        IgnorePair(collectible, collectible);

        IgnorePair(player, player);
    }

    private static void IgnorePair(int layerA, int layerB)
    {
        if (layerA < 0 || layerB < 0)
        {
            return;
        }

        Physics2D.IgnoreLayerCollision(layerA, layerB, true);
    }

    private static SerializedObject GetTagManager()
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0)
        {
            throw new InvalidOperationException("TagManager.asset not found.");
        }

        return new SerializedObject(assets[0]);
    }

    private static bool HasSortingLayer(SerializedProperty sortingLayers, string layerName)
    {
        for (var index = 0; index < sortingLayers.arraySize; index++)
        {
            var entry = sortingLayers.GetArrayElementAtIndex(index);
            if (entry.FindPropertyRelative("name").stringValue == layerName)
            {
                return true;
            }
        }

        return false;
    }

    private static int GenerateSortingLayerId(string layerName)
    {
        unchecked
        {
            const int seed = 0x2D504C52;
            var hash = seed;

            foreach (var character in layerName)
            {
                hash = (hash * 397) ^ character;
            }

            if (hash == 0)
            {
                hash = 1;
            }

            return hash;
        }
    }
}
