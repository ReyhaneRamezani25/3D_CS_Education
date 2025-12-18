using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SeventhDialogueSignArrow : MonoBehaviour
{
    [Header("Sequencer (needs OnDialogueStart/End)")]
    public DialogueSequencerBasic sequencer;

    [Header("Sign Arrow (container)")]
    [Tooltip("UI container that holds the arrow/text.")]
    public GameObject signArrowContainer;

    [Header("Timing")]
    [Tooltip("How many seconds after dialogue 7 starts should it appear.")]
    public float delayFromSeventhStart = 2f;
    [Tooltip("Arrow reveal duration.")]
    public float revealDuration = 0.6f;

    [Header("Reveal Mode")]
    [Tooltip("If enabled, reveals with horizontal Fill (Left->Right); otherwise with fade (CanvasGroup + TMP alpha).")]
    public bool useLeftToRightReveal = false;

    [Header("Lifecycle")]
    [Tooltip("Hide the arrow at the end of dialogue 7?")]
    public bool hideAtEndOfSeventh = true;
    [Tooltip("Keep the arrow container disabled until reveal?")]
    public bool disableObjectUntilReveal = true;

    [Header("Optional Red 23 Text (on Arrow)")]
    public TMP_Text targetTMPFor23;
    public float delayAfterArrowFor23 = 1f;
    public Color redColor = Color.red;

    [Header("Post Text #1 (shows after Arrow, hides at end)")]
    [Tooltip("Text container/object shown after the Arrow.")]
    public GameObject postTextContainer;
    [Tooltip("TMP reference for the specific text (optional).")]
    public TMP_Text postTextTMP;
    [Tooltip("Delay between end of Arrow reveal and start of text #1.")]
    public float delayAfterArrowForText = 0.7f;
    [Tooltip("Reveal duration for text #1.")]
    public float postTextRevealDuration = 0.45f;
    [Tooltip("Hide text #1 at the end of dialogue 7?")]
    public bool hidePostTextAtEnd = true;
    [Tooltip("Keep text #1 container disabled until reveal?")]
    public bool disablePostTextUntilReveal = true;

    [Header("Post Text #2 (same behavior as #1)")]
    [Tooltip("Second text container that should behave exactly like the first.")]
    public GameObject postTextContainer2;
    [Tooltip("TMP for the second text (optional).")]
    public TMP_Text postTextTMP2;
    [Tooltip("Delay between end of text #1 (or Arrow if #1 is absent) and start of text #2.")]
    public float delayAfterPrevForText2 = 0.7f;
    [Tooltip("Reveal duration for text #2.")]
    public float postTextRevealDuration2 = 0.45f;
    [Tooltip("Hide text #2 at the end of dialogue 7?")]
    public bool hidePostText2AtEnd = true;
    [Tooltip("Keep text #2 container disabled until reveal?")]
    public bool disablePostText2UntilReveal = true;

    CanvasGroup _cg;
    List<TMP_Text> _tmps;
    bool _hasUIOrTMP;
    Coroutine _showCo;

    CanvasGroup _postCG1;
    List<TMP_Text> _postTMPs1;

    CanvasGroup _postCG2;
    List<TMP_Text> _postTMPs2;

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

        if (postTextContainer != null)
        {
            if (disablePostTextUntilReveal)
            {
                postTextContainer.SetActive(false);
            }
            else
            {
                PrepareAsFadedHidden(postTextContainer, isPost: true, which: 1);
            }
        }

        if (postTextContainer2 != null)
        {
            if (disablePostText2UntilReveal)
            {
                postTextContainer2.SetActive(false);
            }
            else
            {
                PrepareAsFadedHidden(postTextContainer2, isPost: true, which: 2);
            }
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
        if (index != 6) return;

        if (_showCo != null) StopCoroutine(_showCo);
        _showCo = StartCoroutine(CoRevealSequence());
    }

    void OnDialogueEnd(int index)
    {
        if (index != 6) return;

        if (hideAtEndOfSeventh && signArrowContainer != null)
        {
            StartCoroutine(CoFadeOutIfPossible(signArrowContainer, 0.25f));
        }

        if (hidePostTextAtEnd && postTextContainer != null)
        {
            StartCoroutine(CoFadeOutIfPossible(postTextContainer, 0.25f));
        }

        if (hidePostText2AtEnd && postTextContainer2 != null)
        {
            StartCoroutine(CoFadeOutIfPossible(postTextContainer2, 0.25f));
        }
    }

    IEnumerator CoRevealSequence()
    {
        yield return CoRevealAfterDelay(delayFromSeventhStart);

        if (targetTMPFor23 != null)
        {
            yield return new WaitForSecondsRealtime(delayAfterArrowFor23);
            targetTMPFor23.text = "23";
            targetTMPFor23.color = redColor;
            targetTMPFor23.alpha = 1f;
        }

        bool showedFirst = false;
        if (postTextContainer != null)
        {
            if (delayAfterArrowForText > 0f)
                yield return new WaitForSecondsRealtime(delayAfterArrowForText);

            if (!postTextContainer.activeSelf) postTextContainer.SetActive(true);
            PrepareAsFadedHidden(postTextContainer, isPost: true, which: 1);
            yield return CoFadeIn(postTextContainer, Mathf.Max(0.0001f, postTextRevealDuration), isPost: true, which: 1);
            showedFirst = true;
        }

        if (postTextContainer2 != null)
        {
            if (delayAfterPrevForText2 > 0f)
                yield return new WaitForSecondsRealtime(delayAfterPrevForText2);

            if (!postTextContainer2.activeSelf) postTextContainer2.SetActive(true);
            PrepareAsFadedHidden(postTextContainer2, isPost: true, which: 2);
            yield return CoFadeIn(postTextContainer2, Mathf.Max(0.0001f, postTextRevealDuration2), isPost: true, which: 2);
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
    }

    void CacheFadables(GameObject target, bool isPost = false, int which = 0)
    {
        if (target == null) return;

        if (!isPost)
        {
            _cg = target.GetComponent<CanvasGroup>();
            if (_cg == null) _cg = target.GetComponentInChildren<CanvasGroup>(true);

            _tmps = new List<TMP_Text>(target.GetComponentsInChildren<TMP_Text>(true));
            bool hasUI = target.GetComponentInChildren<CanvasRenderer>(true) != null;
            bool hasTMP = _tmps != null && _tmps.Count > 0;
            _hasUIOrTMP = hasUI || hasTMP;
        }
        else
        {
            if (which == 1)
            {
                _postCG1 = target.GetComponent<CanvasGroup>();
                if (_postCG1 == null) _postCG1 = target.GetComponentInChildren<CanvasGroup>(true);
                _postTMPs1 = new List<TMP_Text>(target.GetComponentsInChildren<TMP_Text>(true));
            }
            else if (which == 2)
            {
                _postCG2 = target.GetComponent<CanvasGroup>();
                if (_postCG2 == null) _postCG2 = target.GetComponentInChildren<CanvasGroup>(true);
                _postTMPs2 = new List<TMP_Text>(target.GetComponentsInChildren<TMP_Text>(true));
            }
        }
    }

    void PrepareAsFadedHidden(GameObject target, bool isPost = false, int which = 0)
    {
        if (target == null) return;
        CacheFadables(target, isPost, which);

        if (!isPost)
        {
            if (_cg == null) _cg = target.AddComponent<CanvasGroup>();
            _cg.alpha = 0f;
            if (_tmps != null) foreach (var t in _tmps) if (t) t.alpha = 0f;
        }
        else
        {
            if (which == 1)
            {
                if (_postCG1 == null) _postCG1 = target.AddComponent<CanvasGroup>();
                _postCG1.alpha = 0f;
                if (_postTMPs1 != null) foreach (var t in _postTMPs1) if (t) t.alpha = 0f;
            }
            else if (which == 2)
            {
                if (_postCG2 == null) _postCG2 = target.AddComponent<CanvasGroup>();
                _postCG2.alpha = 0f;
                if (_postTMPs2 != null) foreach (var t in _postTMPs2) if (t) t.alpha = 0f;
            }
        }
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

    IEnumerator CoFadeIn(GameObject target, float dur, bool isPost = false, int which = 0)
    {
        CacheFadables(target, isPost, which);
        float t = 0f;

        if (!isPost)
        {
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
        else
        {
            if (which == 1)
            {
                float startA = _postCG1 != null ? _postCG1.alpha : 0f;
                while (t < dur)
                {
                    t += Time.deltaTime;
                    float a = Mathf.Lerp(startA, 1f, t / dur);
                    if (_postCG1 != null) _postCG1.alpha = a;
                    if (_postTMPs1 != null) foreach (var tx in _postTMPs1) if (tx) tx.alpha = a;
                    yield return null;
                }
                if (_postCG1 != null) _postCG1.alpha = 1f;
                if (_postTMPs1 != null) foreach (var tx in _postTMPs1) if (tx) tx.alpha = 1f;
            }
            else if (which == 2)
            {
                float startA = _postCG2 != null ? _postCG2.alpha : 0f;
                while (t < dur)
                {
                    t += Time.deltaTime;
                    float a = Mathf.Lerp(startA, 1f, t / dur);
                    if (_postCG2 != null) _postCG2.alpha = a;
                    if (_postTMPs2 != null) foreach (var tx in _postTMPs2) if (tx) tx.alpha = a;
                    yield return null;
                }
                if (_postCG2 != null) _postCG2.alpha = 1f;
                if (_postTMPs2 != null) foreach (var tx in _postTMPs2) if (tx) tx.alpha = 1f;
            }
        }
    }

    IEnumerator CoFadeOutIfPossible(GameObject target, float dur)
    {
        if (target == null) yield break;

        var cg = target.GetComponent<CanvasGroup>();
        if (cg == null) cg = target.GetComponentInChildren<CanvasGroup>(true);

        if (cg == null)
        {
            target.SetActive(false);
            yield break;
        }

        float t = 0f;
        float startA = cg.alpha;
        while (t < dur)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startA, 0f, t / dur);
            yield return null;
        }
        cg.alpha = 0f;
        target.SetActive(false);
    }
}
