using UnityEngine;
using UnityEngine.UI;

public class PlayerGuideUIController : MonoBehaviour
{
    [SerializeField] private GameObject[] pages;
    [SerializeField] private Text pageIndicatorText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button skipButton;
    [SerializeField] private Text nextButtonLabel;

    private int currentPageIndex;

    private void Awake()
    {
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextClicked);

        if (skipButton != null)
            skipButton.onClick.AddListener(ContinueToGameplay);

        ShowPage(0);
    }

    public void OnNextClicked()
    {
        if (pages == null || pages.Length == 0)
        {
            ContinueToGameplay();
            return;
        }

        if (currentPageIndex >= pages.Length - 1)
        {
            ContinueToGameplay();
            return;
        }

        ShowPage(currentPageIndex + 1);
    }

    public void ContinueToGameplay()
    {
        if (GameLoop.Instance == null)
        {
            Debug.LogError("PlayerGuideUIController: GameLoop is unavailable.");
            return;
        }

        GameLoop.Instance.ContinueToGameplay();
    }

    private void ShowPage(int pageIndex)
    {
        currentPageIndex = Mathf.Clamp(pageIndex, 0, Mathf.Max(0, pages.Length - 1));

        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null)
                pages[i].SetActive(i == currentPageIndex);
        }

        if (pageIndicatorText != null && pages.Length > 0)
            pageIndicatorText.text = $"{currentPageIndex + 1} / {pages.Length}";

        if (nextButtonLabel != null)
        {
            nextButtonLabel.text = currentPageIndex >= pages.Length - 1 ? "进入游戏" : "下一步";
        }
    }
}
