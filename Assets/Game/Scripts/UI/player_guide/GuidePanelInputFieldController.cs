using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GuidePanelInputFieldController : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button confirmButton;
    [SerializeField] private UnityEvent<string> onSubmitted;

    public event Action<string> Submitted;

    private void Awake()
    {
        if (inputField == null)
            inputField = GetComponentInChildren<TMP_InputField>(true);

        if (confirmButton == null)
            confirmButton = transform.Find("guidepanel-confirm-button")?.GetComponent<Button>();

        if (confirmButton != null)
            confirmButton.onClick.AddListener(Submit);

        if (inputField != null)
            inputField.onSubmit.AddListener(OnInputSubmit);
    }

    private void OnDestroy()
    {
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(Submit);

        if (inputField != null)
            inputField.onSubmit.RemoveListener(OnInputSubmit);
    }

    private void OnInputSubmit(string _)
    {
        Submit();
    }

    public void Submit()
    {
        string value = inputField != null ? inputField.text : string.Empty;
        onSubmitted?.Invoke(value);
        Submitted?.Invoke(value);
    }

    public void ClearInput()
    {
        if (inputField != null)
            inputField.text = string.Empty;
    }
}
