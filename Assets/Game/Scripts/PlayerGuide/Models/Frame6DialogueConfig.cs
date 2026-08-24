using UnityEngine;

/// <summary>
/// Frame 6（6-1 至 6-8）的 8 段对话配置。
/// 每段包含文本、建议时长、角色动画提示。
/// </summary>
[CreateAssetMenu(fileName = "Frame6DialogueConfig", menuName = "Game/PlayerGuide/Frame 6 Dialogue Config")]
public class Frame6DialogueConfig : ScriptableObject
{
    [System.Serializable]
    public class DialogueSegment
    {
        [Header("对话内容")]
        [TextArea(2, 4)]
        public string text;

        [Header("时长（秒）")]
        [Tooltip("建议停留时长，用于自动播放模式")]
        public float suggestedDuration = 1.5f;

        [Header("动画提示（可选）")]
        [Tooltip("腓腓的动画状态名称，如 HoldCore、LookDown 等")]
        public string feifeiAnimation;

        [Tooltip("梦核的光效强度（0-1），0 表示不改变")]
        [Range(0f, 1f)]
        public float coreGlowIntensity;

        [Tooltip("是否播放梦核涟漪效果")]
        public bool playCoreRipple;
    }

    [SerializeField] private DialogueSegment[] segments = new DialogueSegment[8];

    public DialogueSegment[] Segments => segments;

    public int SegmentCount => segments?.Length ?? 0;

    public DialogueSegment GetSegment(int index)
    {
        if (segments == null || index < 0 || index >= segments.Length)
            return null;

        return segments[index];
    }

    private void OnValidate()
    {
        if (segments == null || segments.Length != 8)
        {
            System.Array.Resize(ref segments, 8);
        }
    }
}
