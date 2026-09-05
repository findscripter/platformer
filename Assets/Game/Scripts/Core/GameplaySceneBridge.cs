using UnityEngine;

public class GameplaySceneBridge : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Collider2D goalTrigger;

    public void Apply(GameLoop gameLoop)
    {
        DreamremainsLevelBootstrap.Build(this);

        if (gameLoop == null)
            return;

        gameLoop.BindGameplay(player, spawnPoint, goalTrigger);
    }

    public void BindOfficial(PlayerController gameplayPlayer, Transform officialSpawn, Collider2D officialGoal)
    {
        if (gameplayPlayer != null)
            player = gameplayPlayer;
        if (officialSpawn != null)
            spawnPoint = officialSpawn;
        if (officialGoal != null)
            goalTrigger = officialGoal;
    }

    private void Reset()
    {
        player = FindPlayer();
        spawnPoint = FindTransform("SceneRoot/GameplayLayer/SpawnPoint");
        goalTrigger = FindCollider("SceneRoot/GameplayLayer/Runtime_Exits/GoalTrigger");
    }

    private PlayerController FindPlayer()
    {
        
        var testPlayer = GameObject.Find("test_player");
        if (testPlayer != null && testPlayer.activeInHierarchy)
            return testPlayer.GetComponent<PlayerController>();

        var playerObject = GameObject.Find("Runtime_Player");
        return playerObject != null ? playerObject.GetComponent<PlayerController>() : null;
    }

    private Transform FindTransform(string path)
    {
        var segments = path.Split('/');
        Transform current = null;

        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == segments[0])
            {
                current = root.transform;
                break;
            }
        }

        if (current == null)
        {
            return null;
        }

        for (var index = 1; index < segments.Length; index++)
        {
            current = current.Find(segments[index]);
            if (current == null)
            {
                return null;
            }
        }

        return current;
    }

    private Collider2D FindCollider(string path)
    {
        var transform = FindTransform(path);
        return transform != null ? transform.GetComponent<Collider2D>() : null;
    }
}
