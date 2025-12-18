using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DotArrowDuringDialogue : MonoBehaviour
{
    [Header("References")]
    public DialogueVoiceControllerBasic controller;
    public GameObject dotArrow;

    [Header("Trigger Settings")]
    [Tooltip("Dialogue index where this animation triggers (e.g., 1 = second dialogue).")]
    public int showOnIndex = 1;
    [Tooltip("Base delay after dialogue start before any animation begins.")]
    public float delayAfterStart = 0.3f;

    [Header("Arrow #1 Reveal (optional)")]
    public bool useArrowReveal = true;
    public float revealDuration = 0.6f;
    public bool preferImageFill = true;
    [Range(0, 1)] public int fillOrigin = 0; // 0 = Left->Right
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Arrow #2 (optional, Left->Right like sample)")]
    [Tooltip("Second arrow that appears a few seconds after dialogue starts, revealed Left->Right.")]
    public GameObject secondArrow;
    [Tooltip("Seconds after dialogue start to show the second arrow.")]
    public float secondArrowDelayAfterStart = 1.0f;
    public bool secondUseArrowReveal = true;
    public float secondRevealDuration = 0.6f;
    public bool secondPreferImageFill = true;
    [Range(0, 1)] public int secondFillOrigin = 0; // 0 = Left->Right

    [Header("Shift Start Timing")]
    [Tooltip("Extra delay before labels start shifting (seconds).")]
    public float shiftDelayAfterStart = 0.0f;
    [Tooltip("If true, the shift delay starts AFTER the arrow reveal completes.")]
    public bool startShiftAfterArrow = true;

    [Header("Shift 5 Labels Left (E becomes empty)")]
    [Tooltip("Five TMP_Text elements in left-to-right order (A to E).")]
    public TMP_Text labelA;
    public TMP_Text labelB;
    public TMP_Text labelC;
    public TMP_Text labelD;
    public TMP_Text labelE;

    public float labelFadeDuration = 0.25f;
    public float labelStepDelay = 0.1f;
    public Color movedHighlightColor = Color.yellow;
    public float movedHighlightDuration = 0.1f;
    public bool revertColorAfterHighlight = true;

    [Header("End Behavior")]
    public bool hideWhenDialogueEnds = true;

    [Header("Reset")]
    public bool restoreOnDialogueEnd = true;
    public float restoreDelay = 0.3f;

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

    string _initA, _initB, _initC, _initD, _initE;
    Color _colA, _colB, _colC, _colD, _colE;
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
    }

    void HandleEnd(int index)
    {
        if (index == showOnIndex)
        {
            if (_secondArrowRunner != null) { StopCoroutine(_secondArrowRunner); _secondArrowRunner = null; }

            if (hideWhenDialogueEnds)
                HideAllArrowsInstant();

            if (restoreOnDialogueEnd)
                StartCoroutine(CoRestoreAfterDelay());
        }
    }

    IEnumerator CoRun()
    {
        // base delay
        if (delayAfterStart > 0f)
            yield return new WaitForSecondsRealtime(delayAfterStart);

        // arrow #1
        if (useArrowReveal && dotArrow != null)
            yield return CoRevealArrow(dotArrow, _img, preferImageFill, fillOrigin, _origScale, revealDuration);

        // shift delay
        float wait = Mathf.Max(0f, shiftDelayAfterStart);
        if (startShiftAfterArrow && useArrowReveal) wait += revealDuration;
        if (wait > 0f) yield return new WaitForSecondsRealtime(wait);

        // shift 5 labels
        yield return CoShiftLeft5();
    }

    IEnumerator CoShowSecondArrowAfterDelay()
    {
        // second arrow appears relative to dialogue start
        float baseDelay = Mathf.Max(0f, secondArrowDelayAfterStart);
        if (baseDelay > 0f) yield return new WaitForSecondsRealtime(baseDelay);

        if (secondUseArrowReveal && secondArrow != null)
            yield return CoRevealArrow(secondArrow, _img2, secondPreferImageFill, secondFillOrigin, _origScale2, secondRevealDuration);
        else if (secondArrow != null)
            secondArrow.SetActive(true);

        _secondArrowRunner = null;
    }

    // ---------- Arrow helpers (L->R like sample) ----------
    void PrepareArrowHidden(GameObject arrow, Image img, CanvasGroup cg, bool useFill, int origin, Vector3 origScale)
    {
        if (arrow == null) return;
        arrow.SetActive(false);
        if (cg != null) cg.alpha = 1f;

        if (useFill && img != null)
        {
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = origin; // 0 = Left, 1 = Right
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

    // ---------- Shift logic ----------
    IEnumerator CoShiftLeft5()
    {
        if (labelA == null || labelB == null || labelC == null || labelD == null || labelE == null)
            yield break;

        string a = labelA.text;
        string b = labelB.text;
        string c = labelC.text;
        string d = labelD.text;
        string e = labelE.text;

        yield return SwapFade(labelA, b);
        yield return SwapFade(labelB, c);
        yield return SwapFade(labelC, d);
        yield return SwapFade(labelD, e);
        yield return SwapFade(labelE, "");
    }

    // ---------- Snapshot / Restore ----------
    void TakeSnapshot()
    {
        if (_snapshotTaken) return;

        if (labelA) { _initA = labelA.text; _colA = labelA.color; }
        if (labelB) { _initB = labelB.text; _colB = labelB.color; }
        if (labelC) { _initC = labelC.text; _colC = labelC.color; }
        if (labelD) { _initD = labelD.text; _colD = labelD.color; }
        if (labelE) { _initE = labelE.text; _colE = labelE.color; }

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

        if (labelA) { labelA.text = _initA; labelA.color = _colA; labelA.alpha = 1f; }
        if (labelB) { labelB.text = _initB; labelB.color = _colB; labelB.alpha = 1f; }
        if (labelC) { labelC.text = _initC; labelC.color = _colC; labelC.alpha = 1f; }
        if (labelD) { labelD.text = _initD; labelD.color = _colD; labelD.alpha = 1f; }
        if (labelE) { labelE.text = _initE; labelE.color = _colE; labelE.alpha = 1f; }
    }

    // ---------- Helpers ----------
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

        if (revertColorAfterHighlight) target.color = prev;

        if (labelStepDelay > 0f) yield return new WaitForSecondsRealtime(labelStepDelay);
    }

    IEnumerator FadeTMP(TMP_Text tmp, float targetAlpha, float dur)
    {
        if (tmp == null) yield break;

        float start = tmp.alpha;
        float t = 0f;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            tmp.alpha = Mathf.Lerp(start, targetAlpha, t / dur);
            yield return null;
        }

        tmp.alpha = targetAlpha;
    }
}
