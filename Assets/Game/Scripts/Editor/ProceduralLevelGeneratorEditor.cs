using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ProceduralLevelGenerator))]
public class ProceduralLevelGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ProceduralLevelGenerator generator = (ProceduralLevelGenerator)target;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "平台间距: 3.5–5.5 单位（基于玩家 60%–85% 跳跃能力）\n" +
            "高度差: -2.5 到 +3.0 单位（最大跳跃高度 3.26）\n" +
            "敌人生成: 30% 概率站在平台上\n" +
            "存档点: 每 12 个平台一个\n" +
            "塔罗点: 4 个均匀分布",
            MessageType.Info
        );

        EditorGUILayout.Space();

        if (GUILayout.Button("生成关卡", GUILayout.Height(40)))
        {
            Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "Generate Level");
            generator.Generate();
            EditorUtility.SetDirty(generator);
            SceneView.RepaintAll();
        }
    }
}
