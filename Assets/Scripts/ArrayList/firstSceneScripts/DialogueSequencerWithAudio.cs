using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[System.Serializable]
public class DialogueItem
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
    public float gapAfterSeconds = 5f;
}

public class DialogueSequencerWithAudio : MonoBehaviour, IDialogueSequencer
{
    [Header("Items (order matters)")]
    public List<DialogueItem> items = new List<DialogueItem>();

    [Header("Audio")]
    public AudioSource audioSource;
    public float defaultGap = 5f;

    [Header("Flow")]
    public bool playOnStart = true;
    public bool loop = false;

    [Header("UI Helpers (optional)")]
    public ParentImageAutoWidth parentSizer;
    public bool waitEndOfFrameBeforeMeasure = true;

    [Header("Rail Cue (optional)")]
    public RailCueHighlighter railCue;

    [Header("NOE Cues (optional)")]
    [Tooltip("LED controller related to dialogue 6")]
    public NOECueController noeCue6;
    [Tooltip("LED controller related to dialogue 7")]
    public NOECueController noeCue7;

    [Header("Activation Mode")]
    public bool usePerItemRootIfAvailable = false;
    public GameObject commonContainer;

    Coroutine _runner;
    bool _rootsLookShared = false;

    public bool IsRunning => _runner != null;

    void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        if (commonContainer == null) commonContainer = TryInferCommonContainer();
        SetOnlyActive(-1);
    }

    void Start()
    {
        if (playOnStart) Play();
    }

    public void Play()
    {
        if (_runner != null) StopCoroutine(_runner);
        _runner = StartCoroutine(Run());
    }

    public void StopSequence()
    {
        if (_runner != null) StopCoroutine(_runner);
        _runner = null;

        if (audioSource != null) audioSource.Stop();
        SetOnlyActive(-1);
        parentSizer?.Refresh();
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

                if (waitEndOfFrameBeforeMeasure) yield return new WaitForEndOfFrame();
                if (parentSizer != null && it.text != null)
                {
                    parentSizer.sourceText = it.text;
                    parentSizer.Refresh();
                }

                float showTime = it.fixedShowSeconds;
                if (it.voice != null && audioSource != null)
                {
                    audioSource.Stop();
                    audioSource.PlayOneShot(it.voice);
                    if (it.useVoiceLength) showTime = it.voice.length;
                }

                if (railCue != null)
                {
                    if (i == 2) railCue.PlayCue(5, showTime);
                    if (i == 3) railCue.PlayCue(4, showTime);
                }

                if (i == 5 && noeCue6 != null)
                {
                    float noeDuration = showTime + it.extraHoldAfterVoice;
                    noeCue6.PlayCueForDialogue(6, noeDuration);
                }
                else if (i == 6 && noeCue7 != null)
                {
                    float noeDuration = showTime + it.extraHoldAfterVoice;
                    noeCue7.PlayCueForDialogue(7, noeDuration);
                }

                if (showTime > 0f) yield return new WaitForSeconds(showTime);
                if (it.extraHoldAfterVoice > 0f) yield return new WaitForSeconds(it.extraHoldAfterVoice);

                SetOnlyActive(-1);
                parentSizer?.Refresh();

                float gap = it.gapAfterSeconds > 0f ? it.gapAfterSeconds : defaultGap;
                if (gap > 0f) yield return new WaitForSeconds(gap);
            }
        }
        while (loop);

        SetOnlyActive(-1);
        parentSizer?.Refresh();
        _runner = null;
    }

    bool Validate()
    {
        if (items == null || items.Count == 0)
        {
            Debug.LogWarning("[DialogueSequencerWithAudio] items list is empty.");
            return false;
        }

        var seen = new HashSet<GameObject>();
        int sharedCount = 0;
        for (int i = 0; i < items.Count; i++)
        {
            var it = items[i];
            if (it.text == null)
                Debug.LogWarning($"[DialogueSequencerWithAudio] Item {i} has no Text assigned.");

            if (it.root != null && !seen.Add(it.root))
                sharedCount++;
        }
        _rootsLookShared = (sharedCount > 0);
        if (_rootsLookShared && usePerItemRootIfAvailable)
        {
            Debug.LogWarning("[DialogueSequencerWithAudio] Multiple items share the SAME root. Switching to Text-only activation.");
            usePerItemRootIfAvailable = false;
        }

        if (audioSource == null)
            Debug.LogWarning("[DialogueSequencerWithAudio] No AudioSource found; voices will not play.");

        if (commonContainer == null) commonContainer = TryInferCommonContainer();
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
}
