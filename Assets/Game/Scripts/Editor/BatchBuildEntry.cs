using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class BatchBuildEntry
{
    public static void BuildWindows()
    {
        var scenes = new List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
            if (scene.enabled) scenes.Add(scene.path);

        string output = "Builds/FeedbackFix-Windows/Dreamremains.exe";
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes.ToArray(),
            locationPathName = output,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development | BuildOptions.CompressWithLz4
        });

        var summary = report.summary;
        File.WriteAllText("production/qa/build-result.json",
            "{\"result\":\"" + summary.result + "\",\"errors\":" + summary.totalErrors +
            ",\"warnings\":" + summary.totalWarnings + ",\"seconds\":" +
            summary.totalTime.TotalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture) +
            ",\"output\":\"" + output.Replace("\\", "/") + "\",\"bytes\":" + summary.totalSize + "}");

        if (summary.result != BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }
}
