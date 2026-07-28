using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EventNode : DialogueNode
{
    [SerializeReference] public List<IEventAction> actions = new();
    [SerializeReference] public DialogueNode nextNode;

    public override DialogueNodeStep Enter(DialogueContext context)
    {
        var eventContext = new EventContext
        {
            Game = context.Game,
            Dialogue = context,
            SourceNodeId = nodeId,
            PendingNextNode = nextNode
        };

        var deferAdvance = false;
        if (actions != null)
        {
            foreach (IEventAction action in actions)
            {
                if (action == null)
                    continue;

                action.Execute(eventContext);
                if (action is IDeferredEventAction deferred && deferred.DeferDialogueAdvance)
                    deferAdvance = true;
            }
        }

        if (!deferAdvance)
            context.Manager.AdvanceTo(nextNode);

        return DialogueNodeStep.AutoAdvance;
    }
}
