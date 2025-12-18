using UnityEngine;
using UnityEngine.UI;
using TMPro;  // فقط اگر از TextMeshPro استفاده می‌کنی

public class PseudocodeToggle : MonoBehaviour
{
    public Button pseudoButton;
    public GameObject pseudoText;   // شیء متنی که می‌خواهی نشان/مخفی کنی

    private bool isVisible = false;

    void Start()
    {
        
        pseudoText.SetActive(false);

        
        pseudoButton.onClick.AddListener(ToggleText);
    }

    void ToggleText()
    {
        isVisible = !isVisible; 
        pseudoText.SetActive(isVisible);
    }
}
