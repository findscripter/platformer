using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class SpritePrefabGenerator
{
    private const string MenuPath = "Assets/Create 2D Prefabs From Sprites";
    private const string OutputFolder = "Assets/Game/Prefabs/Generated";

    [MenuItem(MenuPath, false, 2000)]
    private static void CreatePrefabsFromSelectedSprites()
    {
        var sprites = GetSelectedSprites();
        if (sprites.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Create 2D Prefabs",
                "Select one or more Sprite assets, or a texture containing imported Sprites.",
                "OK");
            return;
        }

        EnsureFolderExists(OutputFolder);

        var createdPrefabs = new List<GameObject>(sprites.Count);
        try
        {
            for (var index = 0; index < sprites.Count; index++)
            {
                var sprite = sprites[index];
                EditorUtility.DisplayProgressBar(
                    "Creating 2D Prefabs",
                    sprite.name,
                    (float)index / sprites.Count);

                var prefab = CreatePrefab(sprite);
                if (prefab != null)
                {
                    createdPrefabs.Add(prefab);
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        if (createdPrefabs.Count > 0)
        {
            Selection.objects = createdPrefabs.ToArray();
            EditorGUIUtility.PingObject(createdPrefabs[createdPrefabs.Count - 1]);
        }

        Debug.Log(
            $"Created {createdPrefabs.Count} Sprite prefab(s) in {OutputFolder}. " +
            "Unique filenames were used to preserve existing prefabs.");
    }

    [MenuItem(MenuPath, true)]
    private static bool ValidateCreatePrefabsFromSelectedSprites()
    {
        return Selection.objects.Any(IsSpriteOrSpriteTexture);
    }

    private static GameObject CreatePrefab(Sprite sprite)
    {
        var root = new GameObject(sprite.name);
        try
        {
            var spriteRenderer = root.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;

            var polygonCollider = root.AddComponent<PolygonCollider2D>();
            CopyPhysicsShape(sprite, polygonCollider);

            var safeName = MakeSafeFileName(sprite.name);
            var desiredPath = $"{OutputFolder}/{safeName}.prefab";
            var uniquePath = AssetDatabase.GenerateUniqueAssetPath(desiredPath);

            return PrefabUtility.SaveAsPrefabAsset(root, uniquePath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void CopyPhysicsShape(Sprite sprite, PolygonCollider2D polygonCollider)
    {
        var shapeCount = sprite.GetPhysicsShapeCount();
        if (shapeCount <= 0)
        {
            return;
        }

        var paths = new List<Vector2[]>(shapeCount);
        var points = new List<Vector2>();

        for (var shapeIndex = 0; shapeIndex < shapeCount; shapeIndex++)
        {
            points.Clear();
            sprite.GetPhysicsShape(shapeIndex, points);

            if (points.Count >= 3)
            {
                paths.Add(points.ToArray());
            }
        }

        if (paths.Count == 0)
        {
            return;
        }

        polygonCollider.pathCount = paths.Count;
        for (var pathIndex = 0; pathIndex < paths.Count; pathIndex++)
        {
            polygonCollider.SetPath(pathIndex, paths[pathIndex]);
        }
    }

    private static List<Sprite> GetSelectedSprites()
    {
        var sprites = new HashSet<Sprite>();

        foreach (var selectedObject in Selection.objects)
        {
            if (selectedObject is Sprite selectedSprite)
            {
                sprites.Add(selectedSprite);
                continue;
            }

            if (!(selectedObject is Texture2D))
            {
                continue;
            }

            var assetPath = AssetDatabase.GetAssetPath(selectedObject);
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (asset is Sprite sprite)
                {
                    sprites.Add(sprite);
                }
            }
        }

        return sprites
            .OrderBy(AssetDatabase.GetAssetPath)
            .ThenBy(sprite => sprite.name)
            .ToList();
    }

    private static bool IsSpriteOrSpriteTexture(Object selectedObject)
    {
        if (selectedObject is Sprite)
        {
            return true;
        }

        if (!(selectedObject is Texture2D))
        {
            return false;
        }

        var assetPath = AssetDatabase.GetAssetPath(selectedObject);
        return AssetDatabase.LoadAllAssetsAtPath(assetPath).Any(asset => asset is Sprite);
    }

    private static string MakeSafeFileName(string fileName)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var safeCharacters = fileName
            .Select(character => invalidCharacters.Contains(character) ? '_' : character)
            .ToArray();
        var safeName = new string(safeCharacters).Trim();

        return string.IsNullOrEmpty(safeName) ? "Sprite" : safeName;
    }

    private static void EnsureFolderExists(string folderPath)
    {
        var pathParts = folderPath.Split('/');
        var currentPath = pathParts[0];

        for (var index = 1; index < pathParts.Length; index++)
        {
            var nextPath = $"{currentPath}/{pathParts[index]}";
            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, pathParts[index]);
            }

            currentPath = nextPath;
        }
    }
}
