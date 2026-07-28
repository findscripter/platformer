using System;
using UnityEngine;

[Serializable]
public class OpenDoorAction : IEventAction
{
    public string doorId;

    public void Execute(EventContext context)
    {
        if (string.IsNullOrWhiteSpace(doorId))
            return;

        EventCenter.Publish(GameEvents.OpenDoor, doorId);
    }
}
