using UnityEngine;

public class MainMenuUIController : MonoBehaviour
{
    public void StartGame()
    {
        if (GameLoop.Instance == null)
        {
            Debug.LogError("MainMenuUIController: GameLoop is unavailable.");
            return;
        }

        GameLoop.Instance.StartNewGame();
    }

    public void QuitGame()
    {
        if (GameLoop.Instance != null)
        {
            GameLoop.Instance.QuitGame();
            return;
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
