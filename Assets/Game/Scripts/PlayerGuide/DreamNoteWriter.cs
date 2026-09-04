/// <summary>
/// Frame 10 梦笺正文。没有联网 LLM，按玩家写下的梦 + 主牌生成可读笺文，
/// 并配合「正在倾听」等待态，对上「等待 AI 内容 → 阅读」。
/// </summary>
public static class DreamNoteWriter
{
    public static string WaitingLine()
    {
        return "梦核正把这场旅程轻轻收进笺里……";
    }

    public static string Compose(string dream, string mainCard)
    {
        string card = string.IsNullOrWhiteSpace(mainCard) ? "应龙" : mainCard.Trim();
        string memory = string.IsNullOrWhiteSpace(dream)
            ? "风声、脚步，和那些还没说完的话"
            : dream.Trim();

        if (memory.Length > 48)
            memory = memory.Substring(0, 48) + "…";

        return "梦核轻轻展开一页笺。\n"
            + card + " 把路收进光里。\n"
            + "你还记得：" + memory + "\n"
            + "打开看看吧。它已经替你收好了。";
    }
}
