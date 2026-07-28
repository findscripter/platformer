using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerGuideBubbleLayoutConfig",
    menuName = "Game/Player Guide/Bubble Layout Config")]
public class PlayerGuideBubbleLayoutConfig : ScriptableObject
{
    [Header("数量")]
    [Min(0)] public int mediumCount = 3;
    [Min(0)] public int smallCount = 3;

    [Header("布局")]
    [Tooltip("中泡泡轨道最小半径（内层椭圆）")]
    public float mediumMinOrbitRadius = 200f;

    [Tooltip("中泡泡轨道最大半径（内层椭圆）")]
    public float mediumMaxOrbitRadius = 360f;

    [Tooltip("小泡泡轨道最小半径（外层椭圆）")]
    public float smallMinOrbitRadius = 380f;

    [Tooltip("小泡泡轨道最大半径（外层椭圆）")]
    public float smallMaxOrbitRadius = 520f;

    [Tooltip("泡泡之间的最小间距")]
    public float bubblePadding = 16f;

    [Tooltip("距离 bubbles_node 边缘的内边距")]
    public float boundsPadding = 24f;

    [Tooltip("每个泡泡的最大放置尝试次数")]
    [Min(1)] public int placementAttemptsPerBubble = 96;

    [Header("随机")]
    [Tooltip("0 表示每次生成使用随机种子")]
    public int randomSeed;

    [Header("节点名称")]
    public string bubblesNodeName = "bubbles_node";
    public string mainBubbleName = "bubbles_main";
    public string mediumBubbleNamePrefix = "bubbles_other_1";
    public string smallBubbleNamePrefix = "bubbles_other_2";

    [Header("尺寸（用于碰撞检测，默认与场景一致）")]
    public Vector2 mainBubbleSize = new(280f, 274f);
    public Vector2 mediumBubbleSize = new(147f, 146f);
    public Vector2 smallBubbleSize = new(65f, 65f);

    public int TotalOtherBubbleCount => mediumCount + smallCount;
}
