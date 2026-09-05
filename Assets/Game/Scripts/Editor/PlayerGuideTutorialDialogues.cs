using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 创建PlayerGuide场景的教学对话资源
/// </summary>
public static class PlayerGuideTutorialDialogues
{
    private const string OutputFolder = "Assets/Game/Resources/Dialogue/PlayerGuide";

    [MenuItem("Tools/GameJam/5. Generate Tutorial Dialogues")]
    public static void GenerateAll()
    {
        Debug.Log("=== 生成教学对话资源 ===");

        Directory.CreateDirectory(OutputFolder);

        CreateIntroDialogue();
        CreateMoveTeachDialogue();
        CreateJumpTeachDialogue();
        CreateCollectTeachDialogue();
        CreateFeifeiGreetingDialogue();
        CreateEndDialogue();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        DialogueRegistry.Reload();

        Debug.Log("=== 教学对话生成完成 ===");
        Debug.Log($"对话文件位置: {OutputFolder}");
    }

    private static void CreateIntroDialogue()
    {
        var dialogue = LoadOrCreate<DialogueSO>($"{OutputFolder}/dlg_tutorial_intro.asset");
        dialogue.dialogueId = "dlg_tutorial_intro";

        var welcome = new TextNode
        {
            nodeId = "welcome",
            speaker = "梦境之声",
            text = "欢迎来到梦境世界...\n这里是你的试炼场，也是你掌握力量的起点。"
        };

        var instruction = new TextNode
        {
            nodeId = "instruction",
            speaker = "梦境之声",
            text = "在前方，你将学习基本的移动、跳跃与交互。\n准备好了吗？"
        };

        var ready = new ChoiceNode
        {
            nodeId = "ready_choice",
            options = new[]
            {
                new ChoiceOption { text = "我准备好了" },
                new ChoiceOption { text = "先让我看看周围" }
            }
        };

        var start = new TextNode
        {
            nodeId = "start",
            speaker = "梦境之声",
            text = "很好。前进吧，试炼者。"
        };

        var explore = new TextNode
        {
            nodeId = "explore",
            speaker = "梦境之声",
            text = "不必着急。慢慢适应这个世界，当你准备好时再前进。"
        };

        var end = new EndNode { nodeId = "intro_end" };

        welcome.nextNode = instruction;
        instruction.nextNode = ready;
        ready.options[0].nextNode = start;
        ready.options[1].nextNode = explore;
        start.nextNode = end;
        explore.nextNode = end;

        dialogue.startNode = welcome;
        EditorUtility.SetDirty(dialogue);

        Debug.Log("  ✓ 创建对话: dlg_tutorial_intro");
    }

    private static void CreateMoveTeachDialogue()
    {
        var dialogue = LoadOrCreate<DialogueSO>($"{OutputFolder}/dlg_tutorial_move.asset");
        dialogue.dialogueId = "dlg_tutorial_move";

        var teach = new TextNode
        {
            nodeId = "teach_move",
            speaker = "教学提示",
            text = "【基础移动】\n\n使用 A/D 键或 ← → 方向键进行左右移动。\n\n试着走到平台边缘感受一下。"
        };

        var tip = new TextNode
        {
            nodeId = "move_tip",
            speaker = "教学提示",
            text = "小贴士：在空中也可以进行有限的方向调整。"
        };

        var end = new EndNode { nodeId = "move_end" };

        teach.nextNode = tip;
        tip.nextNode = end;

        dialogue.startNode = teach;
        EditorUtility.SetDirty(dialogue);

        Debug.Log("  ✓ 创建对话: dlg_tutorial_move");
    }

    private static void CreateJumpTeachDialogue()
    {
        var dialogue = LoadOrCreate<DialogueSO>($"{OutputFolder}/dlg_tutorial_jump.asset");
        dialogue.dialogueId = "dlg_tutorial_jump";

        var teach = new TextNode
        {
            nodeId = "teach_jump",
            speaker = "教学提示",
            text = "【跳跃】\n\n按下 空格键 或 W/↑ 进行跳跃。\n\n前方有几个平台，试着跳过去。"
        };

        var tip = new TextNode
        {
            nodeId = "jump_tip",
            speaker = "教学提示",
            text = "技巧：按住跳跃键的时间越长，跳得越高。\n\n在空中也可以调整左右方向。"
        };

        var end = new EndNode { nodeId = "jump_end" };

        teach.nextNode = tip;
        tip.nextNode = end;

        dialogue.startNode = teach;
        EditorUtility.SetDirty(dialogue);

        Debug.Log("  ✓ 创建对话: dlg_tutorial_jump");
    }

    private static void CreateCollectTeachDialogue()
    {
        var dialogue = LoadOrCreate<DialogueSO>($"{OutputFolder}/dlg_tutorial_collect.asset");
        dialogue.dialogueId = "dlg_tutorial_collect";

        var teach = new TextNode
        {
            nodeId = "teach_collect",
            speaker = "教学提示",
            text = "【收集】\n\n前方有几个梦境碎片（金色光球）。\n\n碰到它们即可自动收集。"
        };

        var purpose = new TextNode
        {
            nodeId = "collect_purpose",
            speaker = "教学提示",
            text = "收集梦境碎片可以解锁新的能力和区域。\n\n试着收集所有碎片，然后前往终点。"
        };

        var end = new EndNode { nodeId = "collect_end" };

        teach.nextNode = purpose;
        purpose.nextNode = end;

        dialogue.startNode = teach;
        EditorUtility.SetDirty(dialogue);

        Debug.Log("  ✓ 创建对话: dlg_tutorial_collect");
    }

    private static void CreateFeifeiGreetingDialogue()
    {
        var dialogue = LoadOrCreate<DialogueSO>($"{OutputFolder}/dlg_feifei_greeting.asset");
        dialogue.dialogueId = "dlg_feifei_greeting";

        var greeting = new TextNode
        {
            nodeId = "greeting",
            speaker = "腓腓",
            text = "你好呀！我是腓腓，这个梦境世界的守护者。"
        };

        var praise = new TextNode
        {
            nodeId = "praise",
            speaker = "腓腓",
            text = "看起来你已经掌握了基本的移动和跳跃，\n做得不错！"
        };

        var nextStep = new TextNode
        {
            nodeId = "next_step",
            speaker = "腓腓",
            text = "前方就是通往真正冒险的传送门。\n\n你准备好接受更大的挑战了吗？"
        };

        var choice = new ChoiceNode
        {
            nodeId = "choice",
            options = new[]
            {
                new ChoiceOption { text = "我准备好了！" },
                new ChoiceOption { text = "再给我一些建议" }
            }
        };

        var ready = new TextNode
        {
            nodeId = "ready",
            speaker = "腓腓",
            text = "很有勇气！祝你好运，试炼者。\n\n我会在这里等你的好消息。"
        };

        var advice = new TextNode
        {
            nodeId = "advice",
            speaker = "腓腓",
            text = "记住：观察环境，耐心探索。\n\n有些秘密藏在你想不到的地方。\n\n还有，不要害怕失败——梦境会给你无限次重来的机会。"
        };

        var end = new EndNode { nodeId = "feifei_end" };

        greeting.nextNode = praise;
        praise.nextNode = nextStep;
        nextStep.nextNode = choice;
        choice.options[0].nextNode = ready;
        choice.options[1].nextNode = advice;
        ready.nextNode = end;
        advice.nextNode = choice;

        dialogue.startNode = greeting;
        EditorUtility.SetDirty(dialogue);

        Debug.Log("  ✓ 创建对话: dlg_feifei_greeting");
    }

    private static void CreateEndDialogue()
    {
        var dialogue = LoadOrCreate<DialogueSO>($"{OutputFolder}/dlg_tutorial_end.asset");
        dialogue.dialogueId = "dlg_tutorial_end";

        var complete = new TextNode
        {
            nodeId = "complete",
            speaker = "梦境之声",
            text = "恭喜你完成了新手引导！\n\n你已经掌握了基本的移动、跳跃和收集能力。"
        };

        var transition = new TextNode
        {
            nodeId = "transition",
            speaker = "梦境之声",
            text = "现在，真正的冒险即将开始...\n\n准备好进入主游戏场景了吗？"
        };

        var choice = new ChoiceNode
        {
            nodeId = "end_choice",
            options = new[]
            {
                new ChoiceOption { text = "进入游戏" },
                new ChoiceOption { text = "返回主菜单" }
            }
        };

        var loadGameplay = new EventNode
        {
            nodeId = "load_gameplay",
            actions = new System.Collections.Generic.List<IEventAction>
            {
                new LoadSceneEventAction
                {
                    sceneName = "Gameplay",
                    resumeDialogueAfterLoad = false
                }
            }
        };

        var loadMenu = new EventNode
        {
            nodeId = "load_menu",
            actions = new System.Collections.Generic.List<IEventAction>
            {
                new LoadSceneEventAction
                {
                    sceneName = "MainMenu",
                    resumeDialogueAfterLoad = false
                }
            }
        };

        var end = new EndNode { nodeId = "tutorial_complete_end" };

        complete.nextNode = transition;
        transition.nextNode = choice;
        choice.options[0].nextNode = loadGameplay;
        choice.options[1].nextNode = loadMenu;
        loadGameplay.nextNode = end;
        loadMenu.nextNode = end;

        dialogue.startNode = complete;
        EditorUtility.SetDirty(dialogue);

        Debug.Log("  ✓ 创建对话: dlg_tutorial_end");
    }

    private static T LoadOrCreate<T>(string assetPath) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (asset != null)
            return asset;

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, assetPath);
        return asset;
    }
}
