using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class SpikeTrap : MonoBehaviour
{
    [SerializeField] private int zoneIndex = -1;

    public void ConfigureZone(int zone)
    {
        zoneIndex = zone;
    }

    private void Reset()
    {
        Collider2D trapCollider = GetComponent<Collider2D>();
        trapCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryKillPlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryKillPlayer(other);
    }

    private void TryKillPlayer(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
            return;

        if (TarotZoneQuery.HasEffect(zoneIndex, TarotEffectId.E02))
            return;

        player.TryKill();
    }
}
