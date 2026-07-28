using System;

[Serializable]
public class DialogueChoiceRecord
{
    public string dialogueId;
    public string nodeId;
    public int choiceIndex;
}

[Serializable]
public class DialogueSaveData
{
    public string[] completedDialogueIds = Array.Empty<string>();
    public DialogueChoiceRecord[] choices = Array.Empty<DialogueChoiceRecord>();
    public string[] flags = Array.Empty<string>();
    public string[] interactedNpcIds = Array.Empty<string>();

    public string activeDialogueId;
    public string activeNodeId;
    public string activeWaitKind;
}

[Serializable]
public class GameSaveData
{
    public string currentScene;
    public string spawnPointId;
    public DialogueSaveData dialogue = new();
}
