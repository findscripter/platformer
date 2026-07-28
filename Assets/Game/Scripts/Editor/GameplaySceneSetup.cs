using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameplaySceneSetup
{
    private const string GameplayScenePath = "Assets/Game/Scenes/Gameplay.unity";

    [MenuItem("Tools/Project Structure/Setup Gameplay Game Loop")]
    public static void SetupFromMenu()
    {
        Setup();
    }

    public static void SetupBatch()
    {
        try
        {
            Setup();
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"Gameplay scene setup failed: {exception}");
            EditorApplication.Exit(1);
            return;
        }

        EditorApplication.Exit(0);
    }

    private static void Setup()
    {
        if (!File.Exists(GameplayScenePath))
        {
            throw new FileNotFoundException($"Missing scene: {GameplayScenePath}");
        }

        ProjectLayerSetup.Setup();

        EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);

        var spawnPoint = EnsurePath("SceneRoot/GameplayLayer/SpawnPoint");
        spawnPoint.transform.position = new Vector3(0f, 1f, 0f);

        var goalTrigger = EnsurePath("SceneRoot/GameplayLayer/Runtime_Exits/GoalTrigger");
        var goalCollider = EnsureComponent<BoxCollider2D>(goalTrigger);
        goalCollider.isTrigger = true;
        goalCollider.size = new Vector2(2f, 2f);
        SetLayerRecursively(goalTrigger, LayerMask.NameToLayer(GameLayers.Trigger));

        var player = EnsurePath("SceneRoot/GameplayLayer/Runtime_Player");
        SetupPlayer(player);

        var bridgeRoot = EnsurePath("SceneRoot");
        var bridge = EnsureComponent<GameplaySceneBridge>(bridgeRoot);
        AssignGameplayBridge(bridge, player, spawnPoint.transform, goalCollider);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        Debug.Log("Gameplay scene setup completed. Persistent managers/UI belong in Boot scene.");
    }

    private static void SetupPlayer(GameObject player)
    {
        var rigidbody = EnsureComponent<Rigidbody2D>(player);
        rigidbody.gravityScale = 3f;
        rigidbody.freezeRotation = true;

        SetLayerRecursively(player, LayerMask.NameToLayer(GameLayers.Player));

        var collider = EnsureComponent<BoxCollider2D>(player);
        collider.size = new Vector2(0.8f, 1.2f);

        var groundCheckTransform = player.transform.Find("GroundCheck");
        if (groundCheckTransform == null)
        {
            var groundCheckObject = new GameObject("GroundCheck");
            groundCheckObject.transform.SetParent(player.transform, false);
            groundCheckObject.transform.localPosition = new Vector3(0f, -0.6f, 0f);
            groundCheckTransform = groundCheckObject.transform;
        }

        var controller = EnsureComponent<PlayerController>(player);
        var serializedController = new SerializedObject(controller);
        serializedController.FindProperty("groundCheck").objectReferenceValue = groundCheckTransform;
        serializedController.FindProperty("groundCheckRadius").floatValue = 0.1f;
        serializedController.FindProperty("groundLayer").intValue = GameLayers.GroundCheckMask.value;
        serializedController.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetLayerRecursively(GameObject gameObject, int layer)
    {
        if (layer < 0)
        {
            return;
        }

        gameObject.layer = layer;

        foreach (Transform child in gameObject.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private static void AssignGameplayBridge(
        GameplaySceneBridge bridge,
        GameObject player,
        Transform spawnPoint,
        BoxCollider2D goalTrigger)
    {
        var serializedBridge = new SerializedObject(bridge);
        serializedBridge.FindProperty("player").objectReferenceValue = player.GetComponent<PlayerController>();
        serializedBridge.FindProperty("spawnPoint").objectReferenceValue = spawnPoint;
        serializedBridge.FindProperty("goalTrigger").objectReferenceValue = goalTrigger;
        serializedBridge.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject EnsurePath(string path)
    {
        var segments = path.Split('/');
        GameObject current = null;

        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == segments[0])
            {
                current = root;
                break;
            }
        }

        if (current == null)
        {
            current = new GameObject(segments[0]);
        }

        for (var index = 1; index < segments.Length; index++)
        {
            var childTransform = current.transform.Find(segments[index]);
            if (childTransform == null)
            {
                var childObject = new GameObject(segments[index]);
                childObject.transform.SetParent(current.transform, false);
                current = childObject;
            }
            else
            {
                current = childTransform.gameObject;
            }
        }

        return current;
    }

    private static T EnsureComponent<T>(GameObject gameObject) where T : Component
    {
        var component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }
}
