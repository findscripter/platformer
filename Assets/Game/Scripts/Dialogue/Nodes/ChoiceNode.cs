using System;
using UnityEngine;

[Serializable]
public class ChoiceOption
{
    public string text;
    [SerializeReference] public DialogueNode nextNode;
}

[Serializable]
public class ChoiceNode : DialogueNode
{
    public ChoiceOption[] options;

    public override DialogueNodeStep Enter(DialogueContext context)
    {
        context.Manager.PresentChoices(options);
        return DialogueNodeStep.Waiting;
    }
}
