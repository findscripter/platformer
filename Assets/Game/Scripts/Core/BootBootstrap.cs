using UnityEngine;
using UnityEngine.SceneManagement;

public class BootBootstrap : MonoBehaviour
{
    [SerializeField] private GameObject persistentRoot;
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    // [SerializeField] private string gameplaySceneName = "Gameplay";

    private bool hasLoadedEntryScene;

    private void Awake()
    {
        if (persistentRoot == null)
        {
            Debug.LogError("BootBootstrap: PersistentRoot is not assigned.");
            return;
        }

        if (BootPersistentRoot.Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        if (persistentRoot.GetComponent<BootPersistentRoot>() == null)
        {
            persistentRoot.AddComponent<BootPersistentRoot>();
        }
    }

    private void Start()
    {
        if (hasLoadedEntryScene || persistentRoot == null)
        {
            return;
        }

        hasLoadedEntryScene = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(ResolveEntrySceneName(), LoadSceneMode.Single);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (GameLoop.Instance == null)
        {
            Debug.LogError("BootBootstrap: GameLoop not found in persistent root.");
            return;
        }

        var bridge = FindGameplaySceneBridge(scene);
        bridge?.Apply(GameLoop.Instance);

        GameLoop.Instance.Begin(ResolveInitialState(scene.name));
    }

    private string ResolveEntrySceneName()
    {
        if (IsDevelopmentMode())
        {
            if (!string.IsNullOrEmpty(BootSession.EntrySceneName))
            {
                return BootSession.EntrySceneName;
            }

#if UNITY_EDITOR
            var editorEntryScene = UnityEditor.EditorPrefs.GetString(BootSession.EntrySceneNameKey, string.Empty);
            if (!string.IsNullOrEmpty(editorEntryScene))
            {
                return editorEntryScene;
            }
#endif
        }

        return mainMenuSceneName;
    }

    private static GameStateType ResolveInitialState(string sceneName)
    {
        if (sceneName == "MainMenu")
        {
            return GameStateType.MainMenu;
        }

        if (sceneName == "PlayerGuide")
        {
            return GameStateType.PlayerGuide;
        }

        if (sceneName == "Gameplay")
        {
            return GameStateType.Loading;
        }

        return GameStateType.MainMenu;
    }

    private static GameplaySceneBridge FindGameplaySceneBridge(Scene scene)
    {
        if (!scene.isLoaded)
        {
            return null;
        }

        foreach (var root in scene.GetRootGameObjects())
        {
            var bridge = root.GetComponentInChildren<GameplaySceneBridge>(true);
            if (bridge != null)
            {
                return bridge;
            }
        }

        return null;
    }

    private static bool IsDevelopmentMode()
    {
#if UNITY_EDITOR
        return true;
#else
        return false;
#endif
    }
}
