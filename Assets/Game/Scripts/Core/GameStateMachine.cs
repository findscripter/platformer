using System.Collections.Generic;
using UnityEngine;

public class GameStateMachine
{
    private readonly Dictionary<GameStateType, IGameState> states = new();

    private IGameState currentState;

    public GameStateType CurrentStateType { get; private set; }

    public void RegisterState(GameStateType type, IGameState state)
    {
        if (states.ContainsKey(type))
        {
            Debug.LogWarning($"State already registered: {type}");
            return;
        }

        states.Add(type, state);
    }

    public void ChangeState(GameStateType newStateType)
    {
        if (currentState != null && CurrentStateType == newStateType)
        {
            return;
        }

        if (!states.TryGetValue(newStateType, out IGameState newState))
        {
            Debug.LogError($"State not registered: {newStateType}");
            return;
        }

        Debug.Log($"Change State: {CurrentStateType} -> {newStateType}");

        currentState?.Exit();

        CurrentStateType = newStateType;
        currentState = newState;

        currentState.Enter();
    }

    public void Update(float deltaTime)
    {
        currentState?.Update(deltaTime);
    }

    public void FixedUpdate(float fixedDeltaTime)
    {
        currentState?.FixedUpdate(fixedDeltaTime);
    }
}
