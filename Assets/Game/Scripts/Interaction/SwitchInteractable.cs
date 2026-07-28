using UnityEngine;

public class SwitchInteractable : InteractableBase
{
    [SerializeField] private string[] targetIds;
    [SerializeField] private bool once;
    [SerializeField] private bool startActivated;

    private bool activated;

    public bool IsActivated => activated;

    protected override void Awake()
    {
        base.Awake();
        activated = startActivated;
    }

    public override bool CanInteract => !once || !activated;

    public override void Interact(GameContext context)
    {
        activated = !activated;

        if (targetIds == null)
            return;

        foreach (string targetId in targetIds)
        {
            if (string.IsNullOrWhiteSpace(targetId))
                continue;

            EventCenter.Publish(GameEvents.MechanismActivated, targetId);
        }
    }

    public void ConfigureTargets(string[] ids, bool interactOnce, bool initialActivated)
    {
        targetIds = ids;
        once = interactOnce;
        startActivated = initialActivated;
        activated = initialActivated;
    }
}
