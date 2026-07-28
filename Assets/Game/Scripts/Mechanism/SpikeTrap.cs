using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class SpikeTrap : MonoBehaviour
{
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

    private static void TryKillPlayer(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null)
        {
            player.TryKill();
        }
    }
}
