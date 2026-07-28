using System;
using UnityEngine;

[Serializable]
public class GiveItemAction : IEventAction
{
    public string itemId;
    public int count = 1;

    public void Execute(EventContext context)
    {
        Debug.Log($"Give item requested: {itemId} x{count}. InventoryManager is not implemented yet.");
    }
}
