using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public class DialogueItemBasic
{
    [Header("UI")]
    public TextMeshProUGUI text;
    public GameObject root;

    [Header("Voice")]
    public AudioClip voice;
    public bool useVoiceLength = true;
    public float extraHoldAfterVoice = 0f;

    [Header("Timing")]
    public float fixedShowSeconds = 10f;
    public float gapAfterSeconds  = 5f;
}

public class DialogueSequencerBasic : MonoBehaviour, IDialogueSequencer
{
    public event System.Action<int> OnDialogueStart;
    public event System.Action<int> OnDialogueEnd;

    [Header("Items (order matters)")]
    public List<DialogueItemBasic> items = new List<DialogueItemBasic>();

    [Header("Audio")]
    public AudioSource audioSource;
    public float defaultGap = 5f;

    [Header("Flow")]
    [SerializeField] private bool playOnStart = false;
    public bool loop = false;

    [Header("UI Helpers (optional)")]
    public ParentImageAutoWidth parentSizer;
    public bool waitEndOfFrameBeforeMeasure = true;

    [Header("Activation Mode")]
    public bool usePerItemRootIfAvailable = false;
    public GameObject commonContainer;

    [Header("Sign Arrow (hint)")]
    public GameObject hintContainer;
    public GameObject arrow;
    public float fadeDuration = 0.5f;
    public bool scaleFallbackForNonUI = true;
    public bool useLeftToRightRevealForHint = false;
    public float arrowRevealDuration = 0.6f;

    [Header("Second Arrow (L2R)")]
    public GameObject secondArrow;
    public bool useLeftToRightRevealForSecond = true;

    [Header("Early Lead-in (sign & text before L2R)")]
    [Tooltip("This helper text appears slightly earlier than L2R using a fade-in.")]
    public TMP_Text earlyLeadText;
    [Tooltip("How many seconds before the midpoint of dialogue 2 should the sign arrow and this text start.")]
    public float earlyLeadSeconds = 0.35f;
    [Tooltip("Fade duration for the helper text.")]
    public float earlyLeadFade = 0.25f;

    [Header("Cube Labels (swap TEXT sequentially)")]
    [Tooltip("6 TMPs in order of cubes 0..5 (use an empty TMP for index 0 if needed).")]
    public TMP_Text[] cubeLabelsByIndex = new TMP_Text[6];

    [Tooltip("Fade-out/in duration for each label (seconds).")]
    public float labelFadeDuration = 0.4f;

    [Tooltip("Short delay between each move step (seconds).")]
    public float labelStepDelay = 0.15f;

    [Header("Label Clear Option")]
    public TMP_Text[] labelsToClearAfterMove;

    [Header("Highlight moved text (per step)")]
    public Color movedHighlightColor = Color.yellow;
    public float movedHighlightDuration = 0.15f;
    public bool revertColorAfterHighlight = true;

    Coroutine _runner;
    Coroutine _fadeRunner;
    Coroutine _midSecondRunner;
    bool _rootsLookShared = false;

    CanvasGroup _cg;
    List<TMP_Text> _hintTMPs;
    bool _hasUIOrTMP;
    Transform _hintT;
    Vector3 _hintInitialScale;

    string[] _initialLabelTexts;
    Color[]  _initialLabelColors;
    bool _initialBackedUp = false;

    Vector3 _secondArrowInitialScale;

    public bool IsRunning => _runner != null;

    void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.clip = null;
        audioSource.Stop();
        audioSource.spatialBlend = 0f;

        if (commonContainer == null) commonContainer = TryInferCommonContainer();
        SetOnlyActive(-1);

        if (HintTarget != null) HintTarget.SetActive(false);

        if (secondArrow != null)
        {
            _secondArrowInitialScale = secondArrow.transform.localScale;
            HideArrowInstant(secondArrow);
        }

        if (earlyLeadText != null) earlyLeadText.alpha = 0f;
    }

    void Start()
    {
        if (playOnStart) Play();
    }

    public void Play()
    {
        if (_runner != null) StopCoroutine(_runner);
        BackupInitialLabels();
        _runner = StartCoroutine(Run());
    }

    public void StopSequence()
    {
        if (_runner != null) StopCoroutine(_runner);
        if (_midSecondRunner != null) StopCoroutine(_midSecondRunner);
        _runner = null; _midSecondRunner = null;

        if (audioSource != null) audioSource.Stop();
        SetOnlyActive(-1);
        parentSizer?.Refresh();

        if (HintTarget != null) HintTarget.SetActive(false);
        if (secondArrow != null) HideArrowInstant(secondArrow);
        if (earlyLeadText != null) earlyLeadText.alpha = 0f;
    }

    IEnumerator Run()
    {
        if (!Validate()) yield break;

        do
        {
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];

                SetOnlyActive(i);

                OnDialogueStart?.Invoke(i);

                if (waitEndOfFrameBeforeMeasure) yield return new WaitForEndOfFrame();
                if (parentSizer != null && it.text != null)
                {
                    parentSizer.sourceText = it.text;
                    parentSizer.Refresh();
                }

                if (i == 1)
                {
                    StartMidOfSecond(it);
                }

                if (it.voice != null && audioSource != null)
                {
                    audioSource.Stop();
                    var clip = it.voice;
                    audioSource.clip = clip;
                    audioSource.time = 0f;
                    audioSource.Play();

                    while (audioSource != null && audioSource.isPlaying && audioSource.clip == clip)
                        yield return null;

                    if (it.extraHoldAfterVoice > 0f)
                        yield return new WaitForSecondsRealtime(it.extraHoldAfterVoice);
                }
                else
                {
                    if (it.fixedShowSeconds > 0f)
                        yield return new WaitForSeconds(it.fixedShowSeconds);
                }

                if (i == 2)
                {
                    if (HintTarget != null)
                    {
                        if (useLeftToRightRevealForHint)
                            HintTarget.SetActive(false);
                        else
                            HideHintWithFadeThenDisable();
                    }
                    if (earlyLeadText != null) earlyLeadText.alpha = 0f;

                    if (secondArrow != null) HideArrowInstant(secondArrow);

                    RestoreInitialLabels();
                }

                OnDialogueEnd?.Invoke(i);

                SetOnlyActive(-1);
                parentSizer?.Refresh();

                float gap = it.gapAfterSeconds > 0f ? it.gapAfterSeconds : defaultGap;
                if (gap > 0f) yield return new WaitForSecondsRealtime(gap);
            }
        }
        while (loop);

        if (HintTarget != null) HintTarget.SetActive(false);
        if (secondArrow != null) HideArrowInstant(secondArrow);
        if (earlyLeadText != null) earlyLeadText.alpha = 0f;

        SetOnlyActive(-1);
        parentSizer?.Refresh();
        _runner = null;
    }

    void StartMidOfSecond(DialogueItemBasic it)
    {
        if (_midSecondRunner != null) StopCoroutine(_midSecondRunner);

        float dur = 0f;
        if (it.voice != null && audioSource != null && it.voice.length > 0f)
            dur = it.voice.length + Mathf.Max(0f, it.extraHoldAfterVoice);
        else
            dur = Mathf.Max(0f, it.fixedShowSeconds);

        float half = Mathf.Max(0f, dur * 0.5f);
        _midSecondRunner = StartCoroutine(CoMidOfSecondWithLead(half));
    }

    IEnumerator CoMidOfSecondWithLead(float halfSeconds)
    {
        float lead = Mathf.Max(0f, earlyLeadSeconds);
        lead = Mathf.Min(lead, halfSeconds);

        if (halfSeconds - lead > 0f)
            yield return new WaitForSecondsRealtime(halfSeconds - lead);

        if (HintTarget != null)
        {
            if (!HintTarget.activeSelf) HintTarget.SetActive(true);

            if (useLeftToRightRevealForHint)
                RevealArrowLeftToRight(HintTarget, Mathf.Max(0.0001f, arrowRevealDuration * 0.8f));
            else
                ShowHintWithFadeFromHidden();
        }

        if (earlyLeadText != null)
            yield return StartCoroutine(FadeTMP(earlyLeadText, 1f, Mathf.Max(0.0001f, earlyLeadFade)));

        if (lead > 0f)
            yield return new WaitForSecondsRealtime(lead);

        if (secondArrow != null)
        {
            if (useLeftToRightRevealForSecond)
                RevealArrowLeftToRight(secondArrow, arrowRevealDuration);
            else
                EnsureArrowVisible(secondArrow);
        }

        string[] snapshot = SnapshotLabels();
        yield return StartCoroutine(LabelSwapSequence(snapshot));

        yield return new WaitForSecondsRealtime(1f);

        if (cubeLabelsByIndex != null && cubeLabelsByIndex.Length > 5 && cubeLabelsByIndex[5] != null)
        {
            var t5 = cubeLabelsByIndex[5];
            t5.text  = "23";
            t5.color = Color.red;
            t5.alpha = 1f;
        }

        _midSecondRunner = null;
    }

    bool Validate()
    {
        if (items == null || items.Count == 0)
        {
            Debug.LogWarning("[DialogueSequencerBasic] items list is empty.");
            return false;
        }

        var seen = new HashSet<GameObject>();
        int sharedCount = 0;
        for (int i = 0; i < items.Count; i++)
        {
            var it = items[i];
            if (it.text == null)
                Debug.LogWarning($"[DialogueSequencerBasic] Item {i} has no Text assigned.");

            if (it.root != null && !seen.Add(it.root))
                sharedCount++;
        }
        _rootsLookShared = (sharedCount > 0);
        if (_rootsLookShared && usePerItemRootIfAvailable)
        {
            Debug.LogWarning("[DialogueSequencerBasic] Multiple items share the SAME root. Switching to Text-only activation.");
            usePerItemRootIfAvailable = false;
        }

        if (audioSource == null)
            Debug.LogWarning("[DialogueSequencerBasic] No AudioSource found; voices will not play.");

        if (commonContainer == null) commonContainer = TryInferCommonContainer();

        if (cubeLabelsByIndex == null || cubeLabelsByIndex.Length < 6)
            Debug.LogWarning("[DialogueSequencerBasic] cubeLabelsByIndex must have 6 elements (0..5).");

        return true;
    }

    void SetOnlyActive(int indexActive)
    {
        bool anyActive = indexActive >= 0;
        if (commonContainer != null) commonContainer.SetActive(anyActive);

        for (int i = 0; i < items.Count; i++)
        {
            var it = items[i];
            bool active = (i == indexActive);

            if (usePerItemRootIfAvailable && it.root != null && !_rootsLookShared)
                it.root.SetActive(active);
            else if (it.text != null)
                it.text.gameObject.SetActive(active);
        }
    }

    GameObject TryInferCommonContainer()
    {
        if (items == null || items.Count == 0) return null;
        Transform parent = null;
        foreach (var it in items)
        {
            if (it?.text == null) continue;
            if (parent == null) parent = it.text.transform.parent;
            else if (it.text.transform.parent != parent) return null;
        }
        return parent ? parent.gameObject : null;
    }

    GameObject HintTarget => hintContainer != null ? hintContainer : arrow;

    void CacheFadables(GameObject target)
    {
        _hintT = target.transform;
        _hintInitialScale = _hintT.localScale;

        _cg = target.GetComponent<CanvasGroup>();
        if (_cg == null) _cg = target.GetComponentInChildren<CanvasGroup>(true);

        _hintTMPs = new List<TMP_Text>(target.GetComponentsInChildren<TMP_Text>(true));

        bool hasUI = target.GetComponentInChildren<CanvasRenderer>(true) != null;
        bool hasTMP = _hintTMPs.Count > 0;
        _hasUIOrTMP = hasUI || hasTMP;
    }

    void ShowHintWithFadeFromHidden()
    {
        var target = HintTarget;
        if (target == null) return;

        CacheFadables(target);

        Image img = target.GetComponentInChildren<Image>(true);
        if (img != null)
        {
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = 0;
            img.fillAmount = 1f;
        }

        if (_cg == null) _cg = target.AddComponent<CanvasGroup>();
        _cg.alpha = 0f;

        if (!target.activeSelf) target.SetActive(true);

        if (_fadeRunner != null) StopCoroutine(_fadeRunner);
        _fadeRunner = StartCoroutine(FadeInRoutine());
    }

    void HideHintWithFadeThenDisable()
    {
        var target = HintTarget;
        if (target == null || !target.activeSelf) return;

        CacheFadables(target);

        if (_fadeRunner != null) StopCoroutine(_fadeRunner);
        _fadeRunner = StartCoroutine(FadeOutRoutine(() =>
        {
            if (_cg != null) _cg.alpha = 0f;
            if (_hintTMPs != null) foreach (var t in _hintTMPs) if (t) t.alpha = 0f;
            target.SetActive(false);
        }));
    }

    IEnumerator FadeInRoutine()
    {
        float dur = Mathf.Max(0.0001f, fadeDuration);
        float t = 0f;

        while (t < dur)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / dur);
            if (_cg != null) _cg.alpha = a;
            if (_hintTMPs != null) foreach (var tx in _hintTMPs) if (tx) tx.alpha = a;
            yield return null;
        }

        if (_cg != null) _cg.alpha = 1f;
        if (_hintTMPs != null) foreach (var tx in _hintTMPs) if (tx) tx.alpha = 1f;
    }

    IEnumerator FadeOutRoutine(System.Action onDone)
    {
        float dur = Mathf.Max(0.0001f, fadeDuration);
        float t = 0f;

        float startAlpha = 1f;
        if (_cg != null) startAlpha = _cg.alpha;
        else if (_hintTMPs != null && _hintTMPs.Count > 0) startAlpha = _hintTMPs[0] ? _hintTMPs[0].alpha : 1f;

        while (t < dur)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(startAlpha, 0f, t / dur);
            if (_cg != null) _cg.alpha = a;
            if (_hintTMPs != null) foreach (var tx in _hintTMPs) if (tx) tx.alpha = a;
            yield return null;
        }

        if (_cg != null) _cg.alpha = 0f;
        if (_hintTMPs != null) foreach (var tx in _hintTMPs) if (tx) tx.alpha = 0f;

        onDone?.Invoke();
    }

    void RevealArrowLeftToRight(GameObject target, float duration)
    {
        if (target == null) return;
        if (!target.activeSelf) target.SetActive(true);

        Image img = target.GetComponentInChildren<Image>(true);
        if (img != null)
        {
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = 0;
            img.fillAmount = 0f;
            StartCoroutine(CoImageFill(img, 1f, Mathf.Max(0.0001f, duration)));
            return;
        }

        Transform tf = target.transform;
        Vector3 original =
            (target == secondArrow && _secondArrowInitialScale != Vector3.zero)
            ? _secondArrowInitialScale
            : tf.localScale;

        tf.localScale = new Vector3(0f, original.y, original.z);
        StartCoroutine(CoScaleX(tf, original.x, Mathf.Max(0.0001f, duration), original));
    }

    void HideArrowInstant(GameObject target)
    {
        if (target == null) return;
        if (!target.activeSelf) target.SetActive(true);

        Image img = target.GetComponentInChildren<Image>(true);
        if (img != null)
        {
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = 0;
            img.fillAmount = 0f;
            return;
        }

        var cgLocal = target.GetComponentInChildren<CanvasGroup>(true);
        var tmps = target.GetComponentsInChildren<TMP_Text>(true);
        if (cgLocal != null || (tmps != null && tmps.Length > 0))
        {
            if (cgLocal == null) cgLocal = target.AddComponent<CanvasGroup>();
            cgLocal.alpha = 0f;
            foreach (var t in tmps) if (t) t.alpha = 0f;
        }
        else
        {
            Transform tf = target.transform;
            Vector3 original =
                (target == secondArrow && _secondArrowInitialScale != Vector3.zero)
                ? _secondArrowInitialScale
                : tf.localScale;
            tf.localScale = new Vector3(0f, original.y, original.z);
        }
    }

    void EnsureArrowVisible(GameObject target)
    {
        if (target == null) return;
        if (!target.activeSelf) target.SetActive(true);

        Image img = target.GetComponentInChildren<Image>(true);
        if (img != null)
        {
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = 0;
            img.fillAmount = 1f;
            return;
        }

        var cg = target.GetComponentInChildren<CanvasGroup>(true);
        if (cg != null) cg.alpha = 1f;
        foreach (var t in target.GetComponentsInChildren<TMP_Text>(true)) if (t) t.alpha = 1f;

        Transform tf = target.transform;
        Vector3 original =
            (target == secondArrow && _secondArrowInitialScale != Vector3.zero)
            ? _secondArrowInitialScale
            : tf.localScale;
        if (tf.localScale.x == 0f)
            tf.localScale = new Vector3(original.x, original.y, original.z);
    }

    IEnumerator CoImageFill(Image img, float targetFill, float duration)
    {
        if (img == null) yield break;
        float start = img.fillAmount;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            if (img == null) yield break;
            img.fillAmount = Mathf.Lerp(start, targetFill, t / duration);
            yield return null;
        }
        if (img != null) img.fillAmount = targetFill;
    }

    IEnumerator CoScaleX(Transform tf, float targetX, float duration, Vector3 originalForYZ)
    {
        if (tf == null) yield break;
        float startX = tf.localScale.x;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            if (tf == null) yield break;
            float x = Mathf.Lerp(startX, targetX, t / duration);
            tf.localScale = new Vector3(x, originalForYZ.y, originalForYZ.z);
            yield return null;
        }
        if (tf != null)
            tf.localScale = new Vector3(targetX, originalForYZ.y, originalForYZ.z);
    }

    string[] SnapshotLabels()
    {
        if (cubeLabelsByIndex == null) return null;
        string[] original = new string[cubeLabelsByIndex.Length];
        for (int i = 0; i < cubeLabelsByIndex.Length; i++)
            original[i] = cubeLabelsByIndex[i] ? cubeLabelsByIndex[i].text : null;
        return original;
    }

    IEnumerator LabelSwapSequence(string[] original)
    {
        yield return SwapOneStep(1, 0, original);
        yield return SwapOneStep(2, 1, original);
        yield return SwapOneStep(3, 2, original);
        yield return SwapOneStep(4, 3, original);
        yield return SwapOneStep(5, 4, original);
    }

    IEnumerator SwapOneStep(int from, int to, string[] original)
    {
        if (cubeLabelsByIndex == null) yield break;
        if (from < 0 || from >= cubeLabelsByIndex.Length) yield break;
        if (to   < 0 || to   >= cubeLabelsByIndex.Length) yield break;

        TMP_Text lblFrom = cubeLabelsByIndex[from];
        TMP_Text lblTo   = cubeLabelsByIndex[to];
        if (lblFrom == null || lblTo == null) yield break;

        string newText = (original != null && from < original.Length) ? original[from] : lblFrom.text;

        yield return StartCoroutine(FadeTMP(lblTo, 0f, labelFadeDuration));

        lblTo.text = newText;

        if (labelsToClearAfterMove != null && System.Array.IndexOf(labelsToClearAfterMove, lblFrom) >= 0)
            lblFrom.text = "";

        yield return StartCoroutine(FadeTMP(lblTo, 1f, labelFadeDuration));

        Color prevColor = lblTo.color;
        lblTo.color = movedHighlightColor;

        float hl = Mathf.Max(0f, movedHighlightDuration);
        if (hl > 0f) yield return new WaitForSeconds(hl);

        if (revertColorAfterHighlight) lblTo.color = prevColor;

        float extraDelay = Mathf.Max(0f, labelStepDelay - hl);
        if (extraDelay > 0f) yield return new WaitForSeconds(extraDelay);
    }

    IEnumerator FadeTMP(TMP_Text tmp, float targetAlpha, float duration)
    {
        if (tmp == null) yield break;

        float startAlpha = tmp.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            if (tmp == null) yield break;
            tmp.alpha = Mathf.Lerp(startAlpha, targetAlpha, t / duration);
            yield return null;
        }
        if (tmp != null) tmp.alpha = targetAlpha;
    }

    void BackupInitialLabels()
    {
        if (cubeLabelsByIndex == null) return;

        int n = cubeLabelsByIndex.Length;
        _initialLabelTexts  = new string[n];
        _initialLabelColors = new Color[n];

        for (int i = 0; i < n; i++)
        {
            var t = cubeLabelsByIndex[i];
            _initialLabelTexts[i]  = t ? t.text  : null;
            _initialLabelColors[i] = t ? t.color : Color.white;
        }
        _initialBackedUp = true;
    }

    void RestoreInitialLabels()
    {
        if (!_initialBackedUp || cubeLabelsByIndex == null) return;

        int n = Mathf.Min(cubeLabelsByIndex.Length, _initialLabelTexts.Length);
        for (int i = 0; i < n; i++)
        {
            var t = cubeLabelsByIndex[i];
            if (!t) continue;
            t.text  = _initialLabelTexts[i];
            t.color = _initialLabelColors[i];
            t.alpha = 1f;
        }
    }
}
