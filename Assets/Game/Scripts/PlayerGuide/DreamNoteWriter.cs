/// <summary>
/// Frame 10 梦笺正文。按玩家写下的梦 + 本局四张抽牌生成可读笺文。
/// </summary>
public static class DreamNoteWriter
{
    public static string WaitingLine()
    {
        return "正在生成梦笺…";
    }

    public static string Compose(string dream, TarotResultData run)
    {
        string cards = run != null && run.IsComplete()
            ? run.SummarizeDrawn()
            : "尚未翻开的牌";
        return Compose(dream, cards);
    }

    public static string Compose(string dream, string cards)
    {
        return "这次梦笺暂时没能生成。你可以点击下方「重试生成」，再次打开这场梦。\n\n"
            + (string.IsNullOrWhiteSpace(dream)
                ? "你还没有留下梦境文字。"
                : "你留下的原梦：\n\n" + dream);
    }
}
