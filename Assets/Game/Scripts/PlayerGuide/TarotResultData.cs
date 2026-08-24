using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Frame 9 抽牌结果：1 张主牌（决定地图主题）+ 3 张情绪牌（决定关卡挑战）。
/// 由 GameContext 持有，教学关读取后配置关卡。
/// </summary>
[System.Serializable]
public class TarotResultData
{
    [SerializeField] private TarotCardData mainCard;
    [SerializeField] private List<TarotCardData> emotionCards = new();

    public TarotCardData MainCard => mainCard;
    public IReadOnlyList<TarotCardData> EmotionCards => emotionCards;

    public TarotResultData()
    {
    }

    public TarotResultData(TarotCardData mainCard, IEnumerable<TarotCardData> emotionCards)
    {
        this.mainCard = mainCard;
        if (emotionCards != null)
            this.emotionCards.AddRange(emotionCards);
    }

    public string MainCardId => mainCard != null ? mainCard.CardId : string.Empty;

    public List<string> GetEmotionCardIds()
    {
        var ids = new List<string>(emotionCards.Count);
        foreach (var card in emotionCards)
        {
            if (card != null)
                ids.Add(card.CardId);
        }

        return ids;
    }

    /// <summary>
    /// 完整结果需要 1 张主牌 + 3 张情绪牌，且不含空引用。
    /// </summary>
    public bool IsComplete()
    {
        if (mainCard == null || emotionCards.Count != 3)
            return false;

        foreach (var card in emotionCards)
        {
            if (card == null)
                return false;
        }

        return true;
    }
}
