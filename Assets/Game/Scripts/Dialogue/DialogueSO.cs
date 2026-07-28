using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Dialogue", menuName = "Game/Dialogue")]
public class DialogueSO : ScriptableObject
{
    public string dialogueId;
    [SerializeReference] public DialogueNode startNode;

    public DialogueNode FindNodeById(string nodeId)
    {
        if (startNode == null || string.IsNullOrWhiteSpace(nodeId))
            return null;

        var visited = new HashSet<DialogueNode>();
        var queue = new Queue<DialogueNode>();
        queue.Enqueue(startNode);

        while (queue.Count > 0)
        {
            DialogueNode node = queue.Dequeue();
            if (node == null || !visited.Add(node))
                continue;

            if (string.Equals(node.nodeId, nodeId, StringComparison.Ordinal))
                return node;

            foreach (DialogueNode nextNode in EnumerateNextNodes(node))
                queue.Enqueue(nextNode);
        }

        return null;
    }

    private static IEnumerable<DialogueNode> EnumerateNextNodes(DialogueNode node)
    {
        switch (node)
        {
            case TextNode textNode when textNode.nextNode != null:
                yield return textNode.nextNode;
                break;
            case ChoiceNode choiceNode when choiceNode.options != null:
                foreach (ChoiceOption option in choiceNode.options)
                {
                    if (option?.nextNode != null)
                        yield return option.nextNode;
                }
                break;
            case InputNode inputNode:
                if (inputNode.successNode != null)
                    yield return inputNode.successNode;
                if (inputNode.failNode != null)
                    yield return inputNode.failNode;
                break;
            case EventNode eventNode when eventNode.nextNode != null:
                yield return eventNode.nextNode;
                break;
        }
    }
}
