using UnityEngine;
using TMPro;
using ArabicSupport;

public class ArabicTMPWatcher : MonoBehaviour
{
    [SerializeField] private TMP_Text targetText;

    public bool showTashkeel = false;
    public bool useHinduNumbers = true;
    public bool fixOnEnable = true;
    public bool fixOnStart = false;
    public bool watchEveryFrame = true;

    private string _lastAppliedFixed;

    private void Reset()
    {
        if (targetText == null) targetText = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        if (fixOnEnable) ApplyFix();
    }

    private void Start()
    {
        if (fixOnStart) ApplyFix();
    }

    private void Update()
    {
        if (!watchEveryFrame || targetText == null) return;
        var current = targetText.text ?? string.Empty;
        if (!string.Equals(current, _lastAppliedFixed))
        {
            ApplyFixFrom(current);
        }
    }

    public void ApplyFix()
    {
        if (targetText == null) return;
        ApplyFixFrom(targetText.text ?? string.Empty);
    }

    public void SetTextRaw(string raw)
    {
        if (targetText == null) return;
        var fixedText = ArabicFixer.Fix(raw ?? string.Empty, showTashkeel, useHinduNumbers);
        _lastAppliedFixed = fixedText;
        targetText.text = fixedText;
    }

    private void ApplyFixFrom(string input)
    {
        var fixedText = ArabicFixer.Fix(input ?? string.Empty, showTashkeel, useHinduNumbers);
        if (!string.Equals(fixedText, targetText.text))
        {
            targetText.text = fixedText;
        }
        _lastAppliedFixed = fixedText;
    }
}
