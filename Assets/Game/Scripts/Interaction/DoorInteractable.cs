using UnityEngine;

public class DoorInteractable : MonoBehaviour
{
    [SerializeField] private string doorId;
    [SerializeField] private bool startOpen;
    [SerializeField] private Collider2D blockingCollider;
    [SerializeField] private Animator animator;
    [SerializeField] private string openAnimationState = "Door_Open";

    private bool isOpen;

    public string DoorId => string.IsNullOrWhiteSpace(doorId) ? name : doorId;
    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (blockingCollider == null)
            blockingCollider = GetComponent<Collider2D>();

        ApplyState(startOpen, playAnimation: false);
    }

    private void OnEnable()
    {
        EventCenter.Subscribe(GameEvents.MechanismActivated, OnMechanismActivated);
        EventCenter.Subscribe(GameEvents.OpenDoor, OnOpenDoorRequested);
    }

    private void OnDisable()
    {
        EventCenter.Unsubscribe(GameEvents.MechanismActivated, OnMechanismActivated);
        EventCenter.Unsubscribe(GameEvents.OpenDoor, OnOpenDoorRequested);
    }

    public void Configure(string id, bool defaultOpen, string animationState)
    {
        doorId = id;
        startOpen = defaultOpen;
        openAnimationState = animationState;
        ApplyState(startOpen, playAnimation: false);
    }

    private void OnMechanismActivated(string targetId)
    {
        if (targetId != DoorId)
            return;

        Open();
    }

    private void OnOpenDoorRequested(string targetId)
    {
        if (targetId != DoorId)
            return;

        Open();
    }

    public void Open()
    {
        ApplyState(true, playAnimation: true);
    }

    public void Close()
    {
        ApplyState(false, playAnimation: false);
    }

    private void ApplyState(bool open, bool playAnimation)
    {
        isOpen = open;

        if (blockingCollider != null)
            blockingCollider.enabled = !open;

        if (!playAnimation || animator == null || string.IsNullOrWhiteSpace(openAnimationState))
            return;

        animator.Play(openAnimationState);
    }
}
