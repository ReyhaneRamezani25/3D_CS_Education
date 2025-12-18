using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChangeButtonText : MonoBehaviour
{
    public Button myButton;

    void Start()
    {
        if (myButton != null)
        {
            TextMeshProUGUI buttonText = myButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = "Start";
            }
            else
            {
                Debug.LogError("TextMeshProUGUI component not found in the button.");
            }
        }
        else
        {
            Debug.LogError("Button reference is missing.");
        }
    }
}
