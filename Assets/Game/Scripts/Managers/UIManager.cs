using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private GameObject resultPanel;

    public void ShowMainMenu()
    {
        HideAll();

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }

    public void ShowGameplayUI()
    {
        HideAll();

        if (gameplayPanel != null)
            gameplayPanel.SetActive(true);
    }

    public void ShowPauseMenu()
    {
        if (pausePanel != null)
            pausePanel.SetActive(true);
    }

    public void HidePauseMenu()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    public void ShowDeathUI()
    {
        if (deathPanel != null)
            deathPanel.SetActive(true);
    }

    public void HideDeathUI()
    {
        if (deathPanel != null)
            deathPanel.SetActive(false);
    }

    public void ShowResult()
    {
        HideAll();

        if (resultPanel != null)
            resultPanel.SetActive(true);
    }

    public void HideAll()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (gameplayPanel != null)
            gameplayPanel.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (deathPanel != null)
            deathPanel.SetActive(false);

        if (resultPanel != null)
            resultPanel.SetActive(false);
    }
}
