using System.Collections.Generic;
using UnityEngine;

public static class DialogueRegistry
{
    private static Dictionary<string, DialogueSO> cache;

    public static DialogueSO Find(string dialogueId)
    {
        if (string.IsNullOrWhiteSpace(dialogueId))
            return null;

        EnsureCache();
        cache.TryGetValue(dialogueId, out DialogueSO dialogue);
        return dialogue;
    }

    public static void Reload()
    {
        cache = null;
        EnsureCache();
    }

    private static void EnsureCache()
    {
        if (cache != null)
            return;

        cache = new Dictionary<string, DialogueSO>();
        DialogueSO[] dialogues = Resources.LoadAll<DialogueSO>("Dialogue");
        foreach (DialogueSO dialogue in dialogues)
        {
            if (dialogue == null || string.IsNullOrWhiteSpace(dialogue.dialogueId))
                continue;

            cache[dialogue.dialogueId] = dialogue;
        }
    }
}
