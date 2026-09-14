using System;
using UnityEngine;

public sealed class CollectibleTracker : MonoBehaviour
{
    public static CollectibleTracker Active { get; private set; }

    public int CurrentCount { get; private set; }
    public int TotalValue { get; private set; }
    public int AreaASpawned { get; private set; }
    public int AreaACollected { get; private set; }
    public int AreaBSpawned { get; private set; }
    public int AreaBCollected { get; private set; }

    public bool AreaAComplete => AreaASpawned <= 0 || AreaACollected >= AreaASpawned;
    public int AreaARemaining => Mathf.Max(0, AreaASpawned - AreaACollected);

    public event Action<int, int> CountChanged;

    private void Awake()
    {
        if (Active == null)
            Active = this;
    }

    private void OnDestroy()
    {
        if (Active == this)
            Active = null;
    }

    public static CollectibleTracker Ensure(Transform host = null)
    {
        if (Active != null)
            return Active;

        CollectibleTracker found = FindAnyObjectByType<CollectibleTracker>();
        if (found != null)
        {
            Active = found;
            return found;
        }

        Transform parent = host;
        var go = new GameObject("CollectibleTracker");
        if (parent != null)
            go.transform.SetParent(parent, false);
        Active = go.AddComponent<CollectibleTracker>();
        return Active;
    }

    public void ResetRun()
    {
        CurrentCount = 0;
        TotalValue = 0;
        AreaASpawned = 0;
        AreaACollected = 0;
        AreaBSpawned = 0;
        AreaBCollected = 0;
        CountChanged?.Invoke(0, 0);
    }

    public void Register(CollectibleItem item, bool sceneA)
    {
        if (item == null || item.IsCollected)
            return;

        if (Active != null && Active != this)
        {
            Active.Register(item, sceneA);
            return;
        }

        if (sceneA)
            AreaASpawned++;
        else
            AreaBSpawned++;
        CountChanged?.Invoke(CurrentCount, TotalValue);
    }

    public void Collect(CollectibleItem item, int value)
    {
        if (item == null || item.IsCollected)
            return;

        if (Active != null && Active != this)
        {
            Active.Collect(item, value);
            return;
        }

        item.MarkCollected();
        GameLoop.Instance?.Context?.TarotResult?.RecordFragment(item.name);
        CurrentCount++;
        TotalValue += Mathf.Max(0, value);
        if (item.BelongsToSceneA)
            AreaACollected++;
        else
            AreaBCollected++;
        CountChanged?.Invoke(CurrentCount, TotalValue);
        GameLoop.Instance?.Context?.DreamRun?.Log("collect", "拾取梦核碎片");
    }

    public void NotifyRegionChanged()
    {
        CountChanged?.Invoke(CurrentCount, TotalValue);
    }
}
