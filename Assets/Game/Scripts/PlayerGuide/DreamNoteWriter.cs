/// <summary>
/// Frame 10 梦笺正文。按玩家写下的梦 + 本局四张抽牌生成可读笺文。
/// </summary>
public static class DreamNoteWriter
{
    public static string WaitingLine()
    {
        return "梦核轻轻呼吸\n等待 AI 正文返回";
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

        if (memory.Length > 80)
            memory = memory.Substring(0, 80) + "…";

        return "梦核把这场旅程轻轻收进一页笺里。你还记得：" + memory
            + "。这一路上遇见的是" + cardLine
            + "。打开看看吧，它已经替你收好了。";
    }
}
