using UnityEngine;
using TMPro;
using System.Collections;

public class DialogueHighlightParts : MonoBehaviour
{
    [Header("Dialogue")]
    public DialogueVoiceControllerBasic dialogue;
    public int triggerIndex = 1;
    public int hideIndex = -1;

    [Header("Text Target (3D)")]
    public TextMeshPro tmpText3D;
    public TextMesh legacyTextMesh;

    [Header("Content")]
    [TextArea(2, 5)]
    public string baseText = "Insert(head by ref, A: Node pointer, valB)";

    [Header("Highlight Timings (seconds from trigger)")]
    public float highlightAStart = 1f;
    public float highlightADuration = 1.5f;
    public float highlightBStart = 3f;
    public float highlightBDuration = 1.5f;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color highlightColor = Color.red;

    [Header("Visibility")]
    public GameObject textContainer;
    public bool startHidden = true;

    [Header("Extra Text (existing text on component)")]
    public TextMeshPro extraTmpText3D;
    public TextMesh extraLegacyTextMesh;
    public GameObject extraTextContainer;
    public bool extraStartHidden = true;
    public float extraShowTime = 2f;
    public bool extraAutoHide = true;
    public float extraHideTime = 4f;

    [Header("LED")]
    public GameObject ledObject;
    public bool ledStartHidden = true;
    public float ledShowTime = 2.5f;
    public bool ledAutoHide = true;
    public float ledHideTime = 4.5f;

    Coroutine _routineMain;
    Coroutine _routineExtra;
    Coroutine _routineLed;

    void Awake()
    {
        ApplyText(baseText);
        if (startHidden) HideImmediate();
        if (extraStartHidden) HideExtraImmediate();
        if (ledStartHidden) HideLedImmediate();
    }

    void OnEnable()
    {
        if (dialogue != null) dialogue.OnDialogueStart += OnDialogueStart;
        if (startHidden) StartCoroutine(HideNextFrame());
        if (extraStartHidden) StartCoroutine(HideExtraNextFrame());
        if (ledStartHidden) StartCoroutine(HideLedNextFrame());
    }

    void OnDisable()
    {
        if (dialogue != null) dialogue.OnDialogueStart -= OnDialogueStart;
        if (_routineMain != null) StopCoroutine(_routineMain);
        if (_routineExtra != null) StopCoroutine(_routineExtra);
        if (_routineLed != null) StopCoroutine(_routineLed);
    }

    IEnumerator HideNextFrame() { yield return null; HideImmediate(); }
    IEnumerator HideExtraNextFrame() { yield return null; HideExtraImmediate(); }
    IEnumerator HideLedNextFrame() { yield return null; HideLedImmediate(); }

    void OnDialogueStart(int index)
    {
        if (index == triggerIndex)
        {
            if (_routineMain != null) StopCoroutine(_routineMain);
            if (_routineExtra != null) StopCoroutine(_routineExtra);
            if (_routineLed != null) StopCoroutine(_routineLed);

            ShowImmediate();
            ApplyText(baseText);
            _routineMain = StartCoroutine(HighlightSequence());

            _routineExtra = StartCoroutine(ExtraSequence());
            _routineLed = StartCoroutine(LedSequence());
        }

        int hideTarget = hideIndex >= 0 ? hideIndex : (triggerIndex + 1);
        if (index == hideTarget)
        {
            if (_routineMain != null) StopCoroutine(_routineMain);
            if (_routineExtra != null) StopCoroutine(_routineExtra);
            if (_routineLed != null) StopCoroutine(_routineLed);
            HideImmediate();
            HideExtraImmediate();
            HideLedImmediate();
        }
    }

    IEnumerator HighlightSequence()
    {
        yield return new WaitForSeconds(highlightAStart);
        HighlightPart("A: Node pointer", highlightColor);
        yield return new WaitForSeconds(highlightADuration);
        HighlightPart("A: Node pointer", normalColor);

        float nextWait = highlightBStart - (highlightAStart + highlightADuration);
        if (nextWait > 0f) yield return new WaitForSeconds(nextWait);

        HighlightPart("valB", highlightColor);
        yield return new WaitForSeconds(highlightBDuration);
        HighlightPart("valB", normalColor);
    }

    IEnumerator ExtraSequence()
    {
        if (extraStartHidden) HideExtraImmediate();
        if (extraHideTime < extraShowTime) extraHideTime = extraShowTime;
        yield return new WaitForSeconds(Mathf.Max(0f, extraShowTime));
        ShowExtraImmediate();
        if (extraAutoHide)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, extraHideTime - extraShowTime));
            HideExtraImmediate();
        }
    }

    IEnumerator LedSequence()
    {
        if (ledStartHidden) HideLedImmediate();
        if (ledHideTime < ledShowTime) ledHideTime = ledShowTime;
        yield return new WaitForSeconds(Mathf.Max(0f, ledShowTime));
        ShowLedImmediate();
        if (ledAutoHide)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, ledHideTime - ledShowTime));
            HideLedImmediate();
        }
    }

    void HighlightPart(string keyword, Color color)
    {
        string hex = ColorUtility.ToHtmlStringRGB(color);
        string newText = baseText.Replace(keyword, $"<color=#{hex}>{keyword}</color>");
        ApplyText(newText);
    }

    void ApplyText(string t)
    {
        if (tmpText3D != null) tmpText3D.text = t;
        if (legacyTextMesh != null) legacyTextMesh.text = t;
    }

    void HideImmediate()
    {
        if (textContainer != null) { textContainer.SetActive(false); return; }
        if (tmpText3D != null) tmpText3D.gameObject.SetActive(false);
        if (legacyTextMesh != null) legacyTextMesh.gameObject.SetActive(false);
        if (tmpText3D == null && legacyTextMesh == null) gameObject.SetActive(false);
    }

    void ShowImmediate()
    {
        if (textContainer != null) { textContainer.SetActive(true); return; }
        if (tmpText3D != null) tmpText3D.gameObject.SetActive(true);
        if (legacyTextMesh != null) legacyTextMesh.gameObject.SetActive(true);
        if (tmpText3D == null && legacyTextMesh == null) gameObject.SetActive(true);
    }

    void HideExtraImmediate()
    {
        if (extraTextContainer != null) { extraTextContainer.SetActive(false); return; }
        if (extraTmpText3D != null) extraTmpText3D.gameObject.SetActive(false);
        if (extraLegacyTextMesh != null) extraLegacyTextMesh.gameObject.SetActive(false);
    }

    void ShowExtraImmediate()
    {
        if (extraTextContainer != null) { extraTextContainer.SetActive(true); return; }
        if (extraTmpText3D != null) extraTmpText3D.gameObject.SetActive(true);
        if (extraLegacyTextMesh != null) extraLegacyTextMesh.gameObject.SetActive(true);
    }

    void HideLedImmediate()
    {
        if (ledObject != null) ledObject.SetActive(false);
    }

    void ShowLedImmediate()
    {
        if (ledObject != null) ledObject.SetActive(true);
    }

    [ContextMenu("Show Now")]
    public void ShowNow()
    {
        ShowImmediate();
        ApplyText(baseText);
    }

    [ContextMenu("Hide Now")]
    public void HideNow()
    {
        HideImmediate();
    }

    [ContextMenu("Show Extra Now")]
    public void ShowExtraNow()
    {
        ShowExtraImmediate();
    }

    [ContextMenu("Hide Extra Now")]
    public void HideExtraNow()
    {
        HideExtraImmediate();
    }

    [ContextMenu("Show LED Now")]
    public void ShowLEDNow()
    {
        ShowLedImmediate();
    }

    [ContextMenu("Hide LED Now")]
    public void HideLEDNow()
    {
        HideLedImmediate();
    }
}
