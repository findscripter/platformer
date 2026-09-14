using UnityEditor;
using UnityEngine;

/// <summary>
/// 循环 BGM：压缩进内存、后台加载、保留采样率，保证循环播放不断流。
/// </summary>
public sealed class MenuBgmImportProcessor : AssetPostprocessor
{
    private void OnPreprocessAudio()
    {
        string path = assetPath.Replace('\\', '/');
        if (!path.EndsWith("/game_bgm.mp3"))
        {
            return;
        }

        var importer = (AudioImporter)assetImporter;
        importer.forceToMono = false;
        importer.loadInBackground = true;

        var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = 0.8f;
        settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
        settings.preloadAudioData = true;
        importer.defaultSampleSettings = settings;
    }
}
