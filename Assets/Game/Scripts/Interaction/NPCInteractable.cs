using UnityEngine;

public class NPCInteractable : InteractableBase
{
    [SerializeField] private DialogueSO dialogue;
    [SerializeField] private bool once;
    [SerializeField] private bool interacted;

    public override bool CanInteract => dialogue != null && (!once || !interacted);

    public override void Interact(GameContext context)
    {
        if (dialogue == null || context?.DialogueManager == null)
            return;

        interacted = true;
        context.DialogueProgressService?.RecordNpcInteraction(InteractableId);
        context.DialogueManager.BeginDialogue(dialogue, context);
    }

    public void SetDialogue(DialogueSO dialogueAsset)
    {
        dialogue = dialogueAsset;
    }

    public void RestoreInteracted(bool hasInteracted)
    {
        interacted = hasInteracted;
    }
}
