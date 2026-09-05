using UnityEngine;

/// <summary>
/// Sheet1 22 张牌与正逆位 E 编号。运行时以本表为准，不依赖 fixedCards。
/// </summary>
public static class TarotCatalog
{
    public readonly struct Entry
    {
        public readonly string CardId;
        public readonly string DisplayName;
        public readonly string TarotName;
        public readonly TarotEffectId Upright;
        public readonly TarotEffectId Reversed;

        public Entry(string cardId, string displayName, string tarotName, TarotEffectId upright, TarotEffectId reversed)
        {
            CardId = cardId;
            DisplayName = displayName;
            TarotName = tarotName;
            Upright = upright;
            Reversed = reversed;
        }
    }

    public static readonly Entry[] All =
    {
        new("Card_Fool", "夸父逐日", "愚者", TarotEffectId.E03, TarotEffectId.E13),
        new("Card_Magician", "巫咸", "魔术师", TarotEffectId.E04, TarotEffectId.E16),
        new("Card_HighPriestess", "西王母", "女祭司", TarotEffectId.E02, TarotEffectId.E14),
        new("Card_Empress", "女娲", "女皇", TarotEffectId.E07, TarotEffectId.E18),
        new("Card_Emperor", "轩辕", "皇帝", TarotEffectId.E12, TarotEffectId.E15),
        new("Card_Hierophant", "伯益", "教皇", TarotEffectId.E08, TarotEffectId.E00),
        new("Card_Lovers", "伏羲女娲", "恋人", TarotEffectId.E09, TarotEffectId.E13),
        new("Card_YingLong", "应龙", "战车", TarotEffectId.E01, TarotEffectId.E15),
        new("Card_Strength", "九尾狐", "力量", TarotEffectId.E05, TarotEffectId.E17),
        new("Card_Hermit", "广成子", "隐士", TarotEffectId.E06, TarotEffectId.E00),
        new("Card_RiYueLunZhuan", "日月轮转", "命运之轮", TarotEffectId.E09, TarotEffectId.E18),
        new("Card_Justice", "皋陶", "正义", TarotEffectId.E10, TarotEffectId.E13),
        new("Card_HangedMan", "鲧", "倒吊人", TarotEffectId.E12, TarotEffectId.E18),
        new("Card_Death", "玄冥", "死神", TarotEffectId.E11, TarotEffectId.E12),
        new("Card_Temperance", "句芒", "节制", TarotEffectId.E02, TarotEffectId.E14),
        new("Card_Devil", "饕餮", "恶魔", TarotEffectId.E16, TarotEffectId.E04),
        new("Card_Tower", "共工触山", "高塔", TarotEffectId.E15, TarotEffectId.E12),
        new("Card_ChangXi", "常羲沐月", "星星", TarotEffectId.E01, TarotEffectId.E18),
        new("Card_ChanChuJingPo", "蟾蜍精魄", "月亮", TarotEffectId.E13, TarotEffectId.E06),
        new("Card_Sun", "羲和", "太阳", TarotEffectId.E07, TarotEffectId.E00),
        new("Card_Judgement", "后土", "审判", TarotEffectId.E09, TarotEffectId.E17),
        new("Card_World", "盘古", "世界", TarotEffectId.E06, TarotEffectId.E00)
    };

    public const string ResourcesFolder = "PlayerGuide/TarotCards";

    public static bool TryGet(string cardId, out Entry entry)
    {
        if (!string.IsNullOrEmpty(cardId))
        {
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].CardId == cardId)
                {
                    entry = All[i];
                    return true;
                }
            }
        }

        entry = default;
        return false;
    }

    public static TarotEffectId GetEffect(TarotCardData card, bool reversed)
    {
        if (card != null && TryGet(card.CardId, out Entry entry))
            return reversed ? entry.Reversed : entry.Upright;

        return TarotEffectId.E00;
    }

    public static string GetDisplayName(TarotCardData card)
    {
        if (card != null && TryGet(card.CardId, out Entry entry))
            return entry.DisplayName;

        return card != null ? card.DisplayName : string.Empty;
    }

    public static TarotCardData[] LoadDeck()
    {
        TarotCardData[] loaded = Resources.LoadAll<TarotCardData>(ResourcesFolder);
        if (loaded == null || loaded.Length == 0)
            return System.Array.Empty<TarotCardData>();

        var ordered = new TarotCardData[All.Length];
        int count = 0;
        for (int i = 0; i < All.Length; i++)
        {
            TarotCardData match = null;
            for (int j = 0; j < loaded.Length; j++)
            {
                if (loaded[j] != null && loaded[j].CardId == All[i].CardId)
                {
                    match = loaded[j];
                    break;
                }
            }

            if (match != null)
                ordered[count++] = match;
        }

        if (count == All.Length)
            return ordered;

        var compact = new TarotCardData[count];
        System.Array.Copy(ordered, compact, count);
        return compact;
    }
}
