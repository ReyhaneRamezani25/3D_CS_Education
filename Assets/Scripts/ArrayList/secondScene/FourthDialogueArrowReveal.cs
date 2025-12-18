using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FourthDialogueArrowReveal : MonoBehaviour
{
    [Header("Sequencer")]
    public DialogueSequencerBasic sequencer;

    [Header("Sign Arrow (first)")]
    public GameObject signArrow;
    public float signDelayFromFourthStart = 0.25f;
    public float signRevealDuration = 0.6f;
    public bool signUseLeftToRight = true;
    public bool signDisableObjectUntilReveal = true;

    [Header("Follower Arrow (second)")]
    public GameObject followerArrow;
    public float extraDelayAfterSign = 1.0f;
    public float followerRevealDuration = 0.6f;
    public Vector3 followerTargetScale3D = new Vector3(15f, 1f, 8f);
    public bool followerDisableObjectUntilReveal = true;

    [Header("Swap 3 Labels (after follower shown)")]
    [Tooltip("A: first")]
    public TMP_Text swapFirst;
    [Tooltip("B: second")]
    public TMP_Text swapSecond;
    [Tooltip("C: third")]
    public TMP_Text swapThird;

    [Header("Swap timings (like main)")]
    [Tooltip("Fade duration for each label")]
    public float labelFadeDuration = 0.4f;
    [Tooltip("Short delay between swap steps")]
    public float labelStepDelay = 0.15f;

    [Header("Swap start delay")]
    [Tooltip("Seconds after the follower arrow finishes before starting A,B,C swap")]
    public float delayAfterFollower = 0.0f;

    [Header("Highlight (like main)")]
    public Color movedHighlightColor = Color.yellow;
    public float movedHighlightDuration = 0.15f;
    public bool revertColorAfterHighlight = true;

    [Header("Show red 23 after Dialogue 5")]
    [Tooltip("Half a second after dialogue 5 starts, put red '23' on swapFirst")]
    public float delayAfterFifth = 0.5f;
    public Color redColor = Color.red;

    [Header("Lifecycle")]
    [Tooltip("Hide arrows at the end of dialogue 5")]
    public bool hideAtEndOfFifth = true;
    [Tooltip("Restore swapped labels to initial state at the end of dialogue 5")]
    public bool restoreLabelsAtEndOfFifth = true;

    Vector3 _signInitialScale;
    Vector3 _followerInitialScale;
    bool _hasSignScale;
    bool _hasFollowerScale;
    Coroutine _signCo, _followerCo;

    bool _swapSnapshotTaken = false;
    string _initA, _initB, _initC;
    Color _initColA, _initColB, _initColC;

    void Awake()
    {
        if (signArrow != null)
        {
            _signInitialScale = signArrow.transform.localScale;
            _hasSignScale = true;
            if (signDisableObjectUntilReveal) signArrow.SetActive(false);
            else HideVisual(signArrow);
        }
        if (followerArrow != null)
        {
            _followerInitialScale = followerArrow.transform.localScale;
            _hasFollowerScale = true;
            if (followerDisableObjectUntilReveal) followerArrow.SetActive(false);
            else HideVisual(followerArrow);
        }
    }

    void OnEnable()
    {
        if (sequencer != null)
        {
            sequencer.OnDialogueStart += HandleDialogueStart;
            sequencer.OnDialogueEnd   += HandleDialogueEnd;
        }
    }

    void OnDisable()
    {
        if (sequencer != null)
        {
            sequencer.OnDialogueStart -= HandleDialogueStart;
            sequencer.OnDialogueEnd   -= HandleDialogueEnd;
        }
    }

    void HandleDialogueStart(int index)
    {
        if (index == 3)
        {
            if (_signCo != null) StopCoroutine(_signCo);
            _signCo = StartCoroutine(CoShowSignAfterDelay(signDelayFromFourthStart));

            if (_followerCo != null) StopCoroutine(_followerCo);
            float delay = Mathf.Max(0f, signDelayFromFourthStart) + Mathf.Max(0f, extraDelayAfterSign);
            _followerCo = StartCoroutine(CoShowFollowerAfterDelay(delay));
        }

        if (index == 4)
        {
            StartCoroutine(CoShowRed23AfterDelay());
        }
    }

    void HandleDialogueEnd(int index)
    {
        if (index == 4)
        {
            if (hideAtEndOfFifth)
            {
                if (signArrow != null) signArrow.SetActive(false);
                if (followerArrow != null) followerArrow.SetActive(false);
            }

            if (restoreLabelsAtEndOfFifth)
                RestoreOriginalSwapLabels();
        }
    }

    IEnumerator CoShowSignAfterDelay(float delay)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        if (signArrow == null) yield break;
        if (!signArrow.activeSelf) signArrow.SetActive(true);

        Image img = signArrow.GetComponentInChildren<Image>(true);
        if (signUseLeftToRight && img != null)
        {
            PrepareImageForFill(img);
            yield return CoImageFill(img, 1f, Mathf.Max(0.0001f, signRevealDuration));
        }
        else
        {
            Transform tf = signArrow.transform;
            Vector3 orig = _hasSignScale ? _signInitialScale : tf.localScale;
            tf.localScale = new Vector3(0f, orig.y, orig.z);
            yield return CoScaleX(tf, orig.x, Mathf.Max(0.0001f, signRevealDuration), orig.y, orig.z);
        }
    }

    IEnumerator CoShowFollowerAfterDelay(float delay)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        if (followerArrow == null) yield break;
        if (!followerArrow.activeSelf) followerArrow.SetActive(true);

        Image img = followerArrow.GetComponentInChildren<Image>(true);
        if (img != null)
        {
            PrepareImageForFill(img);
            yield return CoImageFill(img, 1f, Mathf.Max(0.0001f, followerRevealDuration));
        }
        else
        {
            Transform tf = followerArrow.transform;
            float y = followerTargetScale3D.y;
            float z = followerTargetScale3D.z;
            tf.localScale = new Vector3(0f, y, z);
            yield return CoScaleX(tf, followerTargetScale3D.x, Mathf.Max(0.0001f, followerRevealDuration), y, z);
        }

        if (swapFirst != null && swapSecond != null && swapThird != null)
        {
            if (delayAfterFollower > 0f)
                yield return new WaitForSecondsRealtime(delayAfterFollower);

            yield return StartCoroutine(CoSwapThreeLikeMain(swapFirst, swapSecond, swapThird));
        }
    }

    IEnumerator CoShowRed23AfterDelay()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, delayAfterFifth));
        if (swapFirst != null)
        {
            swapFirst.text = "23";
            swapFirst.color = redColor;
            swapFirst.alpha = 1f;
        }
        yield break;
    }

    IEnumerator CoSwapThreeLikeMain(TMP_Text a, TMP_Text b, TMP_Text c)
    {
        if (!_swapSnapshotTaken)
        {
            _initA = a != null ? a.text  : null;
            _initB = b != null ? b.text  : null;
            _initC = c != null ? c.text  : null;

            _initColA = a != null ? a.color : Color.white;
            _initColB = b != null ? b.color : Color.white;
            _initColC = c != null ? c.color : Color.white;

            _swapSnapshotTaken = true;
        }

        string txtA = a.text;
        string txtB = b.text;
        string txtC = c.text;

        yield return StartCoroutine(CoSwapOneStepLikeMain(txtB, b, c));
        yield return StartCoroutine(CoSwapOneStepLikeMain(txtA, a, b));
    }

    IEnumerator CoSwapOneStepLikeMain(string fromText, TMP_Text fromLabel, TMP_Text toLabel)
    {
        if (toLabel == null) yield break;

        yield return StartCoroutine(FadeTMP(toLabel, 0f, labelFadeDuration));
        toLabel.text = fromText;
        yield return StartCoroutine(FadeTMP(toLabel, 1f, labelFadeDuration));

        Color prevColor = toLabel.color;
        toLabel.color = movedHighlightColor;

        float hl = Mathf.Max(0f, movedHighlightDuration);
        if (revertColorAfterHighlight && hl > 0f)
        {
            yield return new WaitForSecondsRealtime(hl);
            toLabel.color = prevColor;
        }

        float extraDelay = Mathf.Max(0f, labelStepDelay - hl);
        if (extraDelay > 0f) yield return new WaitForSecondsRealtime(extraDelay);
    }

    void RestoreOriginalSwapLabels()
    {
        if (!_swapSnapshotTaken) return;

        if (swapFirst != null)
        {
            swapFirst.text  = _initA ?? "";
            swapFirst.color = _initColA;
            swapFirst.alpha = 1f;
        }
        if (swapSecond != null)
        {
            swapSecond.text  = _initB ?? "";
            swapSecond.color = _initColB;
            swapSecond.alpha = 1f;
        }
        if (swapThird != null)
        {
            swapThird.text  = _initC ?? "";
            swapThird.color = _initColC;
            swapThird.alpha = 1f;
        }
    }

    void HideVisual(GameObject go)
    {
        if (go == null) return;
        if (!go.activeSelf) go.SetActive(true);
        Image img = go.GetComponentInChildren<Image>(true);
        if (img != null)
        {
            PrepareImageForFill(img);
            img.fillAmount = 0f;
            return;
        }
        Transform tf = go.transform;
        tf.localScale = new Vector3(0f, tf.localScale.y, tf.localScale.z);
    }

    void PrepareImageForFill(Image img)
    {
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = 0;
        img.fillAmount = 0f;
    }

    IEnumerator CoImageFill(Image img, float target, float dur)
    {
        float start = img.fillAmount;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            img.fillAmount = Mathf.Lerp(start, target, t / dur);
            yield return null;
        }
        img.fillAmount = target;
    }

    IEnumerator CoScaleX(Transform tf, float targetX, float dur, float y, float z)
    {
        float startX = tf.localScale.x;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            tf.localScale = new Vector3(Mathf.Lerp(startX, targetX, t / dur), y, z);
            yield return null;
        }
        tf.localScale = new Vector3(targetX, y, z);
    }

    IEnumerator FadeTMP(TMP_Text tmp, float targetAlpha, float dur)
    {
        if (tmp == null) yield break;
        float startAlpha = tmp.alpha;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            tmp.alpha = Mathf.Lerp(startAlpha, targetAlpha, t / dur);
            yield return null;
        }
        tmp.alpha = targetAlpha;
    }
}
