using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class LoadSceneEventAction : IDeferredEventAction
{
    public string sceneName;
    public bool resumeDialogueAfterLoad = true;

    public bool DeferDialogueAdvance => true;

    public void Execute(EventContext context)
    {
        if (context?.Game?.GameLoop == null || string.IsNullOrWhiteSpace(sceneName))
            return;

        context.Game.GameLoop.LoadSceneForDialogue(
            sceneName,
            context.PendingNextNode,
            resumeDialogueAfterLoad);
    }
}
