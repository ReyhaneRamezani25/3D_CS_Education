using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DotArrowForFifthDialogue : MonoBehaviour
{
    [Header("References")]
    public DialogueVoiceControllerBasic controller;
    public GameObject dotArrow;
    public TMP_Text textToClear;

    [Header("Trigger Settings")]
    [Tooltip("Dialogue index to trigger arrow/clear (e.g., 4 = fifth).")]
    public int showOnIndex = 4;

    [Tooltip("Delay after dialogue start before showing the arrow.")]
    public float arrowDelayAfterStart = 0.3f;

    [Tooltip("Delay after dialogue start before clearing the target text.")]
    public float clearDelayAfterStart = 1.0f;

    [Header("Arrow Reveal")]
    public bool useArrowReveal = true;
    public float revealDuration = 0.6f;
    public bool preferImageFill = true;
    [Range(0, 1)] public int fillOrigin = 0; // 0 = Left→Right, 1 = Right→Left
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("End Behavior")]
    [Tooltip("Hide the arrow when the target dialogue (showOnIndex) ends.")]
    public bool hideWhenDialogueEnds = true;

    [Header("Restore On Another Dialogue End")]
    [Tooltip("When this dialogue index ends (e.g., 5 = sixth), restore the cleared text.")]
    public int restoreOnDialogueEndIndex = 5;
    public bool restoreTextOnDialogueEnd = true;
    public float restoreDelay = 0.0f;

    CanvasGroup _cg;
    Image _img;
    Vector3 _origScale = Vector3.one;

    Coroutine _arrowRunner;
    Coroutine _clearRunner;

    // snapshot for text restore
    string _origText;
    float  _origAlpha = 1f;
    bool   _snapTaken = false;

    void Awake()
    {
        if (dotArrow != null)
        {
            _cg = dotArrow.GetComponent<CanvasGroup>();
            if (_cg == null) _cg = dotArrow.AddComponent<CanvasGroup>();
            _img = dotArrow.GetComponentInChildren<Image>(true);
            _origScale = dotArrow.transform.localScale;
            PrepareArrowHidden();
        }

        TakeSnapshot();
    }

    void OnEnable()
    {
        if (controller != null)
        {
            controller.OnDialogueStart += HandleStart;
            controller.OnDialogueEnd   += HandleEnd;
            controller.OnSequenceFinished += HideArrowInstant;
        }
    }

    void OnDisable()
    {
        if (controller != null)
        {
            controller.OnDialogueStart -= HandleStart;
            controller.OnDialogueEnd   -= HandleEnd;
            controller.OnSequenceFinished -= HideArrowInstant;
        }
    }

    void HandleStart(int index)
    {
        if (_arrowRunner != null) StopCoroutine(_arrowRunner);
        if (_clearRunner != null) StopCoroutine(_clearRunner);

        if (index == showOnIndex)
        {
            _arrowRunner = StartCoroutine(CoShowArrowAfterDelay());
            _clearRunner = StartCoroutine(CoClearTextAfterDelay());
        }
        else
        {
            HideArrowInstant();
        }
    }

    void HandleEnd(int index)
    {
        // hide arrow when the dialogue that triggered it ends
        if (index == showOnIndex && hideWhenDialogueEnds)
            HideArrowInstant();

        // restore text when the specified dialogue ends (e.g., 6th => index=5)
        if (restoreTextOnDialogueEnd && index == restoreOnDialogueEndIndex)
            StartCoroutine(CoRestoreAfterDelay());
    }

    // ---------- Arrow ----------
    void PrepareArrowHidden()
    {
        if (dotArrow == null) return;

        dotArrow.SetActive(false);
        if (_cg != null) _cg.alpha = 1f;

        if (preferImageFill && _img != null)
        {
            _img.type = Image.Type.Filled;
            _img.fillMethod = Image.FillMethod.Horizontal;
            _img.fillOrigin = fillOrigin;
            _img.fillAmount = 0f;
        }
        else
        {
            dotArrow.transform.localScale = new Vector3(0f, _origScale.y, _origScale.z);
        }
    }

    IEnumerator CoShowArrowAfterDelay()
    {
        if (arrowDelayAfterStart > 0f)
            yield return new WaitForSecondsRealtime(arrowDelayAfterStart);

        if (dotArrow == null)
        {
            _arrowRunner = null;
            yield break;
        }

        if (!useArrowReveal)
        {
            dotArrow.SetActive(true);
            _arrowRunner = null;
            yield break;
        }

        dotArrow.SetActive(true);
        float dur = Mathf.Max(0.0001f, revealDuration);
        float t = 0f;

        if (preferImageFill && _img != null)
        {
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                _img.fillAmount = Mathf.Lerp(0f, 1f, ease.Evaluate(k));
                yield return null;
            }
            _img.fillAmount = 1f;
        }
        else
        {
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                float x = Mathf.Lerp(0f, _origScale.x, ease.Evaluate(k));
                dotArrow.transform.localScale = new Vector3(x, _origScale.y, _origScale.z);
                yield return null;
            }
            dotArrow.transform.localScale = _origScale;
        }

        _arrowRunner = null;
    }

    void HideArrowInstant()
    {
        if (dotArrow == null) return;

        if (_img != null) _img.fillAmount = 0f;
        dotArrow.SetActive(false);
    }

    // ---------- Clear & Restore text ----------
    void TakeSnapshot()
    {
        if (_snapTaken || textToClear == null) return;
        _origText  = textToClear.text;
        _origAlpha = textToClear.alpha;
        _snapTaken = true;
    }

    IEnumerator CoClearTextAfterDelay()
    {
        if (clearDelayAfterStart > 0f)
            yield return new WaitForSecondsRealtime(clearDelayAfterStart);

        if (textToClear != null)
            textToClear.text = "";

        _clearRunner = null;
    }

    IEnumerator CoRestoreAfterDelay()
    {
        if (restoreDelay > 0f)
            yield return new WaitForSecondsRealtime(restoreDelay);

        RestoreTextNow();
    }

    void RestoreTextNow()
    {
        if (!_snapTaken || textToClear == null) return;
        textToClear.text  = _origText;
        textToClear.alpha = _origAlpha;
    }
}
