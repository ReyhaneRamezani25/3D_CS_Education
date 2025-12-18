using UnityEngine;
using UnityEngine.UI;

public class ToggleSubPanel : MonoBehaviour
{
    [Header("Panel")]
    public GameObject subPanel;

    [Header("Root")]
    public RectTransform contentRoot;

    private bool isExpanded;

    public void Toggle()
    {
        if (subPanel == null) return;

        isExpanded = !isExpanded;
        subPanel.SetActive(isExpanded);

        Canvas.ForceUpdateCanvases();
        if (contentRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
        }
    }
}
