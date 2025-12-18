using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DotArrowForThirdDialogue : MonoBehaviour
{
    [Header("References")]
    public DialogueVoiceControllerBasic controller;
    public GameObject dotArrow; // Arrow #1

    [Header("Trigger Settings")]
    [Tooltip("Dialogue index to trigger on (e.g., 2 = third dialogue).")]
    public int showOnIndex = 2;
    [Tooltip("Base delay after dialogue start before ANY animation (arrow or shift).")]
    public float delayAfterStart = 0.3f;

    [Header("Arrow #1 Reveal (optional)")]
    public bool useArrowReveal = true;
    public float revealDuration = 0.6f;
    public bool preferImageFill = true;
    [Range(0, 1)] public int fillOrigin = 0; // 0 = Left→Right, 1 = Right→Left
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Arrow #2 (optional, Left→Right)")]
    [Tooltip("Second arrow that appears a few seconds after dialogue starts.")]
    public GameObject secondArrow;
    [Tooltip("Seconds after dialogue start to show the second arrow.")]
    public float secondArrowDelayAfterStart = 1.0f;
    public bool secondUseArrowReveal = true;
    public float secondRevealDuration = 0.6f;
    public bool secondPreferImageFill = true;
    [Range(0, 1)] public int secondFillOrigin = 0; // 0 = Left→Right

    [Header("Shift Start Timing")]
    [Tooltip("Extra delay before labels start shifting (in seconds).")]
    public float shiftDelayAfterStart = 0.0f;
    [Tooltip("If true, the shift delay starts AFTER the arrow reveal completes.")]
    public bool startShiftAfterArrow = true;

    [Header("3 Labels (Shift Left: B->A, C->B, then C becomes empty)")]
    public TMP_Text labelA; // left
    public TMP_Text labelB; // middle
    public TMP_Text labelC; // right

    [Header("Text FX")]
    public float labelFadeDuration = 0.25f;
    public float labelStepDelay = 0.1f;
    public Color movedHighlightColor = Color.yellow;
    public float movedHighlightDuration = 0.1f;
    public bool revertColorAfterHighlight = true;

    [Header("Arrow/Shift End Behavior")]
    public bool hideWhenDialogueEnds = true;

    [Header("FILL after another dialogue (C was emptied)")]
    [Tooltip("When this dialogue STARTS, put default text into the previously-emptied C label.")]
    public int fillDefaultOnDialogueIndex = 3; // e.g., next dialogue after showOnIndex
    public float fillDelayAfterStart = 0f;
    public string defaultNumText = "default num";
    public Color defaultNumColor = Color.white;
    [Tooltip("Font size for 'default num' on label C.")]
    public float defaultNumFontSize = 18f;

    [Header("RESTORE on a later dialogue")]
    [Tooltip("When this dialogue ENDS, restore all labels to the initial snapshot.")]
    public int restoreOnDialogueIndex = 5; // e.g., sixth dialogue
    public float restoreDelay = 0f;

    // Arrow #1 cache
    CanvasGroup _cg;
    Image _img;
    Vector3 _origScale = Vector3.one;

    // Arrow #2 cache
    CanvasGroup _cg2;
    Image _img2;
    Vector3 _origScale2 = Vector3.one;

    Coroutine _runner;
    Coroutine _secondArrowRunner;
    Coroutine _fillRunner;

    // snapshot
    string _initA, _initB, _initC;
    Color _colA, _colB, _colC;
    float _fontA, _fontB, _fontC;
    bool _snapshotTaken = false;

    void Awake()
    {
        if (dotArrow != null)
        {
            _cg = dotArrow.GetComponent<CanvasGroup>();
            if (_cg == null) _cg = dotArrow.AddComponent<CanvasGroup>();
            _img = dotArrow.GetComponentInChildren<Image>(true);
            _origScale = dotArrow.transform.localScale;
            PrepareArrowHidden(dotArrow, _img, _cg, preferImageFill, fillOrigin, _origScale);
        }

        if (secondArrow != null)
        {
            _cg2 = secondArrow.GetComponent<CanvasGroup>();
            if (_cg2 == null) _cg2 = secondArrow.AddComponent<CanvasGroup>();
            _img2 = secondArrow.GetComponentInChildren<Image>(true);
            _origScale2 = secondArrow.transform.localScale;
            PrepareArrowHidden(secondArrow, _img2, _cg2, secondPreferImageFill, secondFillOrigin, _origScale2);
        }

        TakeSnapshot();
    }

    void OnEnable()
    {
        if (controller != null)
        {
            controller.OnDialogueStart += HandleStart;
            controller.OnDialogueEnd += HandleEnd;
            controller.OnSequenceFinished += HideAllArrowsInstant;
        }
    }

    void OnDisable()
    {
        if (controller != null)
        {
            controller.OnDialogueStart -= HandleStart;
            controller.OnDialogueEnd -= HandleEnd;
            controller.OnSequenceFinished -= HideAllArrowsInstant;
        }
    }

    void HandleStart(int index)
    {
        if (_runner != null) StopCoroutine(_runner);
        if (_secondArrowRunner != null) StopCoroutine(_secondArrowRunner);
        if (_fillRunner != null) StopCoroutine(_fillRunner);

        if (index == showOnIndex)
        {
            _runner = StartCoroutine(CoRun());
            if (secondArrow != null)
                _secondArrowRunner = StartCoroutine(CoShowSecondArrowAfterDelay());
        }
        else
        {
            HideAllArrowsInstant();
        }

        if (index == fillDefaultOnDialogueIndex)
        {
            _fillRunner = StartCoroutine(CoFillDefaultAfterDelay());
        }
    }

    void HandleEnd(int index)
    {
        if (index == showOnIndex && hideWhenDialogueEnds)
            HideAllArrowsInstant();

        if (index == restoreOnDialogueIndex)
            StartCoroutine(CoRestoreAfterDelay());

        if (_secondArrowRunner != null) { StopCoroutine(_secondArrowRunner); _secondArrowRunner = null; }
        if (_fillRunner != null) { StopCoroutine(_fillRunner); _fillRunner = null; }
    }

    IEnumerator CoRun()
    {
        if (delayAfterStart > 0f)
            yield return new WaitForSecondsRealtime(delayAfterStart);

        if (useArrowReveal && dotArrow != null)
            yield return CoRevealArrow(dotArrow, _img, preferImageFill, fillOrigin, _origScale, revealDuration);

        float wait = Mathf.Max(0f, shiftDelayAfterStart);
        if (startShiftAfterArrow && useArrowReveal) wait += revealDuration;
        if (wait > 0f) yield return new WaitForSecondsRealtime(wait);

        yield return CoShiftLeft3();
    }

    IEnumerator CoShowSecondArrowAfterDelay()
    {
        float baseDelay = Mathf.Max(0f, secondArrowDelayAfterStart);
        if (baseDelay > 0f) yield return new WaitForSecondsRealtime(baseDelay);

        if (secondUseArrowReveal && secondArrow != null)
            yield return CoRevealArrow(secondArrow, _img2, secondPreferImageFill, secondFillOrigin, _origScale2, secondRevealDuration);
        else if (secondArrow != null)
            secondArrow.SetActive(true);

        _secondArrowRunner = null;
    }

    IEnumerator CoFillDefaultAfterDelay()
    {
        if (fillDelayAfterStart > 0f)
            yield return new WaitForSecondsRealtime(fillDelayAfterStart);

        if (labelC != null)
        {
            labelC.text = string.IsNullOrEmpty(defaultNumText) ? "default num" : defaultNumText;
            labelC.color = defaultNumColor;
            labelC.alpha = 1f;
            labelC.fontSize = defaultNumFontSize;
        }

        _fillRunner = null;
    }

    void PrepareArrowHidden(GameObject arrow, Image img, CanvasGroup cg, bool useFill, int origin, Vector3 origScale)
    {
        if (arrow == null) return;

        arrow.SetActive(false);
        if (cg != null) cg.alpha = 1f;

        if (useFill && img != null)
        {
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = origin;
            img.fillAmount = 0f;
        }
        else
        {
            arrow.transform.localScale = new Vector3(0f, origScale.y, origScale.z);
        }
    }

    IEnumerator CoRevealArrow(GameObject arrow, Image img, bool useFill, int origin, Vector3 origScale, float duration)
    {
        if (arrow == null) yield break;
        arrow.SetActive(true);

        float dur = Mathf.Max(0.0001f, duration);
        float t = 0f;

        if (useFill && img != null)
        {
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = origin;
            img.fillAmount = 0f;

            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                img.fillAmount = Mathf.Lerp(0f, 1f, ease.Evaluate(k));
                yield return null;
            }
            img.fillAmount = 1f;
        }
        else
        {
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                float x = Mathf.Lerp(0f, origScale.x, ease.Evaluate(k));
                arrow.transform.localScale = new Vector3(x, origScale.y, origScale.z);
                yield return null;
            }
            arrow.transform.localScale = new Vector3(origScale.x, origScale.y, origScale.z);
        }
    }

    void HideAllArrowsInstant()
    {
        HideArrowInstant(dotArrow, _img);
        HideArrowInstant(secondArrow, _img2);
    }

    void HideArrowInstant(GameObject arrow, Image img)
    {
        if (arrow == null) return;
        if (img != null) img.fillAmount = 0f;
        arrow.SetActive(false);
    }

    IEnumerator CoShiftLeft3()
    {
        if (labelA == null || labelB == null || labelC == null) yield break;

        string b = labelB.text;
        string c = labelC.text;

        yield return SwapFade(labelA, b); // A <- B
        yield return SwapFade(labelB, c); // B <- C
        yield return SwapFade(labelC, ""); // C <- ""
    }

    void TakeSnapshot()
    {
        if (_snapshotTaken) return;

        if (labelA) { _initA = labelA.text; _colA = labelA.color; _fontA = labelA.fontSize; }
        if (labelB) { _initB = labelB.text; _colB = labelB.color; _fontB = labelB.fontSize; }
        if (labelC) { _initC = labelC.text; _colC = labelC.color; _fontC = labelC.fontSize; }

        _snapshotTaken = true;
    }

    IEnumerator CoRestoreAfterDelay()
    {
        if (restoreDelay > 0f)
            yield return new WaitForSecondsRealtime(restoreDelay);

        RestoreInitialTexts();
    }

    void RestoreInitialTexts()
    {
        if (!_snapshotTaken) return;

        if (labelA) { labelA.text = _initA; labelA.color = _colA; labelA.fontSize = _fontA; labelA.alpha = 1f; }
        if (labelB) { labelB.text = _initB; labelB.color = _colB; labelB.fontSize = _fontB; labelB.alpha = 1f; }
        if (labelC) { labelC.text = _initC; labelC.color = _colC; labelC.fontSize = _fontC; labelC.alpha = 1f; }
    }

    IEnumerator SwapFade(TMP_Text target, string newText)
    {
        if (target == null) yield break;

        yield return FadeTMP(target, 0f, labelFadeDuration);
        target.text = newText ?? "";
        yield return FadeTMP(target, 1f, labelFadeDuration);

        Color prev = target.color;
        target.color = movedHighlightColor;

        float hl = Mathf.Max(0f, movedHighlightDuration);
        if (hl > 0f) yield return new WaitForSecondsRealtime(hl);

        if (revertColorAfterHighlight)
            target.color = prev;

        if (labelStepDelay > 0f)
            yield return new WaitForSecondsRealtime(labelStepDelay);
    }

    IEnumerator FadeTMP(TMP_Text tmp, float targetAlpha, float dur)
    {
        if (tmp == null) yield break;
        float start = tmp.alpha;
        float t = 0f;
        dur = Mathf.Max(0.0001f, dur);

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            tmp.alpha = Mathf.Lerp(start, targetAlpha, t / dur);
            yield return null;
        }
        tmp.alpha = targetAlpha;
    }
}
