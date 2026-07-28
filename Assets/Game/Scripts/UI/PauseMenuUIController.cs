using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class PauseMenuUIController : MonoBehaviour
{
    [SerializeField] private Button resumeButton;

    private GameObject runtimeEventSystem;

    private void OnEnable()
    {
        EnsureEventSystem();

        if (resumeButton != null)
        {
            EventSystem.current?.SetSelectedGameObject(resumeButton.gameObject);
        }
    }

    private void OnDisable()
    {
        if (runtimeEventSystem != null)
        {
            Destroy(runtimeEventSystem);
            runtimeEventSystem = null;
        }
    }

    public void ResumeGame()
    {
        GameLoop.Instance?.ResumeGame();
    }

    public void ReturnToMainMenu()
    {
        GameLoop.Instance?.ReturnToMainMenu();
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        runtimeEventSystem = new GameObject(
            "PauseEventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule));

        runtimeEventSystem
            .GetComponent<InputSystemUIInputModule>()
            .AssignDefaultActions();
    }
}
