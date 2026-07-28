using System;
using UnityEngine;

public sealed class CollectibleTracker : MonoBehaviour
{
    public int CurrentCount { get; private set; }
    public int TotalValue { get; private set; }

    public event Action<int, int> CountChanged;

    public void Collect(CollectibleItem item, int value)
    {
        if (item == null || item.IsCollected)
            return;

        item.MarkCollected();
        CurrentCount++;
        TotalValue += Mathf.Max(0, value);
        CountChanged?.Invoke(CurrentCount, TotalValue);
    }
}
