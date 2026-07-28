using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }

    private const string SaveFileName = "game_save.json";

    [SerializeField] private DialogueProgressService dialogueProgressService;

    private GameSaveData currentData = new();
    private string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public GameSaveData CurrentData => currentData;
    public bool HasSaveFile => File.Exists(SaveFilePath);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (dialogueProgressService == null)
            dialogueProgressService = GetComponent<DialogueProgressService>();
    }

    public void CaptureCurrentState(GameContext context)
    {
        if (context == null)
            return;

        currentData.currentScene = ActiveSceneHelper.GetActiveSceneName();
        dialogueProgressService?.CaptureToSaveData(currentData, context);
    }

    public bool Save(GameContext context)
    {
        CaptureCurrentState(context);

        try
        {
            string json = JsonUtility.ToJson(currentData, true);
            File.WriteAllText(SaveFilePath, json);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"SaveSystem: failed to save. {exception.Message}");
            return false;
        }
    }

    public bool Load()
    {
        if (!HasSaveFile)
            return false;

        try
        {
            string json = File.ReadAllText(SaveFilePath);
            currentData = JsonUtility.FromJson<GameSaveData>(json) ?? new GameSaveData();
            if (currentData.dialogue == null)
                currentData.dialogue = new DialogueSaveData();
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"SaveSystem: failed to load. {exception.Message}");
            currentData = new GameSaveData();
            return false;
        }
    }

    public void ApplyLoadedState(GameContext context)
    {
        dialogueProgressService?.ApplyFromSaveData(currentData, context);
    }

    public void ClearSave()
    {
        currentData = new GameSaveData();
        if (HasSaveFile)
            File.Delete(SaveFilePath);
    }
}

internal static class ActiveSceneHelper
{
    public static string GetActiveSceneName()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        return scene.IsValid() ? scene.name : string.Empty;
    }
}
