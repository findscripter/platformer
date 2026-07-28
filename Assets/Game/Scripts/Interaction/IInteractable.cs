using UnityEngine;

public interface IInteractable
{
    Transform Transform { get; }
    string InteractableId { get; }
    string InteractPrompt { get; }
    bool CanInteract { get; }
    void Interact(GameContext context);
}
