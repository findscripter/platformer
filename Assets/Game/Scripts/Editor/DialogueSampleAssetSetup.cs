using System.IO;
using UnityEditor;
using UnityEngine;

public static class DialogueSampleAssetSetup
{
    private const string OutputFolder = "Assets/Game/Resources/Dialogue";
    private const string TutorialAssetPath = OutputFolder + "/dlg_tutorial_001.asset";
    private const string CrossSceneAssetPath = OutputFolder + "/dlg_cross_scene_demo.asset";

    [MenuItem("Tools/Platformer/Create Sample Dialogue Assets")]
    public static void CreateFromMenu()
    {
        Directory.CreateDirectory(OutputFolder);

        CreateTutorialDialogue();
        CreateCrossSceneDialogue();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        DialogueRegistry.Reload();
        Debug.Log("Sample dialogue assets created in Assets/Game/Resources/Dialogue.");
    }

    private static void CreateTutorialDialogue()
    {
        var dialogue = LoadOrCreate<DialogueSO>(TutorialAssetPath);
        dialogue.dialogueId = "dlg_tutorial_001";

        var welcome = new TextNode
        {
            nodeId = "welcome",
            speaker = "向导",
            text = "欢迎来到平台跳跃世界，这里将教你基础操作。"
        };
        var choice = new ChoiceNode
        {
            nodeId = "path_choice",
            options = new[]
            {
                new ChoiceOption { text = "我想学移动" },
                new ChoiceOption { text = "我想学交互" }
            }
        };
        var moveReply = new TextNode
        {
            nodeId = "move_reply",
            speaker = "向导",
            text = "使用方向键或 WASD 移动，空格跳跃。"
        };
        var interactReply = new TextNode
        {
            nodeId = "interact_reply",
            speaker = "向导",
            text = "靠近可交互物体时按 E 进行交互。"
        };
        var password = new InputNode
        {
            nodeId = "password_check",
            question = "请输入通行密码（demo）",
            expectedAnswer = "open"
        };
        var passwordSuccess = new TextNode
        {
            nodeId = "password_success",
            speaker = "向导",
            text = "密码正确，门已为你打开。"
        };
        var passwordFail = new TextNode
        {
            nodeId = "password_fail",
            speaker = "向导",
            text = "密码不对，再试一次吧。"
        };
        var openDoorEvent = new EventNode
        {
            nodeId = "open_door_event",
            actions = new System.Collections.Generic.List<IEventAction>
            {
                new OpenDoorAction { doorId = "tutorial_gate" }
            }
        };
        var end = new EndNode { nodeId = "end" };

        welcome.nextNode = choice;
        choice.options[0].nextNode = moveReply;
        choice.options[1].nextNode = interactReply;
        moveReply.nextNode = password;
        interactReply.nextNode = password;
        password.successNode = passwordSuccess;
        password.failNode = passwordFail;
        passwordSuccess.nextNode = openDoorEvent;
        passwordFail.nextNode = password;
        openDoorEvent.nextNode = end;

        dialogue.startNode = welcome;
        EditorUtility.SetDirty(dialogue);
    }

    private static void CreateCrossSceneDialogue()
    {
        var dialogue = LoadOrCreate<DialogueSO>(CrossSceneAssetPath);
        dialogue.dialogueId = "dlg_cross_scene_demo";

        var ask = new TextNode
        {
            nodeId = "ask_travel",
            speaker = "传送师",
            text = "要前往 Gameplay 场景继续对话吗？"
        };
        var confirm = new TextNode
        {
            nodeId = "confirm_travel",
            speaker = "传送师",
            text = "正在为你打开传送门……"
        };
        var loadScene = new EventNode
        {
            nodeId = "load_gameplay",
            actions = new System.Collections.Generic.List<IEventAction>
            {
                new LoadSceneEventAction
                {
                    sceneName = "Gameplay",
                    resumeDialogueAfterLoad = true
                }
            }
        };
        var arrive = new TextNode
        {
            nodeId = "arrive_gameplay",
            speaker = "传送师",
            text = "已抵达新场景，对话从断点继续。"
        };
        var end = new EndNode { nodeId = "cross_scene_end" };

        ask.nextNode = confirm;
        confirm.nextNode = loadScene;
        loadScene.nextNode = arrive;
        arrive.nextNode = end;

        dialogue.startNode = ask;
        EditorUtility.SetDirty(dialogue);
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
