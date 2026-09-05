using UnityEngine;

/// <summary>
/// TRG 独立触发器。走进即发 MechanismActivated(id)，不解读塔罗、不解锁隐藏路。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public sealed class MechanismTrigger : MonoBehaviour
{
    [SerializeField] private string triggerId;
    [SerializeField] private bool once = true;

    private bool fired;

    public string TriggerId => string.IsNullOrWhiteSpace(triggerId) ? name : triggerId;

    private void Reset()
    {
        Collider2D box = GetComponent<Collider2D>();
        box.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (fired && once)
            return;
        if (other.GetComponentInParent<PlayerController>() == null)
            return;

        fired = true;
        EventCenter.Publish(GameEvents.MechanismActivated, TriggerId);
    }

    public void Configure(string id, bool fireOnce)
    {
        triggerId = id;
        once = fireOnce;
    }
}
