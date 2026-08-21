using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace DreamGame.Editor
{
    /// <summary>
    /// 美术资源自动导入工具
    /// 批量处理E:\腾讯gamejam\美术资源下的141个文件
    /// </summary>
    public class ArtAssetImporter : EditorWindow
    {
        private string sourceDirectory = @"E:\腾讯gamejam\美术资源";
        private string targetDirectory = "Assets/Art";
        private bool overwriteExisting = false;
        private Vector2 scrollPosition;
        private List<string> importLog = new List<string>();

        [MenuItem("DreamGame/美术资源导入工具")]
        public static void ShowWindow()
        {
            GetWindow<ArtAssetImporter>("美术资源导入");
        }

        private void OnGUI()
        {
            GUILayout.Label("美术资源批量导入", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            sourceDirectory = EditorGUILayout.TextField("源目录:", sourceDirectory);
            targetDirectory = EditorGUILayout.TextField("目标目录:", targetDirectory);
            overwriteExisting = EditorGUILayout.Toggle("覆盖已存在文件", overwriteExisting);

            EditorGUILayout.Space();

            if (GUILayout.Button("开始导入", GUILayout.Height(30)))
            {
                ImportAssets();
            }

            if (GUILayout.Button("清空日志"))
            {
                importLog.Clear();
            }

            EditorGUILayout.Space();
            GUILayout.Label("导入日志:", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
            foreach (var log in importLog)
            {
                EditorGUILayout.LabelField(log);
            }
            EditorGUILayout.EndScrollView();
        }

        private void ImportAssets()
        {
            importLog.Clear();
            AddLog("开始导入美术资源...");

            if (!Directory.Exists(sourceDirectory))
            {
                AddLog($"错误: 源目录不存在: {sourceDirectory}");
                return;
            }

            // 创建目标目录结构
            CreateDirectoryStructure();

            // 获取所有文件
            string[] allFiles = Directory.GetFiles(sourceDirectory, "*.*", SearchOption.AllDirectories);
            AddLog($"找到 {allFiles.Length} 个文件");

            int successCount = 0;
            int skipCount = 0;
            int errorCount = 0;

            foreach (string filePath in allFiles)
            {
                string extension = Path.GetExtension(filePath).ToLower();

                // 只处理图片文件
                if (extension == ".png" || extension == ".jpg" || extension == ".jpeg")
                {
                    try
                    {
                        string result = ImportSingleAsset(filePath);
                        if (result == "success")
                            successCount++;
                        else if (result == "skip")
                            skipCount++;
                        else
                            errorCount++;
                    }
                    catch (System.Exception e)
                    {
                        AddLog($"错误: {Path.GetFileName(filePath)} - {e.Message}");
                        errorCount++;
                    }
                }
            }

            AddLog($"\n导入完成!");
            AddLog($"成功: {successCount}, 跳过: {skipCount}, 错误: {errorCount}");

            AssetDatabase.Refresh();
            AddLog("资源数据库已刷新");
        }

        private void CreateDirectoryStructure()
        {
            string[] directories = new string[]
            {
                targetDirectory,
                $"{targetDirectory}/Characters",
                $"{targetDirectory}/UI",
                $"{targetDirectory}/Effects",
                $"{targetDirectory}/Backgrounds",
                $"{targetDirectory}/Props",
                $"{targetDirectory}/Tarot",
                $"{targetDirectory}/Dialogues"
            };

            foreach (string dir in directories)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                    AddLog($"创建目录: {dir}");
                }
            }
        }

        private string ImportSingleAsset(string sourcePath)
        {
            string fileName = Path.GetFileName(sourcePath);
            string subDir = DetermineSubDirectory(fileName);
            string targetPath = Path.Combine(targetDirectory, subDir, fileName);

            // 检查是否已存在
            if (File.Exists(targetPath) && !overwriteExisting)
            {
                AddLog($"跳过: {fileName} (已存在)");
                return "skip";
            }

            // 复制文件
            File.Copy(sourcePath, targetPath, overwriteExisting);
            AddLog($"导入: {subDir}/{fileName}");

            return "success";
        }

        private string DetermineSubDirectory(string fileName)
        {
            string lowerName = fileName.ToLower();

            // 根据文件名判断分类
            if (lowerName.Contains("腓腓") || lowerName.Contains("phiphi") || lowerName.Contains("角色"))
                return "Characters";
            else if (lowerName.Contains("对话框") || lowerName.Contains("dialogue") || lowerName.Contains("文本框"))
                return "Dialogues";
            else if (lowerName.Contains("塔罗") || lowerName.Contains("tarot") || lowerName.Contains("卡牌"))
                return "Tarot";
            else if (lowerName.Contains("按钮") || lowerName.Contains("button") || lowerName.Contains("ui"))
                return "UI";
            else if (lowerName.Contains("特效") || lowerName.Contains("effect") || lowerName.Contains("vfx"))
                return "Effects";
            else if (lowerName.Contains("背景") || lowerName.Contains("background") || lowerName.Contains("bg"))
                return "Backgrounds";
            else if (lowerName.Contains("道具") || lowerName.Contains("prop") || lowerName.Contains("item"))
                return "Props";
            else
                return ""; // 根目录
        }

        private void AddLog(string message)
        {
            importLog.Add($"[{System.DateTime.Now:HH:mm:ss}] {message}");
            Debug.Log(message);
        }
    }
}
