#if UNITY_EDITOR
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class DreamLetterProxyLauncher
{
    [MenuItem("Tools/梦墟/启动梦笺代理")]
    public static void StartProxy()
    {
        string dir = Path.GetFullPath("Tools/DreamLetterProxy");
        string env = Path.Combine(dir, ".env");
        if (!File.Exists(env))
        {
            UnityEngine.Debug.LogError("缺少 Tools/DreamLetterProxy/.env，先复制 .env.example 并填入 DEEPSEEK_API_KEY。");
            return;
        }

        var psi = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = "server.py",
            WorkingDirectory = dir,
            UseShellExecute = true
        };
        Process.Start(psi);
        UnityEngine.Debug.Log("已启动梦笺代理 http://127.0.0.1:8787");
    }
}
#endif
