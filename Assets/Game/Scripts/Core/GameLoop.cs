using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLoop : MonoBehaviour
{
    public static GameLoop Instance { get; private set; }

    [Header("Managers")]
    [SerializeField] private InputManager inputManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private SceneTransitionManager sceneTransitionManager;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private InteractionManager interactionManager;
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private DialogueProgressService dialogueProgressService;
    [SerializeField] private SaveSystem saveSystem;

    [Header("Player")]
    [SerializeField] private PlayerController player;

    [Header("Spawn")]
    [SerializeField] private Transform playerSpawnPoint;

    private GameContext context;
    private GameStateMachine stateMachine;
    private bool hasBegun;
    private bool isSceneTransitioning;
    private bool isReturningToMainMenu;

    private const string PlayerGuideSceneName = "PlayerGuide";
    private const string GameplaySceneName = "Gameplay";
    private const string MainMenuSceneName = "MainMenu";

    public LevelManager LevelManager => levelManager;
    public bool IsSceneTransitioning => isSceneTransitioning;
    public SaveSystem SaveSystem => saveSystem;
    public GameContext Context => context;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureInteractionFramework();
        EnsureSceneTransitionManager();
        CreateContext();
        CreateStateMachine();
        RegisterStates();
    }

    private void EnsureSceneTransitionManager()
    {
        Transform managersRoot = transform.parent != null ? transform.parent : transform;

        if (sceneTransitionManager == null)
        {
            sceneTransitionManager = managersRoot.GetComponentInChildren<SceneTransitionManager>(true);
        }

        if (sceneTransitionManager == null)
        {
            foreach (Transform child in managersRoot)
            {
                if (child.name != "SceneTransitionManager")
                {
                    continue;
                }

                sceneTransitionManager = child.GetComponent<SceneTransitionManager>() ??
                                         child.gameObject.AddComponent<SceneTransitionManager>();
                break;
            }
        }

        if (sceneTransitionManager == null)
        {
            var transitionObject = new GameObject("SceneTransitionManager");
            transitionObject.transform.SetParent(managersRoot, false);
            sceneTransitionManager = transitionObject.AddComponent<SceneTransitionManager>();
        }
    }

    private void EnsureInteractionFramework()
    {
        Transform managersRoot = transform.parent != null ? transform.parent : transform;

        if (interactionManager == null)
        {
            interactionManager = managersRoot.GetComponentInChildren<InteractionManager>(true);
            if (interactionManager == null)
            {
                var interactionObject = new GameObject("InteractionManager");
                interactionObject.transform.SetParent(managersRoot, false);
                interactionManager = interactionObject.AddComponent<InteractionManager>();
            }
        }

        if (dialogueManager == null)
        {
            dialogueManager = managersRoot.GetComponentInChildren<DialogueManager>(true);
            if (dialogueManager == null)
            {
                var dialogueObject = new GameObject("DialogueManager");
                dialogueObject.transform.SetParent(managersRoot, false);
                dialogueManager = dialogueObject.AddComponent<DialogueManager>();
            }
        }

        if (dialogueProgressService == null)
        {
            dialogueProgressService = managersRoot.GetComponentInChildren<DialogueProgressService>(true);
            if (dialogueProgressService == null)
                dialogueProgressService = dialogueManager.gameObject.AddComponent<DialogueProgressService>();
        }

        if (saveSystem == null)
        {
            saveSystem = managersRoot.GetComponentInChildren<SaveSystem>(true);
            if (saveSystem == null)
                saveSystem = dialogueManager.gameObject.AddComponent<SaveSystem>();
        }

        if (dialogueManager.GetComponent<DialogueUIBootstrap>() == null &&
            dialogueManager.GetComponent<DialogueUIRuntimeBootstrap>() == null)
        {
            dialogueManager.gameObject.AddComponent<DialogueUIRuntimeBootstrap>();
        }
    }

    public void LoadSceneForDialogue(string sceneName, DialogueNode pendingNextNode, bool resumeDialogueAfterLoad)
    {
        if (!hasBegun || isSceneTransitioning || isReturningToMainMenu || string.IsNullOrWhiteSpace(sceneName))
            return;

        StartCoroutine(LoadSceneForDialogueRoutine(sceneName, pendingNextNode, resumeDialogueAfterLoad));
    }

    public bool SaveGame()
    {
        return saveSystem != null && saveSystem.Save(context);
    }

    public bool LoadGame()
    {
        if (saveSystem == null || !saveSystem.Load())
            return false;

        saveSystem.ApplyLoadedState(context);
        return true;
    }

    private void Update()
    {
        if (!hasBegun || stateMachine == null)
        {
            return;
        }

        inputManager.Tick();
        stateMachine.Update(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        if (!hasBegun || stateMachine == null)
        {
            return;
        }

        stateMachine.FixedUpdate(Time.fixedDeltaTime);
    }

    public void Begin(GameStateType initialState)
    {
        if (hasBegun)
        {
            return;
        }

        hasBegun = true;
        stateMachine.ChangeState(initialState);
    }

    public void StartNewGame()
    {
        if (!hasBegun || isSceneTransitioning || isReturningToMainMenu)
        {
            return;
        }

        StartCoroutine(LoadPlayerGuideRoutine());
    }

    public void ContinueToGameplay()
    {
        if (!hasBegun || isSceneTransitioning || isReturningToMainMenu)
        {
            return;
        }

        StartCoroutine(LoadGameplayRoutine());
    }

    public void ResumeGame()
    {
        if (!hasBegun || stateMachine.CurrentStateType != GameStateType.Paused)
        {
            return;
        }

        stateMachine.ChangeState(GameStateType.Playing);
    }

    public void ReturnToMainMenu()
    {
        if (!hasBegun || isReturningToMainMenu)
        {
            return;
        }

        saveSystem?.Save(context);
        StartCoroutine(ReturnToMainMenuRoutine());
    }

    public void QuitGame()
    {
        saveSystem?.Save(context);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void BindGameplay(PlayerController gameplayPlayer, Transform spawnPoint, Collider2D goalTrigger)
    {
        player = gameplayPlayer;
        playerSpawnPoint = spawnPoint;

        context.Player = player;
        context.PlayerSpawnPoint = playerSpawnPoint;

        if (levelManager != null)
        {
            levelManager.SetGameplayReferences(spawnPoint, goalTrigger);
        }
    }

    private IEnumerator LoadSceneForDialogueRoutine(
        string sceneName,
        DialogueNode pendingNextNode,
        bool resumeDialogueAfterLoad)
    {
        isSceneTransitioning = true;
        bool dialogueWasRunning = dialogueManager != null && dialogueManager.IsRunning;

        if (dialogueWasRunning)
            dialogueManager.SuspendForSceneTransition();

        if (stateMachine.CurrentStateType != GameStateType.Dialogue)
            inputManager.DisableAllInput();

        var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        if (operation == null)
        {
            Debug.LogError($"GameLoop: scene '{sceneName}' could not be loaded for dialogue.");
            isSceneTransitioning = false;
            if (dialogueWasRunning && resumeDialogueAfterLoad)
                dialogueManager.ResumeAfterSceneTransition();
            yield break;
        }

        while (!operation.isDone)
            yield return null;

        if (sceneName == GameplaySceneName)
        {
            var bridge = FindGameplaySceneBridge(SceneManager.GetActiveScene());
            if (bridge != null)
                bridge.Apply(this);
        }

        dialogueProgressService?.RestoreNpcOnceStates();
        isSceneTransitioning = false;

        if (dialogueWasRunning && resumeDialogueAfterLoad)
        {
            context.StateMachine.ChangeState(GameStateType.Dialogue);
            context.InputManager.EnableUIInput();
            context.Player?.SetInputEnabled(false);
            dialogueManager.ResumeAfterSceneTransition();

            if (pendingNextNode != null)
                dialogueManager.AdvanceTo(pendingNextNode);
        }
        else if (stateMachine.CurrentStateType != GameStateType.Dialogue)
        {
            inputManager.EnableGameplayInput();
        }

        saveSystem?.CaptureCurrentState(context);
    }

    private IEnumerator LoadPlayerGuideRoutine()
    {
        isSceneTransitioning = true;
        inputManager.DisableAllInput();
        uiManager.HideAll();

        var operation = SceneManager.LoadSceneAsync(PlayerGuideSceneName, LoadSceneMode.Single);
        if (operation == null)
        {
            Debug.LogError($"GameLoop: {PlayerGuideSceneName} scene could not be loaded.");
            isSceneTransitioning = false;
            stateMachine.ChangeState(GameStateType.MainMenu);
            yield break;
        }

        while (!operation.isDone)
        {
            yield return null;
        }

        isSceneTransitioning = false;
        stateMachine.ChangeState(GameStateType.PlayerGuide);
    }

    private IEnumerator LoadGameplayRoutine()
    {
        isSceneTransitioning = true;
        inputManager.DisableAllInput();
        uiManager.HideAll();
        sceneTransitionManager?.ShowBlackImmediate();

        var operation = SceneManager.LoadSceneAsync(GameplaySceneName, LoadSceneMode.Single);
        if (operation == null)
        {
            Debug.LogError($"GameLoop: {GameplaySceneName} scene could not be loaded.");
            isSceneTransitioning = false;
            stateMachine.ChangeState(GameStateType.MainMenu);
            yield break;
        }

        while (!operation.isDone)
        {
            yield return null;
        }

        var bridge = FindGameplaySceneBridge(SceneManager.GetActiveScene());
        if (bridge == null)
        {
            Debug.LogError("GameLoop: GameplaySceneBridge was not found after loading Gameplay.");
            isSceneTransitioning = false;
            yield break;
        }

        bridge.Apply(this);

        if (context.Player == null)
        {
            Debug.LogError("GameLoop: Gameplay player was not bound.");
            isSceneTransitioning = false;
            yield break;
        }

        isSceneTransitioning = false;
        stateMachine.ChangeState(GameStateType.Loading);
    }

    private IEnumerator ReturnToMainMenuRoutine()
    {
        isReturningToMainMenu = true;
        inputManager.DisableAllInput();
        uiManager.HideAll();
        Time.timeScale = 1f;

        var operation = SceneManager.LoadSceneAsync(MainMenuSceneName, LoadSceneMode.Single);
        if (operation == null)
        {
            Debug.LogError("GameLoop: MainMenu scene could not be loaded.");
            isReturningToMainMenu = false;
            yield break;
        }

        while (!operation.isDone)
        {
            yield return null;
        }

        player = null;
        playerSpawnPoint = null;
        context.Player = null;
        context.PlayerSpawnPoint = null;
        context.IsLevelLoaded = false;
        levelManager.SetGameplayReferences(null, null);

        isReturningToMainMenu = false;
        stateMachine.ChangeState(GameStateType.MainMenu);
    }

    private static GameplaySceneBridge FindGameplaySceneBridge(Scene scene)
    {
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

    private void CreateContext()
    {
        context = new GameContext
        {
            GameLoop = this,
            InputManager = inputManager,
            UIManager = uiManager,
            SceneTransitionManager = sceneTransitionManager,
            LevelManager = levelManager,
            InteractionManager = interactionManager,
            DialogueManager = dialogueManager,
            DialogueProgressService = dialogueProgressService,
            SaveSystem = saveSystem,
            Player = player,
            PlayerSpawnPoint = playerSpawnPoint
        };
    }

    private void CreateStateMachine()
    {
        stateMachine = new GameStateMachine();
        context.StateMachine = stateMachine;
    }

    private void RegisterStates()
    {
        stateMachine.RegisterState(GameStateType.MainMenu, new MainMenuState(context));
        stateMachine.RegisterState(GameStateType.PlayerGuide, new PlayerGuideState(context));
        stateMachine.RegisterState(GameStateType.Loading, new LoadingState(context));
        stateMachine.RegisterState(GameStateType.Playing, new PlayingState(context));
        stateMachine.RegisterState(GameStateType.Paused, new PauseState(context));
        stateMachine.RegisterState(GameStateType.PlayerDead, new PlayerDeadState(context));
        stateMachine.RegisterState(GameStateType.Respawning, new RespawnState(context));
        stateMachine.RegisterState(GameStateType.LevelClear, new LevelClearState(context));
        stateMachine.RegisterState(GameStateType.Result, new ResultState(context));
        stateMachine.RegisterState(GameStateType.Dialogue, new DialogueState(context));
    }
}
