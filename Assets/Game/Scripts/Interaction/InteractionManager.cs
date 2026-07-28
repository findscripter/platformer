using System.Collections.Generic;
using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }

    private readonly List<IInteractable> inRange = new();

    public IInteractable CurrentFocus { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Register(IInteractable interactable)
    {
        if (interactable == null || inRange.Contains(interactable))
            return;

        inRange.Add(interactable);
    }

    public void Unregister(IInteractable interactable)
    {
        if (interactable == null)
            return;

        inRange.Remove(interactable);
    }

    public void Tick(GameContext context)
    {
        if (context?.Player == null)
            return;

        RefreshFocus(context.Player.transform.position);
    }

    public bool TryInteract(GameContext context)
    {
        if (context?.InputManager == null || CurrentFocus == null || !CurrentFocus.CanInteract)
            return false;

        if (!context.InputManager.InteractPressed)
            return false;

        Vector3 playerPosition = context.Player != null
            ? context.Player.transform.position
            : Vector3.zero;

        EventCenter.Publish(
            GameEvents.InteractStarted,
            new InteractRecord(
                CurrentFocus.InteractableId,
                CurrentFocus.GetType().Name,
                playerPosition));

        CurrentFocus.Interact(context);
        RefreshFocus(playerPosition);
        return true;
    }

    private void RefreshFocus(Vector3 playerPosition)
    {
        IInteractable best = null;
        float bestDistance = float.MaxValue;

        for (int i = inRange.Count - 1; i >= 0; i--)
        {
            IInteractable candidate = inRange[i];
            if (candidate == null || candidate.Transform == null)
            {
                inRange.RemoveAt(i);
                continue;
            }

            if (!candidate.CanInteract)
                continue;

            float distance = Vector2.Distance(playerPosition, candidate.Transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = candidate;
            }
        }

        if (CurrentFocus == best)
            return;

        CurrentFocus = best;
        EventCenter.Publish(GameEvents.InteractionFocusChanged, CurrentFocus);
    }
}
