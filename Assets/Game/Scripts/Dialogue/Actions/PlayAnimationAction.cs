using System;
using UnityEngine;

[Serializable]
public class PlayAnimationAction : IEventAction
{
    public string targetId;
    public string animationState;

    public void Execute(EventContext context)
    {
        if (string.IsNullOrWhiteSpace(targetId))
            return;

        DoorInteractable[] doors = UnityEngine.Object.FindObjectsByType<DoorInteractable>();
        foreach (DoorInteractable door in doors)
        {
            if (door.DoorId != targetId)
                continue;

            door.Open();
            return;
        }

        Debug.LogWarning($"PlayAnimationAction: target '{targetId}' was not found.");
    }
}
