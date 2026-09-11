using UnityEngine;

/// <summary>
/// 对应 T 点解锁的隐藏路 / TH 平台。默认关闭，hiddenPathsUnlocked[n] 为 true 后开启。
/// </summary>
public sealed class HiddenPathController : MonoBehaviour
{
    [SerializeField, Range(0, 3)] private int slotIndex;
    [SerializeField] private GameObject[] targets;
    private bool loggedEnter;

    private void OnEnable()
    {
        EventCenter.Subscribe(GameEvents.TarotNodeActivated, OnNodeActivated);
        Apply(GameLoop.Instance != null ? GameLoop.Instance.Context?.TarotResult : null);
    }

    private void OnDisable()
    {
        EventCenter.Unsubscribe(GameEvents.TarotNodeActivated, OnNodeActivated);
    }

    private void OnNodeActivated(int slot)
    {
        if (slot != slotIndex)
            return;

        Apply(GameLoop.Instance != null ? GameLoop.Instance.Context?.TarotResult : null);
    }

    private void Apply(TarotResultData run)
    {
        bool unlocked = run != null && run.IsHiddenPathUnlocked(slotIndex);
        if (targets == null || targets.Length == 0)
            return;

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
                targets[i].SetActive(unlocked);
        }
    }

    public void Configure(int slot, GameObject[] pathTargets)
    {
        slotIndex = Mathf.Clamp(slot, 0, 3);
        targets = pathTargets;
        loggedEnter = false;
        Apply(GameLoop.Instance != null ? GameLoop.Instance.Context?.TarotResult : null);
    }

    private void Update()
    {
        if (loggedEnter || targets == null)
            return;

        TarotResultData run = GameLoop.Instance != null ? GameLoop.Instance.Context?.TarotResult : null;
        if (run == null || !run.IsHiddenPathUnlocked(slotIndex))
            return;

        PlayerController player = GameLoop.Instance.Context?.Player;
        if (player == null)
            return;

        Vector3 pos = player.transform.position;
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null || !targets[i].activeInHierarchy)
                continue;
            if ((targets[i].transform.position - pos).sqrMagnitude > 2.6f * 2.6f)
                continue;

            loggedEnter = true;
            GameLoop.Instance.Context.DreamRun?.LogOnce(
                "hidden-" + slotIndex,
                "hidden_route_enter",
                "走进一条原先看不见的路");
            return;
        }
    }
}
