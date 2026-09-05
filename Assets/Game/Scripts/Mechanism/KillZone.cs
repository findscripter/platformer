using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class KillZone : MonoBehaviour
{
    private void Reset()
    {
        Collider2D box = GetComponent<Collider2D>();
        box.isTrigger = true;
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
            renderer.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null)
            player.TryKill();
    }
}
