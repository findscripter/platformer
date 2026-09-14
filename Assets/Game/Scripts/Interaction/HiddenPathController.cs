using UnityEngine;

/// <summary>
/// 对应 T 点解锁的隐藏路 / TH 平台。默认关闭，hiddenPathsUnlocked[n] 为 true 后开启。
/// </summary>
public sealed class HiddenPathController : MonoBehaviour
{
    [SerializeField, Range(0, 3)] private int slotIndex;
    [SerializeField] private GameObject[] targets;
    private TarotEffectId requiredEffect;
    private string mechanismId;
    private bool mechanismOpened;

    private void OnEnable()
    {
        EventCenter.Subscribe(GameEvents.TarotNodeActivated, OnNodeActivated);
        EventCenter.Subscribe(GameEvents.MechanismActivated, OnMechanism);
        Apply(GameLoop.Instance != null ? GameLoop.Instance.Context?.TarotResult : null);
    }

    private void OnDisable()
    {
        EventCenter.Unsubscribe(GameEvents.TarotNodeActivated, OnNodeActivated);
        EventCenter.Unsubscribe(GameEvents.MechanismActivated, OnMechanism);
    }

    private void OnNodeActivated(int slot)
    {
        if (requiredEffect==TarotEffectId.E00 && slot != slotIndex)
            return;

        Apply(GameLoop.Instance != null ? GameLoop.Instance.Context?.TarotResult : null);
    }

    private void Apply(TarotResultData run)
    {
        bool unlocked = !string.IsNullOrEmpty(mechanismId)?mechanismOpened:run != null && (requiredEffect==TarotEffectId.E00
            ?run.IsHiddenPathUnlocked(slotIndex):run.HasActiveEffect(requiredEffect));
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
        Apply(GameLoop.Instance != null ? GameLoop.Instance.Context?.TarotResult : null);
    }
    public void ConfigureEffect(TarotEffectId effect,GameObject[] pathTargets)
    {
        requiredEffect=effect;targets=pathTargets;
        Apply(GameLoop.Instance != null ? GameLoop.Instance.Context?.TarotResult : null);
    }
    public void ConfigureMechanism(string id,GameObject[] pathTargets)
    {
        mechanismId=id;targets=pathTargets;Apply(null);
    }
    private void OnMechanism(string id)
    {
        if(string.IsNullOrEmpty(mechanismId)||id!=mechanismId)return;
        mechanismOpened=true;Apply(null);
    }
}
