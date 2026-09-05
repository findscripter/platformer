using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class AirWalkVolume : MonoBehaviour
{
    [SerializeField] private int zoneIndex = -1;

    private PlayerController occupant;
    private bool locked;

    public void Configure(int zone)
    {
        zoneIndex = zone;
    }

    private void Update()
    {
        bool should = occupant != null &&
            (TarotZoneQuery.HasEffect(zoneIndex, TarotEffectId.E01) || HasLiveE01());
        if (should && !locked)
        {
            occupant.AddAirWalkLock();
            locked = true;
        }
        else if (!should && locked)
        {
            occupant.RemoveAirWalkLock();
            locked = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null)
            occupant = player;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null || player != occupant)
            return;
        if (locked)
            player.RemoveAirWalkLock();
        locked = false;
        occupant = null;
    }

    private static bool HasLiveE01()
    {
        TarotResultData run = GameLoop.Instance != null ? GameLoop.Instance.Context?.TarotResult : null;
        return run != null && run.HasActiveEffect(TarotEffectId.E01);
    }
}
