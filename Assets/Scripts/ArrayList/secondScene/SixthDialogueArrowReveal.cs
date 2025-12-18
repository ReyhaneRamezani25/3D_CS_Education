using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SixthSignArrowReveal : MonoBehaviour
{
    [Header("Sequencer (needs OnDialogueStart/End)")]
    public DialogueSequencerBasic sequencer;

    [Header("Sign Arrow (container)")]
    [Tooltip("UI container that holds the arrow/text (same as the second type of hintContainer).")]
    public GameObject signArrowContainer;

    [Header("Timing")]
    [Tooltip("How many seconds after dialogue 6 starts should it appear.")]
    public float delayFromSixthStart = 2f;
    [Tooltip("Arrow reveal duration.")]
    public float revealDuration = 0.6f;

    [Header("Reveal Mode")]
    [Tooltip("If enabled, reveals with horizontal Fill (Left->Right); otherwise with fade (CanvasGroup + TMP alpha).")]
    public bool useLeftToRightReveal = false;

    [Header("Lifecycle")]
    [Tooltip("Hide at the end of dialogue 6?")]
    public bool hideAtEndOfSixth = true;
    [Tooltip("Keep the container disabled until reveal?")]
    public bool disableObjectUntilReveal = true;

    [Header("Red 23 (optional)")]
    [Tooltip("TMP that will show red number 23 after the arrow is revealed.")]
    public TMP_Text targetTMPFor23;
    [Tooltip("Delay after the arrow is revealed before showing 23.")]
    public float delayAfterArrowFor23 = 1f;
    [Tooltip("Color of number 23.")]
    public Color redColor = Color.red;

    private Color _originalTextColor;

    CanvasGroup _cg;
    List<TMP_Text> _tmps;
    bool _hasUIOrTMP;
    Coroutine _showCo;

    void Awake()
    {
        if (signArrowContainer != null)
        {
            if (disableObjectUntilReveal)
            {
                signArrowContainer.SetActive(false);
            }
            else
            {
                if (useLeftToRightReveal)
                    PrepareAsLeftToRightHidden(signArrowContainer);
                else
                    PrepareAsFadedHidden(signArrowContainer);
            }
        }

        if (targetTMPFor23 != null)
        {
            _originalTextColor = targetTMPFor23.color;
            targetTMPFor23.text = "";
            targetTMPFor23.alpha = 0f;
        }
    }

    void OnEnable()
    {
        if (sequencer != null)
        {
            sequencer.OnDialogueStart += OnDialogueStart;
            sequencer.OnDialogueEnd   += OnDialogueEnd;
        }
    }

    void OnDisable()
    {
        if (sequencer != null)
        {
            sequencer.OnDialogueStart -= OnDialogueStart;
            sequencer.OnDialogueEnd   -= OnDialogueEnd;
        }
    }

    void OnDialogueStart(int index)
    {
        if (index != 5) return;

        if (_showCo != null) StopCoroutine(_showCo);
        _showCo = StartCoroutine(CoRevealAfterDelay(delayFromSixthStart));
    }

    void OnDialogueEnd(int index)
    {
        if (index == 5 && targetTMPFor23 != null)
        {
            targetTMPFor23.color = _originalTextColor;
        }

        if (index == 5 && hideAtEndOfSixth && signArrowContainer != null)
        {
            signArrowContainer.SetActive(false);
        }

        if (index == 6 && targetTMPFor23 != null)
        {
            targetTMPFor23.text = "";
            targetTMPFor23.alpha = 0f;
        }
    }

    IEnumerator CoRevealAfterDelay(float delay)
    {
        if (signArrowContainer == null) yield break;

        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

        if (!signArrowContainer.activeSelf) signArrowContainer.SetActive(true);

        if (useLeftToRightReveal)
        {
            var img = signArrowContainer.GetComponentInChildren<Image>(true);
            if (img != null)
            {
                PrepareImageForFill(img);
                yield return CoImageFill(img, 1f, Mathf.Max(0.0001f, revealDuration));
            }
            else
            {
                PrepareAsFadedHidden(signArrowContainer);
                yield return CoFadeIn(signArrowContainer, Mathf.Max(0.0001f, revealDuration));
            }
        }
        else
        {
            PrepareAsFadedHidden(signArrowContainer);
            yield return CoFadeIn(signArrowContainer, Mathf.Max(0.0001f, revealDuration));
        }

        if (targetTMPFor23 != null)
        {
            yield return new WaitForSecondsRealtime(delayAfterArrowFor23);
            targetTMPFor23.text = "23";
            targetTMPFor23.color = redColor;
            targetTMPFor23.alpha = 1f;
        }
    }

    void CacheFadables(GameObject target)
    {
        if (target == null) return;

        _cg = target.GetComponent<CanvasGroup>();
        if (_cg == null) _cg = target.GetComponentInChildren<CanvasGroup>(true);

        _tmps = new List<TMP_Text>(target.GetComponentsInChildren<TMP_Text>(true));

        bool hasUI = target.GetComponentInChildren<CanvasRenderer>(true) != null;
        bool hasTMP = _tmps != null && _tmps.Count > 0;
        _hasUIOrTMP = hasUI || hasTMP;
    }

    void PrepareAsFadedHidden(GameObject target)
    {
        if (target == null) return;
        CacheFadables(target);

        if (_cg == null) _cg = target.AddComponent<CanvasGroup>();
        _cg.alpha = 0f;

        if (_tmps != null)
            foreach (var t in _tmps) if (t) t.alpha = 0f;
    }

    void PrepareAsLeftToRightHidden(GameObject target)
    {
        if (target == null) return;
        var img = target.GetComponentInChildren<Image>(true);
        if (img != null)
            PrepareImageForFill(img);
        else
            PrepareAsFadedHidden(target);
    }

    void PrepareImageForFill(Image img)
    {
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = 0;
        img.fillAmount = 0f;
    }

    IEnumerator CoImageFill(Image img, float targetFill, float dur)
    {
        float start = img.fillAmount;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            if (img == null) yield break;
            img.fillAmount = Mathf.Lerp(start, targetFill, t / dur);
            yield return null;
        }
        if (img != null) img.fillAmount = targetFill;
    }

    IEnumerator CoFadeIn(GameObject target, float dur)
    {
        CacheFadables(target);
        float t = 0f;
        float startA = _cg != null ? _cg.alpha : 0f;

        while (t < dur)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(startA, 1f, t / dur);

            if (_cg != null) _cg.alpha = a;
            if (_tmps != null) foreach (var tx in _tmps) if (tx) tx.alpha = a;

            yield return null;
        }

        if (_cg != null) _cg.alpha = 1f;
        if (_tmps != null) foreach (var tx in _tmps) if (tx) tx.alpha = 1f;
    }
}
