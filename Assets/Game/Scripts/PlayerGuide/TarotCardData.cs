using UnityEngine;

[CreateAssetMenu(fileName = "TarotCard", menuName = "Game/PlayerGuide/Tarot Card")]
public class TarotCardData : ScriptableObject
{
    [SerializeField] private string cardId;
    [SerializeField] private string displayName;
    [SerializeField] private string tarotName;
    [SerializeField] private Sprite cardFace;
    [SerializeField] private bool isMainCard;

    public string CardId => string.IsNullOrWhiteSpace(cardId) ? name : cardId;
    public string DisplayName => displayName;
    public string TarotName => tarotName;
    public Sprite CardFace => cardFace;
    public bool IsMainCard => isMainCard;
}
