using UnityEngine;

public class GameContext
{
    public GameLoop GameLoop;
    public GameStateMachine StateMachine;

    public InputManager InputManager;
    public UIManager UIManager;
    public SceneTransitionManager SceneTransitionManager;
    public LevelManager LevelManager;
    public InteractionManager InteractionManager;
    public DialogueManager DialogueManager;
    public DialogueProgressService DialogueProgressService;
    public SaveSystem SaveSystem;
    public PlayerController Player;

    public Transform PlayerSpawnPoint;

    public bool IsLevelLoaded;
    public bool IsPlayerDead;
    public bool IsLevelClear;

    public float DeadStateTimer;
    public float LevelClearTimer;

    // Player Guide Runtime Data
    public string PlayerDreamInput;
    public TarotResultData TarotResult;

    public void ResetRuntimeFlags()
    {
        IsPlayerDead = false;
        IsLevelClear = false;
        DeadStateTimer = 0f;
        LevelClearTimer = 0f;
    }

    public void ClearPlayerGuideData()
    {
        PlayerDreamInput = null;
        TarotResult = null;
    }
}
