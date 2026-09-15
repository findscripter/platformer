#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>Reproducible Windows demo build; the resulting Player can run its opt-in AI self-test.</summary>
public static class DreamDemoHandoffBuild
{
    const string PendingKey = "Dreamremains.HandoffBuild.Pending";
    const string ReportPath = "QA/demo-handoff-20260915/windows-build-result.json";
    static double idleSince = -1;
    static double reloadIdleSince = -1;
    static readonly string[] Scenes = {
        "Assets/Game/Scenes/Boot.unity", "Assets/Game/Scenes/MainMenu.unity",
        "Assets/Game/Scenes/PlayerGuide.unity", "Assets/Game/Scenes/Loading.unity",
        "Assets/Game/Scenes/Gameplay.unity"
    };
    static readonly string[] TextResources = {
        "DreamLetter/connection", "DreamLetter/analyze_system", "DreamLetter/letter_system",
        "DreamLetter/letter_sparse_system", "DreamLetter/letter_ending_system",
        "DreamLetter/card_lenses", "DreamLetter/tarot_reference", "DreamLetter/local_sample_lenses"
    };

    [MenuItem("Tools/梦墟/交接验收/构建 Windows Demo")]
    public static void BuildWindowsDemo()
    {
        if (!string.IsNullOrEmpty(SessionState.GetString(PendingKey, "")))
        {
            Debug.LogWarning("[DemoHandoffBuild] A build is already queued; waiting for script reload/import to finish.");
            return;
        }
        string output = Argument("-dreamBuildOutput") ?? "Builds/Handoff-Windows/Dreamremains.exe";
        output = Path.GetFullPath(output);
        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        var reportJson = new JObject { ["utc"] = DateTime.UtcNow.ToString("O"), ["unity_version"] = Application.unityVersion,
            ["target"] = "StandaloneWindows64", ["scripting_backend"] = "Mono", ["managed_stripping"] = "Disabled",
            ["development_build"] = false, ["output"] = output, ["scenes"] = new JArray(Scenes) };
        var target = NamedBuildTarget.Standalone;
        var request = new JObject { ["phase"] = "waiting_reload", ["queued_utc"] = DateTime.UtcNow.ToString("O"),
            ["old_backend"] = (int)PlayerSettings.GetScriptingBackend(target),
            ["old_stripping"] = (int)PlayerSettings.GetManagedStrippingLevel(target),
            ["old_product"] = PlayerSettings.productName, ["report"] = reportJson };
        try
        {
            ValidateInputs(output);
            // Persist before a setter can request domain reload. Do not re-apply these
            // setters when resuming: even assigning an unchanged backend can queue reload.
            SessionState.SetString(PendingKey, request.ToString());
            if (PlayerSettings.GetScriptingBackend(target) != ScriptingImplementation.Mono2x)
                PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.Mono2x);
            if (PlayerSettings.GetManagedStrippingLevel(target) != ManagedStrippingLevel.Disabled)
                PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.Disabled);
            // Give the distributed demo its own save directory, separate from both development projects.
            if (PlayerSettings.productName != "Dreamremains Demo") PlayerSettings.productName = "Dreamremains Demo";
            AssetDatabase.SaveAssets();
            reportJson["result"] = "WaitingForScriptReload";
            File.WriteAllText(ReportPath, reportJson.ToString());
            Debug.Log("[DemoHandoffBuild] Build settings saved. Reloading scripts once before starting Windows build.");
            reloadIdleSince = -1;
            EditorApplication.update += CheckReloadWasRequested;
            // BuildPlayer cannot run in the same update that changed compilation settings.
            // ResumeAfterReload is the only path to the actual build.
            EditorUtility.RequestScriptReload();
        }
        catch (Exception error) { Complete(request, error); }
    }

    static void CheckReloadWasRequested()
    {
        // This callback is lost on the requested reload. If Unity remains idle without
        // performing it, finish with an actionable error instead of leaving batchmode hung.
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) { reloadIdleSince = -1; return; }
        if (reloadIdleSince < 0) { reloadIdleSince = EditorApplication.timeSinceStartup; return; }
        if (EditorApplication.timeSinceStartup - reloadIdleSince < 10) return;
        string serialized = SessionState.GetString(PendingKey, "");
        EditorApplication.update -= CheckReloadWasRequested;
        if (!string.IsNullOrEmpty(serialized)) Complete(JObject.Parse(serialized),
            new BuildFailedException("Unity stayed idle without completing the requested script reload. Check for a reload lock, then retry."));
    }

    [InitializeOnLoadMethod]
    static void ResumeAfterReload()
    {
        string serialized = SessionState.GetString(PendingKey, "");
        if (string.IsNullOrEmpty(serialized)) return;
        var request = JObject.Parse(serialized);
        if (request["phase"]?.Value<string>() == "building")
        {
            // A reload during BuildPlayer must not dispatch the same build twice.
            EditorApplication.delayCall += () => Complete(request,
                new BuildFailedException("Scripts reloaded while BuildPlayer was already running; build stopped safely."));
            return;
        }
        EditorApplication.delayCall += QueueWhenReady;
    }

    static void QueueWhenReady()
    {
        idleSince = -1;
        EditorApplication.update -= WaitForReady;
        EditorApplication.update -= CheckReloadWasRequested;
        EditorApplication.update += WaitForReady;
    }

    static void WaitForReady()
    {
        string serialized = SessionState.GetString(PendingKey, "");
        if (string.IsNullOrEmpty(serialized)) { EditorApplication.update -= WaitForReady; return; }
        JObject request = JObject.Parse(serialized);
        if (DateTime.UtcNow - DateTime.Parse(request["queued_utc"].Value<string>(), null,
            System.Globalization.DateTimeStyles.RoundtripKind) > TimeSpan.FromMinutes(10))
        {
            Complete(request, new BuildFailedException("Timed out waiting for Unity compilation/import before building."));
            return;
        }
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode
            || BuildPipeline.isBuildingPlayer) { idleSince = -1; return; }
        if (idleSince < 0) { idleSince = EditorApplication.timeSinceStartup; return; }
        if (EditorApplication.timeSinceStartup - idleSince < 1) return;
        EditorApplication.update -= WaitForReady;
        // Never run the build from the settings setter's stack or assembly-load callback.
        EditorApplication.delayCall += BuildAfterReload;
    }

    static void BuildAfterReload()
    {
        string serialized = SessionState.GetString(PendingKey, "");
        if (string.IsNullOrEmpty(serialized)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) { QueueWhenReady(); return; }
        JObject request = JObject.Parse(serialized);
        if (request["phase"]?.Value<string>() == "building") return;
        var reportJson = (JObject)request["report"];
        string output = reportJson["output"].Value<string>();
        try
        {
            ValidateInputs(output);
            var target = NamedBuildTarget.Standalone;
            if (PlayerSettings.GetScriptingBackend(target) != ScriptingImplementation.Mono2x
                || PlayerSettings.GetManagedStrippingLevel(target) != ManagedStrippingLevel.Disabled
                || PlayerSettings.productName != "Dreamremains Demo")
                throw new BuildFailedException("Build settings changed while the build was queued. Start a fresh build.");
            request["phase"] = "building";
            SessionState.SetString(PendingKey, request.ToString());
            reportJson["domain_reload_completed"] = true;
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = Scenes, locationPathName = output, target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.CompressWithLz4 | BuildOptions.DetailedBuildReport
            });
            reportJson["result"] = report.summary.result.ToString();
            reportJson["errors"] = report.summary.totalErrors;
            reportJson["warnings"] = report.summary.totalWarnings;
            reportJson["seconds"] = report.summary.totalTime.TotalSeconds;
            reportJson["bytes"] = report.summary.totalSize;
            reportJson["product_name"] = PlayerSettings.productName;
            reportJson["embedded_primary_present"] = true;
            reportJson["embedded_backup_present"] = true;
            if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException("Windows Demo build failed. Read Unity's build log.");
            Debug.Log("[DemoHandoffBuild] Windows Player ready: " + output);
            Complete(request, null);
        }
        catch (Exception error) { Complete(request, error); }
    }

    static void Complete(JObject request, Exception error)
    {
        EditorApplication.update -= WaitForReady;
        EditorApplication.delayCall -= BuildAfterReload;
        SessionState.EraseString(PendingKey);
        var reportJson = (JObject)request["report"];
        int exitCode = error == null ? 0 : 1;
        if (error != null)
        {
            reportJson["result"] = "Failed";
            reportJson["error"] = error.GetType().Name + ": " + error.Message;
            Debug.LogError("[DemoHandoffBuild] " + error.GetType().Name + ": " + error.Message);
        }
        try
        {
            var target = NamedBuildTarget.Standalone;
            var oldBackend = (ScriptingImplementation)request["old_backend"].Value<int>();
            var oldStripping = (ManagedStrippingLevel)request["old_stripping"].Value<int>();
            if (PlayerSettings.GetScriptingBackend(target) != oldBackend) PlayerSettings.SetScriptingBackend(target, oldBackend);
            if (PlayerSettings.GetManagedStrippingLevel(target) != oldStripping) PlayerSettings.SetManagedStrippingLevel(target, oldStripping);
            if (PlayerSettings.productName != request["old_product"].Value<string>()) PlayerSettings.productName = request["old_product"].Value<string>();
            AssetDatabase.SaveAssets();
            reportJson["project_settings_restored"] = true;
        }
        catch (Exception restoreError)
        {
            exitCode = 1;
            reportJson["restore_error"] = restoreError.GetType().Name + ": " + restoreError.Message;
        }
        File.WriteAllText(ReportPath, reportJson.ToString());
        if (Application.isBatchMode) EditorApplication.Exit(exitCode);
        if (exitCode != 0) Debug.LogError("Windows Demo build failed. Read " + ReportPath + ".");
    }

    static void ValidateInputs(string output)
    {
        if (!output.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) throw new BuildFailedException("Output must end in .exe.");
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
            throw new BuildFailedException("Windows build support is not installed.");
        if (!EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).SequenceEqual(Scenes))
            throw new BuildFailedException("Scene order must be Boot / MainMenu / PlayerGuide / Loading / Gameplay.");
        foreach (string scene in Scenes) if (!File.Exists(scene)) throw new BuildFailedException("Missing build scene: " + scene);
        foreach (string path in TextResources)
        {
            var resource = Resources.Load<TextAsset>(path);
            if (resource == null || string.IsNullOrWhiteSpace(resource.text)) throw new BuildFailedException("Missing embedded resource: " + path);
        }
        if (!DreamLetterClient.HasEmbeddedApiKey) throw new BuildFailedException("Embedded primary key is missing. Save it in the connection settings window.");
        if (DreamLetterClient.GetBackupProvider()?.Ready != true) throw new BuildFailedException("Embedded backup provider is not ready.");
        if (TarotCatalog.LoadDeck().Length != 22) throw new BuildFailedException("Expected 22 embedded tarot card assets.");
    }

    static string Argument(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}
#endif
