using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Game.Editor
{
    /// <summary>
    /// 一键完成所有美术资源导入、配置、动画生成、场景搭建
    /// </summary>
    public class OneClickSetupTool : EditorWindow
    {
        private const string SOURCE_ART_PATH = @"E:\腾讯gamejam\美术资源\extracted";
        private const string UNITY_ART_PATH = "Assets/Game/Art";

        private bool step1_CopyFiles = true;
        private bool step2_ConfigureSprites = true;
        private bool step3_GenerateAnimations = true;
        private bool step4_LinkSceneResources = true;

        private Vector2 scrollPos;
        private string statusLog = "";

        [MenuItem("Tools/GameJam/🚀 One Click Setup (All in One)")]
        public static void ShowWindow()
        {
            var window = GetWindow<OneClickSetupTool>("一键搭建工具");
            window.minSize = new Vector2(600, 500);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("🎮 GameJam 一键搭建工具", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // 步骤选择
            GUILayout.Label("执行步骤:", EditorStyles.boldLabel);
            step1_CopyFiles = EditorGUILayout.Toggle("1️⃣ 复制美术资源到Unity", step1_CopyFiles);
            step2_ConfigureSprites = EditorGUILayout.Toggle("2️⃣ 配置Sprite导入设置", step2_ConfigureSprites);
            step3_GenerateAnimations = EditorGUILayout.Toggle("3️⃣ 生成动画剪辑", step3_GenerateAnimations);
            step4_LinkSceneResources = EditorGUILayout.Toggle("4️⃣ 链接场景资源", step4_LinkSceneResources);

            GUILayout.Space(10);

            // 执行按钮
            if (GUILayout.Button("🚀 开始执行", GUILayout.Height(40)))
            {
                ExecuteSetup();
            }

            GUILayout.Space(10);

            // 状态日志
            GUILayout.Label("执行日志:", EditorStyles.boldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(300));
            EditorGUILayout.TextArea(statusLog, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("清空日志"))
            {
                statusLog = "";
            }
        }

        private void ExecuteSetup()
        {
            statusLog = "";
            Log("========== 开始执行一键搭建 ==========");

            try
            {
                if (step1_CopyFiles) Step1_CopyArtResources();
                if (step2_ConfigureSprites) Step2_ConfigureSprites();
                if (step3_GenerateAnimations) Step3_GenerateAnimations();
                if (step4_LinkSceneResources) Step4_LinkSceneResources();

                Log("========== ✅ 全部完成！ ==========");
                EditorUtility.DisplayDialog("完成", "一键搭建完成！\n请打开 PlayerGuide 场景并点击 Play 测试。", "OK");
            }
            catch (System.Exception e)
            {
                Log($"❌ 错误: {e.Message}");
                Log(e.StackTrace);
                EditorUtility.DisplayDialog("错误", $"执行出错:\n{e.Message}", "OK");
            }
        }

        #region Step 1: 复制美术资源

        private void Step1_CopyArtResources()
        {
            Log("\n[步骤1] 复制美术资源到Unity...");

            if (!Directory.Exists(SOURCE_ART_PATH))
            {
                Log($"⚠️ 源目录不存在: {SOURCE_ART_PATH}");
                return;
            }

            // 复制关键帧动画
            CopyAnimationFrames("角色", "Player");
            CopyAnimationFrames("怪物", "Enemy");
            CopyAnimationFrames("腓腓", "Feifei");

            // 复制UI元件
            CopyUIElements();

            AssetDatabase.Refresh();
            Log("✅ 步骤1完成: 资源复制完成");
        }

        private void CopyAnimationFrames(string sourceName, string targetName)
        {
            string sourcePath = Path.Combine(SOURCE_ART_PATH, "关键帧", "关键帧", sourceName);
            string targetPath = Path.Combine(Application.dataPath, "Game/Art/Characters/Animations", targetName);

            if (!Directory.Exists(sourcePath))
            {
                Log($"⚠️ 源目录不存在: {sourcePath}");
                return;
            }

            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }

            CopyDirectory(sourcePath, targetPath);
            Log($"  ✓ 复制 {sourceName} → {targetName}");
        }

        private void CopyUIElements()
        {
            string sourcePath = Path.Combine(SOURCE_ART_PATH, "元件");
            string targetPath = Path.Combine(Application.dataPath, "Game/Art/UI/Elements");

            if (!Directory.Exists(sourcePath))
            {
                Log($"⚠️ UI元件目录不存在: {sourcePath}");
                return;
            }

            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }

            CopyDirectory(sourcePath, targetPath);
            Log($"  ✓ 复制 UI元件");
        }

        private void CopyDirectory(string sourceDir, string targetDir)
        {
            foreach (string file in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
            {
                string ext = Path.GetExtension(file).ToLower();
                if (ext != ".png" && ext != ".jpg" && ext != ".jpeg") continue;

                string relativePath = file.Substring(sourceDir.Length + 1);
                string targetFile = Path.Combine(targetDir, relativePath);
                string targetFileDir = Path.GetDirectoryName(targetFile);

                if (!Directory.Exists(targetFileDir))
                {
                    Directory.CreateDirectory(targetFileDir);
                }

                File.Copy(file, targetFile, true);
            }
        }

        #endregion

        #region Step 2: 配置Sprite导入设置

        private void Step2_ConfigureSprites()
        {
            Log("\n[步骤2] 配置Sprite导入设置...");

            // 配置角色动画帧
            ConfigureCharacterSprites("Player");
            ConfigureCharacterSprites("Enemy");
            ConfigureCharacterSprites("Feifei");

            // 配置UI元件
            ConfigureUISprites();

            AssetDatabase.Refresh();
            Log("✅ 步骤2完成: Sprite配置完成");
        }

        private void ConfigureCharacterSprites(string characterName)
        {
            string path = $"Assets/Game/Art/Characters/Animations/{characterName}";
            if (!Directory.Exists(Path.Combine(Application.dataPath, path.Substring(7))))
            {
                Log($"⚠️ 目录不存在: {path}");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { path });
            int count = 0;

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 100;
                    importer.filterMode = FilterMode.Point;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.maxTextureSize = 2048;
                    importer.SaveAndReimport();
                    count++;
                }
            }

            Log($"  ✓ 配置 {characterName}: {count} 个文件");
        }

        private void ConfigureUISprites()
        {
            string path = "Assets/Game/Art/UI/Elements";
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { path });
            int count = 0;

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

                if (importer != null && Path.GetFileName(assetPath) == "dialogues.png")
                {
                    // dialogues.png 需要切分成多个Sprite
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Multiple;
                    importer.spritePixelsPerUnit = 100;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.SaveAndReimport();
                    count++;
                    Log($"  ⚠️ dialogues.png 已设置为Multiple模式，请手动用Sprite Editor切分");
                }
                else if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 100;
                    importer.SaveAndReimport();
                    count++;
                }
            }

            Log($"  ✓ 配置 UI元件: {count} 个文件");
        }

        #endregion

        #region Step 3: 生成动画剪辑

        private void Step3_GenerateAnimations()
        {
            Log("\n[步骤3] 生成动画剪辑...");

            GenerateCharacterAnimations("Player", new Dictionary<string, int>
            {
                { "待机", 20 }, { "走路", 9 }, { "跑步", 9 }, { "跳跃", 8 }, { "攻击", 17 }
            });

            GenerateCharacterAnimations("Enemy", new Dictionary<string, int>
            {
                { "走路", 8 }, { "受击", 6 }
            });

            GenerateCharacterAnimations("Feifei", new Dictionary<string, int>
            {
                { "待机", 10 }
            });

            Log("✅ 步骤3完成: 动画生成完成");
        }

        private void GenerateCharacterAnimations(string characterName, Dictionary<string, int> animations)
        {
            string basePath = $"Assets/Game/Art/Characters/Animations/{characterName}";
            string animOutputPath = $"Assets/Game/Art/Characters/Animations/{characterName}/GeneratedAnims";

            if (!Directory.Exists(Path.Combine(Application.dataPath, animOutputPath.Substring(7))))
            {
                Directory.CreateDirectory(Path.Combine(Application.dataPath, animOutputPath.Substring(7)));
            }

            foreach (var anim in animations)
            {
                string animName = anim.Key;
                int expectedFrames = anim.Value;

                string animFolderPath = Path.Combine(basePath, animName);
                if (!Directory.Exists(Path.Combine(Application.dataPath, animFolderPath.Substring(7))))
                {
                    Log($"  ⚠️ 动画目录不存在: {animFolderPath}");
                    continue;
                }

                // 查找所有PNG文件并排序
                string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { animFolderPath });
                List<Sprite> sprites = new List<Sprite>();

                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sprite != null) sprites.Add(sprite);
                }

                // 按文件名数字排序
                sprites = sprites.OrderBy(s =>
                {
                    string name = s.name;
                    string numStr = new string(name.Where(char.IsDigit).ToArray());
                    return int.TryParse(numStr, out int num) ? num : 0;
                }).ToList();

                if (sprites.Count == 0)
                {
                    Log($"  ⚠️ 没有找到帧: {animName}");
                    continue;
                }

                // 创建Animation Clip
                AnimationClip clip = new AnimationClip();
                clip.frameRate = animName == "待机" ? 12 : (animName == "走路" ? 15 : 20);

                EditorCurveBinding spriteBinding = new EditorCurveBinding
                {
                    type = typeof(SpriteRenderer),
                    path = "",
                    propertyName = "m_Sprite"
                };

                ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Count];
                for (int i = 0; i < sprites.Count; i++)
                {
                    keyframes[i] = new ObjectReferenceKeyframe
                    {
                        time = i / clip.frameRate,
                        value = sprites[i]
                    };
                }

                AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);

                // 循环设置
                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = (animName == "待机" || animName == "走路" || animName == "跑步");
                AnimationUtility.SetAnimationClipSettings(clip, settings);

                string clipPath = $"{animOutputPath}/{characterName}_{animName}.anim";
                AssetDatabase.CreateAsset(clip, clipPath);

                Log($"  ✓ 生成 {characterName}/{animName}: {sprites.Count} 帧");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        #endregion

        #region Step 4: 链接场景资源

        private void Step4_LinkSceneResources()
        {
            Log("\n[步骤4] 链接场景资源...");
            Log("  ℹ️ 此步骤需要手动完成:");
            Log("  1. 打开 PlayerGuide.unity 场景");
            Log("  2. 将生成的 Animator Controller 拖到 player 对象的 Animator 组件");
            Log("  3. 将对应的 Sprite 拖到 SpriteRenderer 组件");
            Log("  4. 配置对话框使用 dialogues.png 的切分Sprite");
            Log("✅ 步骤4提示完成");
        }

        #endregion

        private void Log(string message)
        {
            statusLog += message + "\n";
            Debug.Log(message);
            Repaint();
        }
    }
}
