using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

/// <summary>A bounded local reading, using the actual four cards and only recorded actions.</summary>
public static class LocalDreamLetterComposer
{
    enum Theme { Open, Teeth, Chase, Arrival, Falling, Reunion, Choice }

    public static JObject ComposeDocument(GameContext context)
    {
        if (context?.TarotResult == null || !context.TarotResult.IsComplete()) return null;
        string dream = context.PlayerDreamInput ?? string.Empty;
        bool empty = DreamInputValidator.ShouldSuggestSample(dream);
        bool sample = Regex.Replace(dream, @"\s+", "") == Regex.Replace(DreamInputView.SampleDream, @"\s+", "");
        Theme theme = FindTheme(dream, empty, sample);
        bool sparse = dream.Count(c => !char.IsWhiteSpace(c) && !char.IsPunctuation(c)) < 8;
        string[] sentences = Regex.Split(dream, @"(?<=[。！？!?])|\r?\n").Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToArray();
        string excerpt = string.Join("", sentences);
        if (excerpt.Length > 160) excerpt = excerpt.Substring(0, 160) + "…";
        var paragraphs = new string[4];
        var readings = new JArray();
        var links = Enumerable.Range(0, 4).Select(_ => new List<string>()).ToArray();
        for (int i = 0; i < 4; i++)
        {
            TarotCardData card = context.TarotResult.GetCard(i);
            string name = TarotCatalog.GetDisplayName(card);
            bool reversed = context.TarotResult.IsReversed(i);
            string orientation = reversed ? "逆位" : "正位";
            TarotCatalog.TryGet(card.CardId, out TarotCatalog.Entry entry);
            string tarot = CanonicalTarotName(entry.TarotName);
            string reading = theme == Theme.Teeth ? ToothReading(tarot, reversed) : Reading(tarot, reversed, Subject(theme), empty);
            paragraphs[i] = name + orientation + reading;
            readings.Add(new JObject { ["card"] = name, ["orientation"] = orientation,
                ["dream_evidence"] = empty ? "无直接梦境证据" : sentences.Length > 0 ? sentences[i % sentences.Length] : dream,
                ["contribution"] = reading });
        }

        var events = context.DreamRun?.Events ?? Array.Empty<DreamRunRecorder.Entry>();
        var used = new HashSet<string>();
        int fragments = 0, limit = sparse ? 1 : 2;
        var route = events.FirstOrDefault(e => e.Type == "route_choice" && !string.IsNullOrWhiteSpace(e.Description));
        var hidden = events.FirstOrDefault(e => e.Type == "hidden_route_enter");
        var death = events.FirstOrDefault(e => e.Type == "death");
        var retry = events.FirstOrDefault(e => e.Type == "retry");
        bool choosing = !empty && (sample || theme == Theme.Choice || theme == Theme.Arrival || theme == Theme.Chase);
        bool restart = !empty && (theme == Theme.Teeth || theme == Theme.Falling || theme == Theme.Arrival || sample || HasAny(dream, "反复", "循环", "回到", "重新", "未完成", "中断"));
        if (choosing && route.EventId != null && fragments < limit)
        {
            string actual = route.Description.Contains("下层") ? "这次在岔路口，你走向了下层" : route.Description.Contains("上层") ? "这次在岔路口，你走向了上层" : "这次在岔路口，你选定了一个方向";
            string connection = theme == Theme.Chase ? "。与梦中被追赶的处境相比，这一小段路有了自己作出的方向选择。" : theme == Theme.Arrival ? "。梦中想抵达的地方仍未因此到达，行走却不只剩下赶上的目标，也留下了选路的过程。" : "。梦中的方向与这段路并不相同，作出选择的动作却可以放在一起看。";
            paragraphs[0] += actual + connection;
            AddEvidence(0, route, links, used); fragments++;
        }
        if (restart && death.EventId != null && retry.EventId != null && fragments < limit)
        {
            paragraphs[3] += theme == Theme.Teeth
                ? "而这一路受阻之后，你又重新开始。与梦中的掉落相对照，这次的中断之后还有行动，并没有停在失去的瞬间。"
                : theme == Theme.Falling
                    ? "梦里留下的是下落，而这一路受阻之后，你又重新开始。两段经历在中断之后有了不同的走向。"
                    : "这一路受阻之后，你又重新开始。它与梦中没有结束的过程呼应，也让中断不只留下停止。";
            AddEvidence(3, death, links, used); AddEvidence(3, retry, links, used); fragments++;
        }
        if (choosing && hidden.EventId != null && fragments < limit)
        {
            paragraphs[2] += "后来，你走进了一条开启的支路。它与梦中寻找方向的处境相连，多出来的是尝试另一种走法，尚不能回答梦的终点。";
            AddEvidence(2, hidden, links, used); fragments++;
        }
        if (!empty && theme == Theme.Chase && fragments < limit)
        {
            var combat = events.FirstOrDefault(e => e.Type == "combat");
            var damage = events.FirstOrDefault(e => e.Type == "damage");
            if (combat.EventId != null)
            {
                paragraphs[1] += "梦中的追赶让你处在被接近的一方，而这次途中，你也曾与阻力正面交手。接触的方式有了不同，感受是否相同仍由你辨认。";
                AddEvidence(1, combat, links, used); fragments++;
            }
            else if (damage.EventId != null)
            {
                paragraphs[1] += "这次途中你也曾受到伤害。它与梦中被追赶的处境相近，都是阻力靠近了自己，但并不决定你如何看待它。";
                AddEvidence(1, damage, links, used); fragments++;
            }
        }
        if (!empty && fragments < limit && HasAny(dream, "丢了", "遗失", "寻找", "找不到", "记忆"))
        {
            var collect = events.FirstOrDefault(e => e.Type == "collect");
            if (collect.EventId != null)
            {
                paragraphs[2] += "这次途中，你也停下来拾取过东西。与梦中的寻找放在一起，除了尚未找到的部分，也有真正被留意到的东西。";
                AddEvidence(2, collect, links, used);
            }
        }

        var body = new StringBuilder();
        if (empty) body.Append("这一页还没有留下清楚的梦中画面。腓腓把空白留在这里，四张牌暂时只是四个可以展开的角度。\n\n");
        else
        {
            body.Append("「").Append(excerpt).Append("」\n\n");
            body.Append(Opening(theme)).Append("\n\n");
        }
        // The four structured readings are not four unrelated chapters in the displayed letter.
        // Pair their perspectives around the same dream before returning to its central image.
        body.Append(paragraphs[0]).Append(paragraphs[1]).Append("\n\n");
        body.Append(paragraphs[2]).Append(paragraphs[3]).Append("\n\n");
        body.Append(Closing(theme, empty));
        for (int i = 0; i < 4; i++) readings[i]["behavior_event_ids"] = new JArray(links[i]);
        return new JObject { ["source"] = "local", ["core_interpretation"] = empty ? "等待被记起的梦" : Subject(theme),
            ["card_readings"] = readings, ["used_behavior_event_ids"] = new JArray(used.OrderBy(x => x)),
            ["unsupported_claims"] = new JArray(), ["report_body"] = body.ToString() };
    }

    static Theme FindTheme(string dream, bool empty, bool sample)
    {
        if (empty) return Theme.Open;
        string compact = Regex.Replace(dream, @"[\s\p{P}]", "");
        if (Regex.IsMatch(compact, @"^(我)?(梦到|梦见)?((牙齿|牙)(都|全|全部|突然)?(掉了|脱落了?)|掉牙了?)$")) return Theme.Teeth;
        if (sample) return Theme.Choice;
        if (!HasAny(dream, "没有被追", "没被追", "没有人追") && HasAny(dream, "被追", "追着我", "追赶我", "有人追")) return Theme.Chase;
        if (HasAny(dream, "赶不上", "没赶上", "找不到教室", "还没到", "未能抵达", "未能到达")) return Theme.Arrival;
        if (!HasAny(dream, "没有掉", "没掉", "没有坠落") && HasAny(dream, "坠落", "一直下落", "往下掉", "从高处掉")) return Theme.Falling;
        if (HasAny(dream, "去世", "故人", "前任", "重逢", "分手")) return Theme.Reunion;
        if (HasAny(dream, "迷路", "找路", "不知道往哪") ||
            (HasAny(dream, "岔路", "路口") && HasAny(dream, "选择", "犹豫", "没决定", "未决定", "不知"))) return Theme.Choice;
        return Theme.Open;
    }
    static string Subject(Theme theme) => theme switch
    {
        Theme.Teeth => "熟悉的身体发生变化", Theme.Chase => "被追赶的处境", Theme.Arrival => "想赶上却未能抵达的过程",
        Theme.Falling => "失去支撑与下落", Theme.Reunion => "关系在梦中的再次出现", Theme.Choice => "梦中尚待辨认的方向", _ => "这段梦中的经历"
    };
    static string Opening(Theme theme) => theme switch
    {
        Theme.Teeth => "熟悉的身体发生了变化。腓腓想停在这个动作上，让掉落除了失去，也保留松脱或改变的可能。",
        Theme.Chase => "被追赶把距离变成了梦的一部分。腓腓读到这里，会留意接近与离开之间的关系，而不急着给追赶一个现实中的名字。",
        Theme.Arrival => "想抵达的地方，与真正走到的地方之间还有距离。腓腓读到的，不只是赶上与错过，也有寻找的过程。",
        Theme.Falling => "下落让支撑变得格外清楚。腓腓想陪你停在这个动作上，看看失去支撑与暂时放开，是否可能是不同的读法。",
        Theme.Reunion => "某段关系在梦里再次出现。腓腓更愿意把目光留在这次相遇本身：它如何发生，比替它决定一个现实中的结果更值得停留。",
        Theme.Choice => "方向并不总在行走之前就清楚。腓腓读到这段梦，会留意抵达之前那些辨认与选择的时刻。",
        _ => "腓腓想陪你再看一看这段经历。同一个梦里的动作或画面，也许能从几个不同的角度理解。"
    };
    static string Closing(Theme theme, bool empty)
    {
        if (empty) return "我把这一页留给尚未想起的画面，暂时不替它决定含义。\n\n——尚未写下的梦，也可以留白";
        return theme switch
        {
            Theme.Teeth => "掉落可以让人想到失去，也可以让人想到熟悉的样子改变了。它留下的感受，仍可以有不同的名字。\n\n——熟悉的样子，也会发生变化",
            Theme.Chase => "追赶带来的不只是距离，也可能是接近与离开的关系。哪一层更贴近这场梦，仍可以慢慢辨认。\n\n——距离之中，还有不同的关系",
            Theme.Arrival => "这场梦也许不只关乎有没有赶上，还关乎抵达之前的过程。终点仍悬着，寻找本身已经是梦的一部分。\n\n——抵达之前，寻找也在发生",
            Theme.Falling => "下落没有替这场梦决定一个结局。它也许让支撑变得清楚，也许留下了失去控制时的另一种感受。\n\n——下落的意义，不只有一种",
            Theme.Reunion => "这次梦中的相遇，并不替现实中的关系作出回答。它让某个曾经相连的部分，再次有了被看见的机会。\n\n——相遇的意义，仍然留有余地",
            Theme.Choice => "方向还没有因此变得唯一。也许这场梦留住的，正是决定之前可以辨认不同可能的时刻。\n\n——方向未定，可能仍然存在",
            _ => "同一段经历可以有不同的读法。哪一层更贴近你醒来时记住的感觉，暂时不必只有一个答案。\n\n——同一场梦，仍有不同的读法"
        };
    }
    static string Reading(string tarot, bool reversed, string subject, bool empty)
    {
        string angle = tarot switch
        {
            "愚者" => reversed ? "开始与迟疑之间，也许还有未看清的部分" : "一个开始不一定先有确定的终点",
            "魔术师" => reversed ? "行动与本来的意图，也许未必总在同一方向" : "可能性与真正采取行动之间，还隔着一次选择",
            "女祭司" => reversed ? "不清楚的感受，可能让理解暂时失去方向" : "尚未说清的感受，也可以暂时保留自己的位置",
            "女皇" => reversed ? "付出和得到承接之间，也许存在不同的分量" : "一个处境里，也许还有能够容纳不同感受的余地",
            "皇帝" => reversed ? "原本提供秩序的东西，也可能让变化难以发生" : "边界既能提供支撑，也会让内外的差别更加清楚",
            "教皇" => reversed ? "熟悉的理解之外，也许还有自己的看法" : "习惯中的理解，会影响一段经历被怎样看待",
            "恋人" => reversed ? "靠近与保持距离之间，未必已经有一致的选择" : "彼此相连的部分，也可能需要各自作出选择",
            "战车" => reversed ? "移动的速度与想去的方向，未必总能一致" : "方向与行动相连时，行走才有了明确的指向",
            "力量" => reversed ? "用力与犹疑之间，可能还有尚未被承接的感受" : "面对阻力，也可以包含耐心而不只是用力",
            "隐者" => reversed ? "独处与隔开之间，也许有不容易察觉的距离" : "停下来辨认自己的感受，也是一种寻找",
            "命运之轮" => reversed ? "重复的过程，也许让某个尚未结束的部分再次出现" : "变化会让同一件事在不同的时刻呈现不同意味",
            "正义" => reversed ? "期待与实际经历之间，也许有未能平衡的部分" : "选择与随之发生的事，可以放在一起重新看待",
            "倒吊人" => reversed ? "一直维持同一个角度，也许会让理解停住" : "暂时停下，也许能让同一件事显出另一个侧面",
            "死神" => reversed ? "告别与继续之间，也许有尚未放开的部分" : "结束除了失去，也可以被理解为一种变化",
            "节制" => reversed ? "不同的需要，也许还没有找到能够共处的位置" : "不同的感受可以同时存在，不必立刻相互抵消",
            "恶魔" => reversed ? "曾经紧密的连接，也许开始有了重新选择的余地" : "某种连接带来的牵引，可能比表面看起来更难松开",
            "塔" => reversed ? "维持熟悉的样子，与容许变化之间也许存在拉扯" : "原有结构的改变，会让曾经依靠的部分变得清楚",
            "星星" => reversed ? "暂时看不见可能，不等于已经知道了最终结果" : "结果未定的时候，也可以给可能性留一个位置",
            "月亮" => reversed ? "辨清一部分感受，可能让整段经历有不同的读法" : "不确定会影响一段经历被看见的方式",
            "太阳" => reversed ? "尚未明朗的结果，也许让感受停在等待之中" : "把经历看清楚，与立刻得到答案可以是两回事",
            "审判" => reversed ? "理解过去与作出回应之间，也许还有停留" : "重新回看同一段经历，可能改变它被理解的方式",
            "世界" => reversed ? "尚未结束的部分，也许正是这段经历仍有分量的地方" : "把不同部分放在一起看，可能让整体关系更清楚",
            _ => "同一段经历，也许还有另一种读法"
        };
        return empty ? "留下这样的角度：" + angle + "。" : "与" + subject + "放在一起，可以这样理解：" + angle + "。";
    }
    private static string ToothReading(string tarot, bool reversed)
    {
        // Complete prose selected by the actual card and orientation, not instructions from a prompt.
        switch (tarot)
        {
            case "愚者": return reversed ? "也许贴近变化来临时尚未准备好的那一面；牙齿的掉落显得比理解它更快。" : "让掉落不只像一个结束，也可能是熟悉状态之外的一种开端。";
            case "魔术师": return reversed ? "可能回应身体发生变化、意图却未必能掌握它的落差。" : "把这幅掉落的画面与主动性放在一起：变化之中，也许仍有能回应的部分。";
            case "女祭司": return reversed ? "让牙齿掉落后尚不清楚的感受浮现出来，像是变化已经发生，理解还没跟上。" : "把目光留在掉落之后还没说出的感受上，而不急于替这幅画面命名。";
            case "女皇": return reversed ? "可能让掉落带上一点耗损的意味，原本完整的一部分似乎难以维持。" : "为这场变化添上承接的意味：一部分松脱时，是否仍有可以容纳变化的余地。";
            case "皇帝": return reversed ? "也许与原本稳固的结构发生松动相呼应，熟悉的秩序没有保持原样。" : "让人留意牙齿原本的稳固，也让掉落与对边界、结构的理解相遇。";
            case "教皇": return reversed ? "让掉落有了离开旧有样子的可能；熟悉不一定是唯一的形状。" : "把牙齿原本熟悉的样子与惯常的理解相连，变化也许打断了某种习以为常。";
            case "恋人": return reversed ? "也许回应原本相连的部分出现脱离，完整与分离之间有了一道缝隙。" : "让脱落的一部分与整体的关系变得清楚：变化究竟改变了怎样的连接。";
            case "战车": return reversed ? "可能贴近变化难以掌握的那一瞬，掉落发生了，却未必有清楚的方向。" : "为这场身体变化带来方向的角度：掉落之后的进程，还没有在梦里展开。";
            case "力量": return reversed ? "也许让这幅画面显得不再坚固，但这种脆弱只是梦的一种可能意味。" : "让掉落与温柔的承受相遇，变化未必只能以强硬的方式被理解。";
            case "隐者": return reversed ? "让脱离整体的那部分，可能带上一点分隔的意味。" : "把注意力收回这场身体的变化，让掉落本身成为可以停留的画面。";
            case "命运之轮": return reversed ? "可能让掉落停在尚未转过去的一刻，变化发生了，后续却仍悬着。" : "让掉落也带上更替的意味，熟悉的状态并非总会保持不变。";
            case "正义": return reversed ? "可能回应完整被打破后的不平衡，掉落改变了原本的排列。" : "把掉落前后的差异放在一起，变化也许让原本看不见的平衡变得清楚。";
            case "倒吊人": return reversed ? "也许贴近变化已发生、理解却仍停在原处的那一面。" : "让这幅掉落的画面暂时慢下来，也许换一个角度，它还有另一种含义。";
            case "死神": return reversed ? "可能让牙齿的掉落与难以告别旧样子的意味相遇。" : "让掉落呈现出结束和更替的意味，一部分离开了原本的位置。";
            case "节制": return reversed ? "也许回应身体熟悉的协调被打断，掉落使整体显得不再一样。" : "为变化带来重新协调的角度：原本的形状改变之后，整体仍可能有别的安排。";
            case "恶魔": return reversed ? "让掉落也可能像一次松脱，原本紧密相连的部分有了距离。" : "让这幅画面与依附的意味相遇：原本紧连的一部分，究竟为何显得重要。";
            case "塔": return reversed ? "也许把目光放在稳固已经松动、却还没有被完全理解的地方。" : "与掉落带来的突然变化呼应，原本熟悉的结构在这一刻被打断。";
            case "星星": return reversed ? "让人留意掉落之后尚未显现的可能，变化还没有给出清楚的后续。" : "为变化保留希望的角度，掉落也许不是理解这场梦的终点。";
            case "月亮": return reversed ? "让掉落这一具体变化变得清楚，至于它为何发生，仍没有唯一的解释。" : "让熟悉身体发生变化时的不确定浮现出来，掉落的含义仍朦胧着。";
            case "太阳": return reversed ? "也许回应变化之后尚未明朗的那一面，掉落之外的画面没有完全展开。" : "把这场身体变化照得更清楚：掉落是真切的画面，它的含义却仍可以开放。";
            case "审判": return reversed ? "可能让掉落停在还没来得及理解的一刻，变化与回应之间仍有距离。" : "让这场掉落有了重新理解熟悉之物的意味，旧有的样子也许因此被再次看见。";
            case "世界": return reversed ? "与完整中少了一部分的画面呼应，掉落之后似乎还有一环没有接上。" : "把掉落的一部分与整个身体放在一起看，变化也可能属于一个更大的过程。";
            default: return "为这场掉落保留了另一种可能的理解。";
        }
    }

    static bool HasAny(string value, params string[] words) => words.Any(value.Contains);
    static string CanonicalTarotName(string name) => name == "高塔" ? "塔" : name == "隐士" ? "隐者" : name;
    static void AddEvidence(int slot, DreamRunRecorder.Entry entry, List<string>[] links, HashSet<string> used)
    {
        if (string.IsNullOrEmpty(entry.EventId) || !used.Add(entry.EventId)) return;
        links[slot].Add(entry.EventId);
    }
}
