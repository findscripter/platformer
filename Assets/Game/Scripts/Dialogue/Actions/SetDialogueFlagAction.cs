using System;
using UnityEngine;

[Serializable]
public class SetDialogueFlagAction : IEventAction
{
    public string flagId;

    public void Execute(EventContext context)
    {
        if (string.IsNullOrWhiteSpace(flagId))
            return;

        context?.Game?.DialogueProgressService?.SetFlag(flagId);
    }
}
