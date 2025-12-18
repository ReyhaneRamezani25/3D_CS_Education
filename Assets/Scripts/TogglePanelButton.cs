using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class AccordionItem : MonoBehaviour
{
    [Header("Assign if you have them; otherwise I will auto-find")]
    [SerializeField] Button headerButton;
    [SerializeField] RectTransform subPanel;
    [SerializeField] RectTransform scrollContent;

    RectTransform itemRT;
    bool isOpen = false;

    void Awake()
    {
        itemRT = GetComponent<RectTransform>();

        if (!headerButton)
        {
            headerButton = GetComponentInChildren<Button>(true);
            if (!headerButton) Debug.LogError($"[AccordionItem] No Button found under '{name}'. Add a Button component to the header object.");
        }
        if (!subPanel)
        {
            var t = transform.Find("SubPanel");
            if (t) subPanel = t as RectTransform;
        }
        if (!subPanel) Debug.LogError($"[AccordionItem] SubPanel not assigned under '{name}'. Create a child RectTransform named 'SubPanel' for sub buttons.");

        if (!scrollContent)
        {
            var vg = GetComponentInParent<VerticalLayoutGroup>();
            if (vg) scrollContent = vg.GetComponent<RectTransform>();
        }
        if (!scrollContent) Debug.LogWarning($"[AccordionItem] scrollContent not set on '{name}'. Set it to ScrollView/Viewport/Content for proper layout updates.");

        if (subPanel) subPanel.gameObject.SetActive(false);

        if (headerButton)
        {
            headerButton.onClick.AddListener(() =>
            {
                Debug.Log($"[AccordionItem] Click on '{name}'");
                Toggle();
            });
        }
    }

    public void Toggle()
    {
        if (!subPanel) return;

        isOpen = !isOpen;
        subPanel.gameObject.SetActive(isOpen);
        StartCoroutine(RebuildNextFrame());
    }

    IEnumerator RebuildNextFrame()
    {
        yield return null;
        ForceRebuild();
    }

    void ForceRebuild()
    {
        if (subPanel) LayoutRebuilder.ForceRebuildLayoutImmediate(subPanel);
        if (itemRT) LayoutRebuilder.ForceRebuildLayoutImmediate(itemRT);
        if (scrollContent) LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent);
        Canvas.ForceUpdateCanvases();
    }
}
