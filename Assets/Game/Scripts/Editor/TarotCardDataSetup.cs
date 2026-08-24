using UnityEditor;
using UnityEngine;

/// <summary>
/// 创建 4 张固定塔罗牌数据资源
/// </summary>
public static class TarotCardDataSetup
{
    private const string OutputFolder = "Assets/Game/Resources/PlayerGuide/TarotCards";

    [MenuItem("Tools/GameJam/7. Generate Tarot Card Data Assets")]
    public static void Generate()
    {
        Debug.Log("=== 生成塔罗牌数据资源 ===");

        // 确保目录存在
        if (!System.IO.Directory.Exists(OutputFolder))
        {
            System.IO.Directory.CreateDirectory(OutputFolder);
        }

        // 创建 4 张固定卡牌
        CreateCard("Card_YingLong", "应龙", "战车", isMain: true);
        CreateCard("Card_RiYueLunZhuan", "日月轮转", "命运之轮", isMain: false);
        CreateCard("Card_ChangXi", "常羲·星月祈愿", "星星", isMain: false);
        CreateCard("Card_ChanChuJingPo", "蟾蜍精魄", "月亮", isMain: false);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✓ 4 张塔罗牌数据已生成: {OutputFolder}");
    }

    private static void CreateCard(string fileName, string displayName, string tarotName, bool isMain)
    {
        string assetPath = $"{OutputFolder}/{fileName}.asset";

        var card = AssetDatabase.LoadAssetAtPath<TarotCardData>(assetPath);
        if (card == null)
        {
            card = ScriptableObject.CreateInstance<TarotCardData>();
            AssetDatabase.CreateAsset(card, assetPath);
        }

        // 使用反射设置私有字段
        var cardIdField = typeof(TarotCardData).GetField("cardId",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var displayNameField = typeof(TarotCardData).GetField("displayName",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var tarotNameField = typeof(TarotCardData).GetField("tarotName",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var isMainCardField = typeof(TarotCardData).GetField("isMainCard",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        cardIdField?.SetValue(card, fileName);
        displayNameField?.SetValue(card, displayName);
        tarotNameField?.SetValue(card, tarotName);
        isMainCardField?.SetValue(card, isMain);

        EditorUtility.SetDirty(card);

        Debug.Log($"  ✓ 创建卡牌: {displayName} ({tarotName}) [主牌: {isMain}]");
    }
}
