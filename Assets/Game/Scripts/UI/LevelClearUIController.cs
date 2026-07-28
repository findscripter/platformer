using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed class LevelClearUIController : MonoBehaviour
{
    [SerializeField] private Button mainMenuButton;

    private GameObject runtimeEventSystem;

    private void OnEnable()
    {
        EnsureEventSystem();

        if (mainMenuButton != null)
        {
            EventSystem.current?.SetSelectedGameObject(mainMenuButton.gameObject);
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

    public void ReturnToMainMenu()
    {
        GameLoop.Instance?.ReturnToMainMenu();
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        runtimeEventSystem = new GameObject(
            "LevelClearEventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule));

        runtimeEventSystem
            .GetComponent<InputSystemUIInputModule>()
            .AssignDefaultActions();
    }
}
