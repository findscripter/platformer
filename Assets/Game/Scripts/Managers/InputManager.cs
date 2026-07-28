using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private bool inputEnabled;

    private InputActionMap gameplayMap;
    private InputActionMap uiMap;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction interactAction;
    private InputAction gameplayPauseAction;
    private InputAction confirmAction;
    private InputAction cancelAction;

    public float MoveX { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool InteractPressed { get; private set; }
    public bool PausePressed { get; private set; }
    public bool ConfirmPressed { get; private set; }

    public bool GameplayInputEnabled { get => inputEnabled; private set => inputEnabled = value; }
    public bool UIInputEnabled { get; private set; }

    private void Awake()
    {
        if (inputActions == null)
        {
            Debug.LogError("InputManager: InputActionAsset is not assigned.");
            return;
        }

        gameplayMap = inputActions.FindActionMap("Gameplay");
        uiMap = inputActions.FindActionMap("UI");

        moveAction = gameplayMap.FindAction("Move");
        jumpAction = gameplayMap.FindAction("Jump");
        interactAction = gameplayMap.FindAction("Interact");
        gameplayPauseAction = gameplayMap.FindAction("Pause");
        confirmAction = uiMap.FindAction("Confirm");
        cancelAction = uiMap.FindAction("Cancel");
    }

    private void OnDestroy()
    {
        DisableAllInput();
    }

    public void Tick()
    {
        MoveX = 0f;
        InteractPressed = false;
        JumpPressed = false;
        PausePressed = false;
        ConfirmPressed = false;

        if (moveAction == null)
            return;

        if (GameplayInputEnabled)
        {
            MoveX = moveAction.ReadValue<Vector2>().x;
            JumpPressed = jumpAction.WasPressedThisFrame();
            if (interactAction != null)
                InteractPressed = interactAction.WasPressedThisFrame();
            PausePressed = gameplayPauseAction.WasPressedThisFrame();
            
        }

        if (UIInputEnabled)
        {
            ConfirmPressed = confirmAction.WasPressedThisFrame();
            PausePressed = cancelAction.WasPressedThisFrame();
        }
    }

    public void EnableGameplayInput()
    {
        GameplayInputEnabled = true;
        UIInputEnabled = false;

        uiMap?.Disable();
        gameplayMap?.Enable();
    }

    public void EnableUIInput()
    {
        GameplayInputEnabled = false;
        UIInputEnabled = true;

        gameplayMap?.Disable();
        uiMap?.Enable();
    }

    public void DisableAllInput()
    {
        GameplayInputEnabled = false;
        UIInputEnabled = false;

        gameplayMap?.Disable();
        uiMap?.Disable();
    }
}
