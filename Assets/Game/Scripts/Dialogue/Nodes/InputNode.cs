using System;
using UnityEngine;

[Serializable]
public class InputNode : DialogueNode
{
    public string question;
    public string expectedAnswer;
    [SerializeReference] public DialogueNode successNode;
    [SerializeReference] public DialogueNode failNode;

    public override DialogueNodeStep Enter(DialogueContext context)
    {
        context.Manager.PresentInput(question);
        return DialogueNodeStep.Waiting;
    }
}
