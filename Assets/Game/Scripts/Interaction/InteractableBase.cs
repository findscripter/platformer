using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactableId;
    [SerializeField] private string interactPrompt = "交互";
    [SerializeField, Min(0.1f)] private float interactRadius = 1.5f;

    private CircleCollider2D triggerCollider;

    public Transform Transform => transform;
    public virtual string InteractableId => string.IsNullOrWhiteSpace(interactableId) ? name : interactableId;
    public virtual string InteractPrompt => interactPrompt;
    public virtual bool CanInteract => true;

    public abstract void Interact(GameContext context);

    protected virtual void Awake()
    {
        triggerCollider = GetComponent<CircleCollider2D>();
        triggerCollider.isTrigger = true;
        triggerCollider.radius = interactRadius;
    }

    protected virtual void Reset()
    {
        var collider = GetComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = interactRadius;
    }

    public void Configure(string id, string prompt, float radius)
    {
        interactableId = id;
        interactPrompt = prompt;
        interactRadius = radius;

        if (triggerCollider == null)
            triggerCollider = GetComponent<CircleCollider2D>();

        if (triggerCollider != null)
            triggerCollider.radius = interactRadius;
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() == null)
            return;

        InteractionManager.Instance?.Register(this);
    }

    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() == null)
            return;

        InteractionManager.Instance?.Unregister(this);
    }
}
