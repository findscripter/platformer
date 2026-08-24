using UnityEditor;
using UnityEngine;

public static class EffectsSetup
{
    [MenuItem("Tools/Art/Setup Effects")]
    public static void SetupEffects()
    {
        // 确保特效贴图正确导入
        ConfigureEffectTexture("Assets/Game/Art/Effects/Mist FX.png");
        ConfigureEffectTexture("Assets/Game/Art/Effects/Mist FX2.png");
        ConfigureEffectTexture("Assets/Game/Art/Effects/Wave FX.png");

        // 创建材质
        CreateEffectMaterial("Mist FX", "Assets/Game/Art/Effects/Mist FX.png", "Assets/Game/Art/Effects/MistFX_Material.mat");
        CreateEffectMaterial("Mist FX2", "Assets/Game/Art/Effects/Mist FX2.png", "Assets/Game/Art/Effects/MistFX2_Material.mat");
        CreateEffectMaterial("Wave FX", "Assets/Game/Art/Effects/Wave FX.png", "Assets/Game/Art/Effects/WaveFX_Material.mat");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Effects setup complete! Materials created for Mist FX, Mist FX2, and Wave FX.");
    }

    private static void ConfigureEffectTexture(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
    }

    private static void CreateEffectMaterial(string name, string texturePath, string materialPath)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture == null)
        {
            Debug.LogWarning($"Texture not found: {texturePath}");
            return;
        }

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (mat == null)
        {
            // 使用 Particles/Standard Unlit 着色器（支持半透明和 alpha）
            Shader shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            mat = new Material(shader);
            mat.name = name;
            AssetDatabase.CreateAsset(mat, materialPath);
        }

        mat.mainTexture = texture;

        // 设置渲染模式为 Transparent
        mat.SetFloat("_Mode", 3); // Transparent mode
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;

        EditorUtility.SetDirty(mat);
        Debug.Log($"Created material: {materialPath}");
    }
}
