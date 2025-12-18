using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MultiTextsShowOnDialogue : MonoBehaviour
{
    [Header("Dialogue Settings")]
    public DialogueVoiceControllerBasic controller;
    [Tooltip("Index 5 = sixth dialogue (0-based).")]
    public int showOnIndex = 5;

    [Header("Main Texts (2)")]
    public TMP_Text textA;
    public TMP_Text textB;

    [Header("Point Lights (2)")]
    public Light pointLightA;
    public Light pointLightB;

    [Header("Texts To Clear (4)")]
    [Tooltip("These texts will be cleared after a specified delay.")]
    public List<TMP_Text> textsToClear = new List<TMP_Text>();
    [Tooltip("Delay after dialogue starts before clearing these texts.")]
    public float clearDelayAfterStart = 1.5f;

    [Header("Dot Arrow")]
    public GameObject dotArrow;
    [Tooltip("Delay after dialogue start before the dot arrow appears.")]
    public float arrowDelayAfterStart = 0.5f;
    [Tooltip("Duration of left-to-right reveal.")]
    public float arrowRevealDuration = 0.6f;
    public AnimationCurve arrowEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Timing")]
    [Tooltip("Optional delay after dialogue starts before showing main texts/lights.")]
    public float showDelayAfterStart = 0f;
    [Tooltip("Hide texts and lights before the target dialogue starts.")]
    public bool hideBeforeStart = true;

    Coroutine _runner;
    Coroutine _clearRunner;
    Coroutine _arrowRunner;

    // for restore
    List<string> _originalTexts = new List<string>();
    bool _snapshotTaken = false;

    Image _arrowImage;

    void Awake()
    {
        TakeSnapshot();

        if (dotArrow != null)
        {
            _arrowImage = dotArrow.GetComponentInChildren<Image>(true);
            PrepareArrowHidden();
        }

        if (hideBeforeStart)
            HideAllInstant();
    }

    void OnEnable()
    {
        if (controller != null)
        {
            controller.OnDialogueStart += HandleStart;
            controller.OnDialogueEnd += HandleEnd;
        }
    }

    void OnDisable()
    {
        if (controller != null)
        {
            controller.OnDialogueStart -= HandleStart;
            controller.OnDialogueEnd -= HandleEnd;
        }
    }

    void HandleStart(int index)
    {
        StopAllCoroutines();

        if (index == showOnIndex)
        {
            _runner = StartCoroutine(CoShow());
            _clearRunner = StartCoroutine(CoClearTextsAfterDelay());
            _arrowRunner = StartCoroutine(CoShowArrowAfterDelay());
        }
        else if (hideBeforeStart)
        {
            HideAllInstant();
            HideArrowInstant();
        }
    }

    void HandleEnd(int index)
    {
        if (index != showOnIndex) return;

        StopAllCoroutines();
        HideAllInstant();
        HideArrowInstant();
        RestoreOriginalTexts();
    }

    IEnumerator CoShow()
    {
        if (showDelayAfterStart > 0f)
            yield return new WaitForSecondsRealtime(showDelayAfterStart);

        ShowAllInstant();
    }

    IEnumerator CoClearTextsAfterDelay()
    {
        if (clearDelayAfterStart > 0f)
            yield return new WaitForSecondsRealtime(clearDelayAfterStart);

        foreach (var t in textsToClear)
            if (t != null) t.text = "";
    }

    // ---------- DOT ARROW ----------
    IEnumerator CoShowArrowAfterDelay()
    {
        if (dotArrow == null) yield break;

        if (arrowDelayAfterStart > 0f)
            yield return new WaitForSecondsRealtime(arrowDelayAfterStart);

        ShowArrowWithReveal();
    }

    void PrepareArrowHidden()
    {
        if (dotArrow == null) return;

        dotArrow.SetActive(false);
        if (_arrowImage != null)
        {
            _arrowImage.type = Image.Type.Filled;
            _arrowImage.fillMethod = Image.FillMethod.Horizontal;
            _arrowImage.fillOrigin = 0;
            _arrowImage.fillAmount = 0f;
        }
    }

    void ShowArrowWithReveal()
    {
        if (dotArrow == null) return;

        dotArrow.SetActive(true);

        if (_arrowImage != null)
        {
            StartCoroutine(CoImageFill(_arrowImage, 1f, arrowRevealDuration));
        }
        else
        {
            Transform tf = dotArrow.transform;
            Vector3 orig = tf.localScale;
            tf.localScale = new Vector3(0f, orig.y, orig.z);
            StartCoroutine(CoScaleX(tf, orig.x, arrowRevealDuration, orig.y, orig.z));
        }
    }

    void HideArrowInstant()
    {
        if (dotArrow == null) return;

        if (_arrowImage != null)
            _arrowImage.fillAmount = 0f;

        dotArrow.SetActive(false);
    }

    IEnumerator CoImageFill(Image img, float target, float dur)
    {
        float start = img.fillAmount;
        float t = 0f;
        dur = Mathf.Max(0.0001f, dur);

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            img.fillAmount = Mathf.Lerp(start, target, arrowEase.Evaluate(k));
            yield return null;
        }

        img.fillAmount = target;
    }

    IEnumerator CoScaleX(Transform tf, float targetX, float dur, float y, float z)
    {
        float startX = tf.localScale.x;
        float t = 0f;
        dur = Mathf.Max(0.0001f, dur);

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            tf.localScale = new Vector3(Mathf.Lerp(startX, targetX, arrowEase.Evaluate(k)), y, z);
            yield return null;
        }

        tf.localScale = new Vector3(targetX, y, z);
    }

    // ---------- SHOW/HIDE ----------
    void ShowAllInstant()
    {
        SetAlpha(textA, 1f);
        SetAlpha(textB, 1f);

        if (pointLightA != null) pointLightA.enabled = true;
        if (pointLightB != null) pointLightB.enabled = true;
    }

    void HideAllInstant()
    {
        SetAlpha(textA, 0f);
        SetAlpha(textB, 0f);

        if (pointLightA != null) pointLightA.enabled = false;
        if (pointLightB != null) pointLightB.enabled = false;
    }

    // ---------- RESTORE ----------
    void TakeSnapshot()
    {
        if (_snapshotTaken) return;

        _originalTexts.Clear();
        foreach (var t in textsToClear)
            _originalTexts.Add(t ? t.text : "");

        _snapshotTaken = true;
    }

    void RestoreOriginalTexts()
    {
        if (!_snapshotTaken) return;

        for (int i = 0; i < textsToClear.Count; i++)
        {
            var t = textsToClear[i];
            if (t == null) continue;

            if (i < _originalTexts.Count)
                t.text = _originalTexts[i];
        }
    }

    void SetAlpha(TMP_Text t, float a)
    {
        if (t != null) t.alpha = a;
    }
}
