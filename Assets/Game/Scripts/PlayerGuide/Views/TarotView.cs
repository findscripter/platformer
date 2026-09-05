using System.Collections;
using UnityEngine;

/// <summary>
/// Frame 9 塔罗面板视图包装。
/// 忠实复刻 PlayerGuideFlowControllerV2 中 tarotPanel / tarotController 的调用逻辑。
/// </summary>
public class TarotView : MonoBehaviour
{
    [SerializeField] private GameObject tarotPanel;
    [SerializeField] private TarotDrawController tarotController;

    public void SetPanelActive(bool active)
    {
        if (tarotPanel != null)
        {
            tarotPanel.SetActive(active);
        }
    }

    public bool HasController => tarotController != null;

    public IEnumerator StartTarotFlow()
    {
        if (tarotController != null)
        {
            yield return tarotController.StartTarotFlow();
        }
    }
}
