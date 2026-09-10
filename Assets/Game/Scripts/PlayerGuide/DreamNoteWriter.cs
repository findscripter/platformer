/// <summary>
/// Frame 10 梦笺正文。按玩家写下的梦 + 本局四张抽牌生成可读笺文。
/// </summary>
public static class DreamNoteWriter
{
    public static string WaitingLine()
    {
        return "梦核正把这场旅程轻轻收进笺里……";
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
        string cardLine = string.IsNullOrWhiteSpace(cards) ? "尚未翻开的牌" : cards.Trim();
        string memory = string.IsNullOrWhiteSpace(dream)
            ? "那场还没说完的梦"
            : dream.Trim();

        if (memory.Length > 48)
            memory = memory.Substring(0, 48) + "…";

        return "梦核轻轻展开一页笺。\n"
            + cardLine + "\n"
            + "你还记得：" + memory + "\n"
            + "打开看看吧。它已经替你收好了。";
    }
}
