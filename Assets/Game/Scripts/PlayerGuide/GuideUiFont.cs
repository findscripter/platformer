using TMPro;
using UnityEngine;

public static class GuideUiFont
{
    private const string FontAssetGuid = "7e794ed8003821b4dac6b110719e0135";

    public static TMP_FontAsset Load()
    {
#if UNITY_EDITOR
        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(FontAssetGuid);
        var editorFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (editorFont != null)
            return editorFont;
#endif
        var resourceFont = Resources.Load<TMP_FontAsset>("Game/Art/source/dialogue_font_TMP");
        return resourceFont != null ? resourceFont : TMP_Settings.defaultFontAsset;
    }

    public static void Apply(TMP_Text text)
    {
        if (text == null)
            return;

        var font = Load();
        if (font != null)
            text.font = font;
    }
}
