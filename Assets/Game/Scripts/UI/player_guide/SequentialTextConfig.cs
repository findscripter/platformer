using UnityEngine;

[CreateAssetMenu(fileName = "SequentialTextConfig", menuName = "Game/UI/Sequential Text Config")]
public class SequentialTextConfig : ScriptableObject
{
    [TextArea(2, 6)]
    public string[] lines;
}
