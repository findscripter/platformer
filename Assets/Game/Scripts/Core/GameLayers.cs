using UnityEngine;

/// <summary>
/// Project-wide physics and sorting layer names from the scene layer scheme.
/// See Assets/docs/最终场景配置方案.md section 4.
/// </summary>
public static class GameLayers
{
    public const string BackgroundSorting = "Background";
    public const string MidgroundSorting = "Midground";
    public const string InteractiveDownSorting = "InteractiveDown";
    public const string PlayerSorting = "Player";
    public const string InteractiveUpSorting = "InteractiveUp";
    public const string InteractiveSorting = InteractiveUpSorting;
    public const string ForegroundSorting = "Foreground";
    public const string UiSorting = "UI";

    public const string Ground = "Ground";
    public const string Platform = "Platform";
    public const string Player = "Player";
    public const string Enemy = "Enemy";
    public const string Trigger = "Trigger";
    public const string Collectible = "Collectible";
    public const string Mechanism = "Mechanism";

    public static int GroundLayer => LayerMask.NameToLayer(Ground);
    public static int PlatformLayer => LayerMask.NameToLayer(Platform);
    public static int PlayerLayer => LayerMask.NameToLayer(Player);
    public static int EnemyLayer => LayerMask.NameToLayer(Enemy);
    public static int TriggerLayer => LayerMask.NameToLayer(Trigger);
    public static int CollectibleLayer => LayerMask.NameToLayer(Collectible);
    public static int MechanismLayer => LayerMask.NameToLayer(Mechanism);

    public static LayerMask GroundCheckMask => LayerMask.GetMask(Ground, Platform);

    public static int GetSortingLayerId(string sortingLayerName)
    {
        return SortingLayer.NameToID(sortingLayerName);
    }
}
