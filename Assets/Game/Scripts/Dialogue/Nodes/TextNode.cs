using System;
using UnityEngine;

[Serializable]
public class TextNode : DialogueNode
{
    public string speaker;
    public string text;
    [SerializeReference] public DialogueNode nextNode;

    public override DialogueNodeStep Enter(DialogueContext context)
    {
        context.Manager.PresentText(speaker, text);
        return DialogueNodeStep.Waiting;
    }
}
