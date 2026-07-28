using System;

[Serializable]
public abstract class DialogueNode
{
    public string nodeId;

    public abstract DialogueNodeStep Enter(DialogueContext context);
}
