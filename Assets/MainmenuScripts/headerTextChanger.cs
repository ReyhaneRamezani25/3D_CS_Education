using UnityEngine;
using TMPro;
using ArabicSupport;

public class headerTextChanger : MonoBehaviour
{
    public TextMeshProUGUI targetText;

    public bool showTashkeel = false;
    public bool useHinduNumbers = true;

    void Start()
    {
        if (targetText != null)
        {
            string originalText = targetText.text;


            string fixedText = ArabicFixer.Fix(originalText, showTashkeel, useHinduNumbers);
            targetText.text = fixedText;

        }
        else
        {
            Debug.LogError("error for TMP fix");
        }
    }
}


