using UnityEngine;

public class SceneTransitionInteractable : InteractableBase
{
    [SerializeField] private string targetScene;
    [SerializeField] private string targetSpawnId;

    public override void Interact(GameContext context)
    {
        if (string.IsNullOrWhiteSpace(targetScene))
        {
            Debug.LogWarning($"{name}: Scene transition target is not configured.");
            return;
        }

        Debug.Log(
            $"Scene transition requested: {targetScene} (spawn={targetSpawnId}). " +
            "SceneTransitionManager is not implemented yet.");
    }

    public void Configure(string sceneId, string spawnId, string prompt)
    {
        targetScene = sceneId;
        targetSpawnId = spawnId;
        Configure(sceneId, prompt, 1.5f);
    }
}
