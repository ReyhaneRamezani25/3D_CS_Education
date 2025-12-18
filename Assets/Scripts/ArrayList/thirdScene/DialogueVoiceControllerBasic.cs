using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DialogueVoiceControllerBasic : MonoBehaviour, IDialogueSequencer
{
    // ایونت‌ها
    public event System.Action<int> OnDialogueStart;
    public event System.Action<int> OnDialogueEnd;
    public event System.Action OnSequenceFinished;

    [Header("Items (order matters)")]
    public List<DialogueItemBasic> items = new List<DialogueItemBasic>();

    [Header("Audio")]
    public AudioSource audioSource;
    [Tooltip("اگر فاصله‌ی بعد از آیتم مشخص نشده باشد از این مقدار استفاده می‌شود.")]
    public float defaultGapSeconds = 0.5f;

    [Header("Flow")]
    [SerializeField] private bool playOnStart = false;
    public bool loop = false;

    [Header("Activation Mode")]
    [Tooltip("اگر برای هر آیتم Root جدا تعریف شده و مشترک نیست، همان Root فعال/غیرفعال می‌شود؛ وگرنه خود Text فعال/غیرفعال می‌شود.")]
    public bool usePerItemRootIfAvailable = true;
    public GameObject commonContainer;

    // وضعیت داخلی
    Coroutine _runner;
    bool _rootsLookShared = false;
    int _currentIndex = -1;
    bool _skipCurrent = false;

    // ====== Properties برای مصرف بیرونی ======
    public bool IsRunning => _runner != null;
    /// <summary>ایندکس فعلی دیالوگ در حال پخش؛ اگر چیزی پخش نمی‌شود مقدار -1 است.</summary>
    public int CurrentIndex => _currentIndex;

    void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.clip = null;
        audioSource.Stop();

        if (commonContainer == null) commonContainer = TryInferCommonContainer();
        AnalyzeSharedRoots();
        SetOnlyActive(-1); // همه خاموش
    }

    void Start()
    {
        if (playOnStart) Play();
    }

    // ===========================
    //    IDialogueSequencer Impl
    // ===========================

    public void Play()
    {
        StopSequence();                 // پاکسازی حالت قبلی
        _runner = StartCoroutine(RunSequence());
    }

    public void StopSequence()
    {
        // توقف کوروتین‌ها
        if (_runner != null)
        {
            StopCoroutine(_runner);
            _runner = null;
        }

        // توقف صدا
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = null;
        }

        // ریست وضعیت
        _skipCurrent = false;
        _currentIndex = -1;

        // خاموش کردن نمایش
        SetOnlyActive(-1);
    }

    // ===========================
    //         Core Logic
    // ===========================

    IEnumerator RunSequence()
    {
        if (!Validate()) yield break;

        do
        {
            for (int i = 0; i < items.Count; i++)
            {
                _currentIndex = i;
                var it = items[i];

                SetOnlyActive(i);
                OnDialogueStart?.Invoke(i);

                yield return PlayOneItem(it);

                OnDialogueEnd?.Invoke(i);
                SetOnlyActive(-1);

                float gap = it.gapAfterSeconds > 0 ? it.gapAfterSeconds : defaultGapSeconds;
                if (gap > 0f) yield return new WaitForSecondsRealtime(gap);

                if (_runner == null) yield break; // اگر بیرون StopSequence شده باشد
            }
        }
        while (loop);

        SetOnlyActive(-1);
        _runner = null;
        _currentIndex = -1;
        OnSequenceFinished?.Invoke();
    }

    IEnumerator PlayOneItem(DialogueItemBasic it)
    {
        _skipCurrent = false;

        if (audioSource != null && it.voice != null)
        {
            audioSource.Stop();
            audioSource.clip = it.voice;
            audioSource.time = 0f;
            audioSource.Play();

            // منتظر بمان تا صدا تمام شود یا skip بخورد
            while (audioSource != null && audioSource.isPlaying && !_skipCurrent)
                yield return null;

            // نگه‌داشت پس از صدا (در صورت عدم skip)
            if (!_skipCurrent && it.extraHoldAfterVoice > 0f)
                yield return new WaitForSecondsRealtime(it.extraHoldAfterVoice);
        }
        else
        {
            if (it.fixedShowSeconds > 0f)
                yield return new WaitForSecondsRealtime(it.fixedShowSeconds);
        }

        _skipCurrent = false; // مصرف شد
    }

    // ===========================
    //         Helpers
    // ===========================

    bool Validate()
    {
        if (items == null || items.Count == 0)
        {
            Debug.LogWarning("[DialogueVoiceControllerBasic] Items list is empty!");
            return false;
        }

        if (audioSource == null)
            Debug.LogWarning("[DialogueVoiceControllerBasic] No AudioSource set.");

        AnalyzeSharedRoots();

        if (_rootsLookShared && usePerItemRootIfAvailable)
        {
            Debug.LogWarning("[DialogueVoiceControllerBasic] Shared roots detected; switching to Text-only activation.");
            usePerItemRootIfAvailable = false;
        }

        if (commonContainer == null) commonContainer = TryInferCommonContainer();

        return true;
    }

    void AnalyzeSharedRoots()
    {
        _rootsLookShared = false;
        if (items == null) return;
        var seen = new HashSet<GameObject>();
        int sharedCount = 0;
        foreach (var it in items)
        {
            if (it == null || it.root == null) continue;
            if (!seen.Add(it.root)) sharedCount++;
        }
        _rootsLookShared = sharedCount > 0;
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
