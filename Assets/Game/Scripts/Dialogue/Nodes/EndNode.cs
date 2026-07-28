using System;

[Serializable]
public class EndNode : DialogueNode
{
    public override DialogueNodeStep Enter(DialogueContext context)
    {
        context.Manager.EndDialogue();
        return DialogueNodeStep.End;
    }
}
