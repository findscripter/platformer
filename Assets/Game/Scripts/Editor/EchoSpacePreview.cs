#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class EchoSpacePreview
{
    private const string RequestPath = "Temp/preview_echo_dialogue.request";

    private static bool hooked;

    [InitializeOnLoadMethod]
    private static void WatchRequest()
    {
        if (hooked)
            return;
        hooked = true;
        EditorApplication.update += Tick;
    }

    [MenuItem("Tools/梦墟/预览回响对白")]
    public static void PreviewFromMenu()
    {
        QueuePreview();
    }

    public static void QueuePreview()
    {
        Directory.CreateDirectory("Temp");
        File.WriteAllText(RequestPath, "1");
    }

    private static void Tick()
    {
        if (!File.Exists(RequestPath))
            return;

        try
        {
            File.Delete(RequestPath);
        }
        catch
        {
            return;
        }

        EditorPrefs.SetBool(EchoSpaceController.PreviewFlag, true);
        if (EditorApplication.isPlaying)
        {
            var echo = EchoSpaceController.Ensure();
            echo.StartCoroutine(PreviewPlaying(echo));
        }
        else
        {
            EditorApplication.isPlaying = true;
        }
    }

    private static System.Collections.IEnumerator PreviewPlaying(EchoSpaceController echo)
    {
        echo.PreviewLastDialogue();
        yield return new WaitForEndOfFrame();
        yield return new WaitForSecondsRealtime(0.2f);
        yield return new WaitForEndOfFrame();
        if (File.Exists(EchoSpaceController.PreviewShotPath))
            File.Delete(EchoSpaceController.PreviewShotPath);
        ScreenCapture.CaptureScreenshot(EchoSpaceController.PreviewShotPath);
        yield return new WaitForSecondsRealtime(0.6f);
        File.WriteAllText(EchoSpaceController.PreviewDonePath, EchoSpaceController.PreviewShotPath);
    }
}
#endif
