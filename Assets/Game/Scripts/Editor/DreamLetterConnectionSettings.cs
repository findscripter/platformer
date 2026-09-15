#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using System.Threading.Tasks;

public sealed class DreamLetterConnectionSettings : EditorWindow
{
    public const string SettingsAssetPath = "Assets/Game/Resources/DreamLetter/connection.json";
    private string newKey = "";
    private string backupKey = "";
    private string backupEndpoint = "";
    private string backupModel = "";
    private int backupProtocol;
    private string status;
    private Task<DreamLetterHttp.Response> connectionTest;
    private double testStarted;
    private bool testAnthropic;
    private bool testResponses;
    private string testProvider;

    [MenuItem("Tools/梦墟/梦笺连接设置")]
    public static void Open() => GetWindow<DreamLetterConnectionSettings>("梦笺内置 Key");

    private void OnEnable()
    {
        minSize = new Vector2(560, 680);
        if (!File.Exists(SettingsAssetPath)) return;
        try
        {
            JObject settings = JObject.Parse(File.ReadAllText(SettingsAssetPath));
            newKey = settings["apiKey"]?.Value<string>() ?? "";
            var backup = settings["backup"] as JObject;
            backupKey = backup?["apiKey"]?.Value<string>() ?? "";
            backupEndpoint = backup?["endpoint"]?.Value<string>() ?? "";
            backupModel = backup?["model"]?.Value<string>() ?? "";
            backupProtocol = backup?["protocol"]?.Value<string>() == "responses" ? 2 : backup?["protocol"]?.Value<string>() == "anthropic" ? 1 : 0;
        }
        catch (JsonException) { status = "内置配置格式有误，可重新填写并保存。"; }
    }

    private void OnDisable() => FinishTest();

    private void OnGUI()
    {
        EditorGUILayout.LabelField("参赛版 · 游戏内置 Key", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("在这里填写一次并保存，Unity 测试与打包后的游戏都会使用它。评委直接运行游戏即可，无需填写、安装 Unity 或启动代理。", MessageType.Info);
        EditorGUILayout.LabelField("游戏内置 Key", DreamLetterClient.HasEmbeddedApiKey ? "已保存" : "尚未填写");
        EditorGUILayout.LabelField("DeepSeek API Key");
        EditorGUILayout.LabelField("联网模型", DreamLetterClient.ModelName + "（非思考模式）");
        newKey = EditorGUILayout.PasswordField(newKey);
        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("备用 · ltoken", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("DeepSeek 优先；超时、服务异常或回复不完整时，自动向备用模型发送同一份梦境、塔罗牌及行为记录。", MessageType.Info);
        EditorGUILayout.LabelField("备用 API Key");
        backupKey = EditorGUILayout.PasswordField(backupKey);
        backupEndpoint = EditorGUILayout.TextField("完整请求地址", backupEndpoint);
        backupModel = EditorGUILayout.TextField("备用模型名", backupModel);
        backupProtocol = EditorGUILayout.Popup("接口格式", backupProtocol, new[] { "OpenAI Chat Completions", "Anthropic Messages", "OpenAI Responses" });
        using (new EditorGUI.DisabledScope(connectionTest != null))
        {
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(newKey)))
            {
                if (GUILayout.Button("保存到游戏")) SaveKey();
                if (GUILayout.Button("保存并检查 DeepSeek"))
                    if (SaveKey()) StartConnectionTest();
                using (new EditorGUI.DisabledScope(!BackupProvider().Ready))
                    if (GUILayout.Button("保存并检查备用接口"))
                        if (SaveKey()) StartConnectionTest(true);
            }
            using (new EditorGUI.DisabledScope(!DreamLetterClient.HasEmbeddedApiKey))
            {
                if (GUILayout.Button("打包 Windows 参赛版")) BatchBuildEntry.BuildWindows();
            }
        }
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("内置配置资源");
        EditorGUILayout.SelectableLabel(Path.GetFullPath(SettingsAssetPath), GUILayout.Height(38));
        if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, MessageType.Info);
    }

    private bool SaveKey()
    {
        string key = newKey.Trim();
        if (string.IsNullOrWhiteSpace(key) || key.IndexOfAny(new[] { '\r', '\n', '"', '\'' }) >= 0)
        {
            status = "请填写有效的 Key，不包含换行或引号。";
            return false;
        }
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsAssetPath));
            if (backupKey.IndexOfAny(new[] { '\r', '\n', '"', '\'' }) >= 0)
            {
                status = "备用 Key 不应包含换行或引号。";
                return false;
            }
            JObject settings;
            try { settings = File.Exists(SettingsAssetPath) ? JObject.Parse(File.ReadAllText(SettingsAssetPath)) : new JObject(); }
            catch (JsonException) { settings = new JObject(); }
            settings["apiKey"] = key;
            settings["backup"] = new JObject
            {
                ["apiKey"] = backupKey.Trim(), ["endpoint"] = backupEndpoint.Trim(),
                ["model"] = backupModel.Trim(), ["protocol"] = backupProtocol == 2 ? "responses" : backupProtocol == 1 ? "anthropic" : "openai"
            };
            File.WriteAllText(SettingsAssetPath, settings.ToString(Formatting.Indented), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(SettingsAssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            newKey = key;
            status = BackupProvider().Ready ? "主、备用连接已保存到游戏，打包时自动包含。" : "已保存到游戏，打包时自动包含；补齐备用接口资料后可自动切换。";
            return true;
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
        {
            status = "保存失败，请检查工程目录的写入权限。";
            return false;
        }
    }

    private DreamLetterGateway.Provider BackupProvider() => new DreamLetterGateway.Provider("ltoken", backupEndpoint.Trim(), backupModel.Trim(), backupKey.Trim(), backupProtocol == 1, backupProtocol == 2);

    private void StartConnectionTest(bool backup = false)
    {
        FinishTest();
        var provider = backup ? BackupProvider() : new DreamLetterGateway.Provider("deepseek", DreamLetterGateway.PrimaryEndpoint, DreamLetterClient.ModelName, newKey.Trim());
        testAnthropic = provider.Anthropic;
        testResponses = provider.Responses;
        testProvider = backup ? "备用 " + provider.Model : "DeepSeek";
        var payload = DreamLetterGateway.BuildRequest(provider, "Reply with a JSON object.", "Return {\"ok\":true} only.", 0, 128);
        connectionTest = DreamLetterHttp.PostAsync(payload.ToString(Formatting.None), provider.Key, 30, true, provider.Endpoint, provider.Anthropic, provider.Responses);
        testStarted = EditorApplication.timeSinceStartup;
        status = "正在检查 " + testProvider + " 的 Windows HTTPS 连接……";
        EditorApplication.update += PollConnectionTest;
    }

    private void PollConnectionTest()
    {
        if (connectionTest == null) return;
        if (!connectionTest.IsCompleted && EditorApplication.timeSinceStartup - testStarted < 31) return;
        var response = connectionTest.IsCompleted && !connectionTest.IsFaulted && !connectionTest.IsCanceled
            ? connectionTest.Result : new DreamLetterHttp.Response { Error = "timeout" };
        bool valid = false;
        if (response.Success)
        {
            try
            {
                valid = DreamLetterReply.TryRead(response.Body, out _, out _, out _, testAnthropic, testResponses);
            }
            catch (JsonException) { }
        }
        status = valid ? testProvider + " 连接成功，模型已返回内容。可以进入游戏测试梦笺。" :
            "连接检查失败（" + (response.Error ?? "HTTP " + response.StatusCode) + "）。请核对账户及网络后重试。";
        FinishTest();
        Repaint();
    }

    private void FinishTest()
    {
        EditorApplication.update -= PollConnectionTest;
        DreamLetterHttp.Cancel(connectionTest);
        connectionTest = null;
    }
}
#endif
