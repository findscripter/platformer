using System.Text.RegularExpressions;

/// <summary>
/// Frame 8 状态 A → B 的判定：玩家写下的梦境是否需要腓腓补问一次。
/// 意象或情绪线索不足时返回 true。
/// </summary>
public static class DreamInputValidator
{
    private const int MinMeaningfulLength = 5;

    private static readonly string[] VagueKeywords =
    {
        "不知道", "不清楚", "没想好", "想不起", "记不清", "忘了", "忘记了",
        "随便", "随意", "都行", "无所谓", "没什么", "算了", "懒得", "不想说"
    };

    private static readonly Regex SymbolsOnlyPattern = new(@"^[\d\s\p{P}\p{S}]+$", RegexOptions.Compiled);
    private static readonly Regex RepeatedCharPattern = new(@"^(.)\1*$", RegexOptions.Compiled);

    public static bool NeedsFollowUp(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return true;

        string trimmed = input.Trim();

        if (CountMeaningfulChars(trimmed) < MinMeaningfulLength)
            return true;

        if (SymbolsOnlyPattern.IsMatch(trimmed))
            return true;

        if (RepeatedCharPattern.IsMatch(trimmed))
            return true;

        foreach (string keyword in VagueKeywords)
        {
            if (trimmed.Contains(keyword))
                return true;
        }

        return false;
    }

    private static int CountMeaningfulChars(string value)
    {
        int count = 0;
        foreach (char c in value)
        {
            if (!char.IsWhiteSpace(c) && !char.IsPunctuation(c) && !char.IsSymbol(c))
                count++;
        }

        return count;
    }
}
