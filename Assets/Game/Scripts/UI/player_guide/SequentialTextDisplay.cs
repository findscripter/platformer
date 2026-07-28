using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SequentialTextDisplay : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private SequentialTextConfig config;
    [SerializeField] private TMP_Text targetText;
    [SerializeField] private UnityEvent onSequenceCompleted;

    private int currentIndex;
    private Button cachedButton;
    private bool advanceEnabled = true;

    public int CurrentIndex => currentIndex;
    public event Action SequenceCompleted;

    private void Awake()
    {
        if (targetText == null)
            targetText = GetComponentInChildren<TMP_Text>(true);

        cachedButton = GetComponent<Button>();
        if (cachedButton != null)
            cachedButton.onClick.AddListener(Advance);
    }

    private void OnDestroy()
    {
        if (cachedButton != null)
            cachedButton.onClick.RemoveListener(Advance);
    }

    private void OnEnable()
    {
        currentIndex = 0;
        RefreshText();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (cachedButton != null || !advanceEnabled)
            return;

        Advance();
    }

    public void SetConfig(SequentialTextConfig newConfig, bool reset = true)
    {
        config = newConfig;

        if (reset)
            ResetSequence();
        else
            RefreshText();
    }

    public void SetAdvanceEnabled(bool enabled)
    {
        advanceEnabled = enabled;

        if (cachedButton == null)
            cachedButton = GetComponent<Button>();

        if (cachedButton != null)
            cachedButton.interactable = enabled;
    }

    public void EnsureClickBinding()
    {
        if (cachedButton == null)
            cachedButton = GetComponent<Button>();

        if (cachedButton != null)
        {
            cachedButton.onClick.RemoveListener(Advance);
            cachedButton.onClick.AddListener(Advance);
            cachedButton.interactable = advanceEnabled;
        }
    }

    public void Advance()
    {
        if (!advanceEnabled)
            return;

        if (config == null || config.lines == null || config.lines.Length == 0)
        {
            CompleteSequence();
            return;
        }

        if (currentIndex >= config.lines.Length - 1)
        {
            CompleteSequence();
            return;
        }

        currentIndex++;
        RefreshText();
    }

    public void ResetSequence()
    {
        currentIndex = 0;
        RefreshText();
    }

    private void RefreshText()
    {
        if (targetText == null || config?.lines == null || config.lines.Length == 0)
            return;

        currentIndex = Mathf.Clamp(currentIndex, 0, config.lines.Length - 1);
        targetText.text = config.lines[currentIndex] ?? string.Empty;
    }

    private void CompleteSequence()
    {
        onSequenceCompleted?.Invoke();
        SequenceCompleted?.Invoke();
    }
}
