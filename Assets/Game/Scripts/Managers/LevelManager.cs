using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private Transform defaultSpawnPoint;
    [SerializeField] private Collider2D goalTrigger;
    [SerializeField] private float deathY = -10f;

    private PlayerController cachedPlayer;
    private Collider2D cachedPlayerCollider;

    public Transform DefaultSpawnPoint => defaultSpawnPoint;

    public void SetGameplayReferences(Transform spawnPoint, Collider2D goal)
    {
        defaultSpawnPoint = spawnPoint;
        goalTrigger = goal;
    }

    public void LoadLevel()
    {
        Debug.Log("Level Loaded");
    }

    public void ResetLevel()
    {
        Debug.Log("Level Reset");
    }

    public bool CheckPlayerDead(PlayerController player)
    {
        if (player == null)
            return false;

        if (player.transform.position.y < deathY)
            return true;

        if (player.IsDead)
            return true;

        return false;
    }

    public bool CheckLevelClear(PlayerController player)
    {
        if (player == null || goalTrigger == null)
            return false;

        if (cachedPlayer != player)
        {
            cachedPlayer = player;
            cachedPlayerCollider = player.GetComponent<Collider2D>();
        }

        if (cachedPlayerCollider != null)
        {
            return goalTrigger.IsTouching(cachedPlayerCollider);
        }

        return goalTrigger.bounds.Contains(player.transform.position);
    }
}
