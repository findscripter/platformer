using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DialogueProgressService : MonoBehaviour
{
    private readonly HashSet<string> completedDialogueIds = new();
    private readonly List<DialogueChoiceRecord> choiceRecords = new();
    private readonly HashSet<string> flags = new();
    private readonly HashSet<string> interactedNpcIds = new();

    private DialogueSaveData runtimeData = new();

    private void OnEnable()
    {
        EventCenter.Subscribe(GameEvents.DialogueNodeEntered, OnDialogueNodeEntered);
        EventCenter.Subscribe(GameEvents.DialogueChoice, OnDialogueChoice);
        EventCenter.Subscribe(GameEvents.DialogueEnded, OnDialogueEnded);
    }

    private void OnDisable()
    {
        EventCenter.Unsubscribe(GameEvents.DialogueNodeEntered, OnDialogueNodeEntered);
        EventCenter.Unsubscribe(GameEvents.DialogueChoice, OnDialogueChoice);
        EventCenter.Unsubscribe(GameEvents.DialogueEnded, OnDialogueEnded);
    }

    public bool HasFlag(string flag)
    {
        return !string.IsNullOrWhiteSpace(flag) && flags.Contains(flag);
    }

    public void SetFlag(string flag)
    {
        if (string.IsNullOrWhiteSpace(flag))
            return;

        flags.Add(flag);
        runtimeData.flags = flags.ToArray();
    }

    public bool IsDialogueCompleted(string dialogueId)
    {
        return !string.IsNullOrWhiteSpace(dialogueId) && completedDialogueIds.Contains(dialogueId);
    }

    public bool HasInteractedWithNpc(string npcId)
    {
        return !string.IsNullOrWhiteSpace(npcId) && interactedNpcIds.Contains(npcId);
    }

    public void RecordNpcInteraction(string npcId)
    {
        if (string.IsNullOrWhiteSpace(npcId))
            return;

        interactedNpcIds.Add(npcId);
        runtimeData.interactedNpcIds = interactedNpcIds.ToArray();
    }

    public void CaptureToSaveData(GameSaveData saveData, GameContext context)
    {
        if (saveData == null)
            return;

        saveData.dialogue = BuildDialogueSaveData(context);
    }

    public void ApplyFromSaveData(GameSaveData saveData, GameContext context)
    {
        if (saveData?.dialogue == null)
            return;

        runtimeData = saveData.dialogue;
        completedDialogueIds.Clear();
        choiceRecords.Clear();
        flags.Clear();
        interactedNpcIds.Clear();

        if (runtimeData.completedDialogueIds != null)
        {
            foreach (string dialogueId in runtimeData.completedDialogueIds)
                completedDialogueIds.Add(dialogueId);
        }

        if (runtimeData.choices != null)
            choiceRecords.AddRange(runtimeData.choices);

        if (runtimeData.flags != null)
        {
            foreach (string flag in runtimeData.flags)
                flags.Add(flag);
        }

        if (runtimeData.interactedNpcIds != null)
        {
            foreach (string npcId in runtimeData.interactedNpcIds)
                interactedNpcIds.Add(npcId);
        }

        RestoreNpcOnceStates();
        TryResumeActiveDialogue(context);
    }

    private DialogueSaveData BuildDialogueSaveData(GameContext context)
    {
        var data = new DialogueSaveData
        {
            completedDialogueIds = completedDialogueIds.ToArray(),
            choices = choiceRecords.ToArray(),
            flags = flags.ToArray(),
            interactedNpcIds = interactedNpcIds.ToArray()
        };

        DialogueSnapshot snapshot = context?.DialogueManager?.GetSnapshot() ?? default;
        if (snapshot.HasActiveDialogue)
        {
            data.activeDialogueId = snapshot.DialogueId;
            data.activeNodeId = snapshot.NodeId;
            data.activeWaitKind = snapshot.WaitKind;
        }

        return data;
    }

    private void TryResumeActiveDialogue(GameContext context)
    {
        if (context?.DialogueManager == null ||
            string.IsNullOrWhiteSpace(runtimeData.activeDialogueId) ||
            string.IsNullOrWhiteSpace(runtimeData.activeNodeId))
            return;

        DialogueSO dialogue = DialogueRegistry.Find(runtimeData.activeDialogueId);
        if (dialogue == null)
        {
            Debug.LogWarning(
                $"DialogueProgressService: dialogue '{runtimeData.activeDialogueId}' was not found for resume.");
            return;
        }

        context.DialogueManager.ResumeFromSave(dialogue, runtimeData.activeNodeId, context);
    }

    public void RestoreNpcOnceStates()
    {
        var npcs = FindObjectsByType<NPCInteractable>(FindObjectsInactive.Include);
        foreach (NPCInteractable npc in npcs)
        {
            if (interactedNpcIds.Contains(npc.InteractableId))
                npc.RestoreInteracted(true);
        }
    }

    private void OnDialogueNodeEntered(string nodeId)
    {
        _ = nodeId;
    }

    private void OnDialogueChoice(DialogueChoiceResult result)
    {
        if (string.IsNullOrWhiteSpace(result.DialogueId))
            return;

        choiceRecords.Add(new DialogueChoiceRecord
        {
            dialogueId = result.DialogueId,
            nodeId = result.NodeId,
            choiceIndex = result.ChoiceIndex
        });
        runtimeData.choices = choiceRecords.ToArray();
    }

    private void OnDialogueEnded(DialogueSO dialogue)
    {
        if (dialogue == null || string.IsNullOrWhiteSpace(dialogue.dialogueId))
            return;

        completedDialogueIds.Add(dialogue.dialogueId);
        runtimeData.completedDialogueIds = completedDialogueIds.ToArray();
        runtimeData.activeDialogueId = null;
        runtimeData.activeNodeId = null;
        runtimeData.activeWaitKind = null;
    }
}
