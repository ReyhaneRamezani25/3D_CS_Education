using System.Collections;
using UnityEngine;
using TMPro;

[System.Serializable]
public class TimedCanvasEntry
{
    public GameObject arrow;
    public float arrowShowDelay = 0f;
    public TMP_Text label;
    public string overrideText = "";
    public float labelShowDelay = 0f;
}

public class DialogueCanvasArrowsSequence : MonoBehaviour
{
    [SerializeField] private DialogueVoiceControllerBasic controller;
    [SerializeField] private int targetDialogueIndex = 8;
    [SerializeField] private bool oneBasedIndex = true;

    [SerializeField] private TimedCanvasEntry[] entries;

    [SerializeField] private bool startHidden = true;
    [SerializeField] private bool revertOnDialogueEnd = true;
    [SerializeField] private bool debugApplyNow = false;

    private bool[] origArrowActive;
    private bool[] origLabelActive;
    private string[] origLabelTexts;

    private Coroutine[] arrowRoutines;
    private Coroutine[] labelRoutines;

    private void Awake()
    {
        CacheOriginals();
        if (startHidden) HideAllNow();
        if (debugApplyNow) StartSequence();
    }

    private void OnEnable()
    {
        if (controller == null) controller = GetComponent<DialogueVoiceControllerBasic>();
        if (controller != null)
        {
            controller.OnDialogueStart += OnDialogueStart;
            controller.OnDialogueEnd += OnDialogueEnd;
        }
    }

    private void OnDisable()
    {
        if (controller != null)
        {
            controller.OnDialogueStart -= OnDialogueStart;
            controller.OnDialogueEnd -= OnDialogueEnd;
        }
        StopAllCoroutinesSafe();
        RevertAll();
    }

    private void OnDialogueStart(int index)
    {
        int normalized = oneBasedIndex ? (index + 1) : index;
        if (normalized != targetDialogueIndex) return;
        StartSequence();
    }

    private void OnDialogueEnd(int index)
    {
        int normalized = oneBasedIndex ? (index + 1) : index;
        if (normalized != targetDialogueIndex) return;
        if (revertOnDialogueEnd)
        {
            StopAllCoroutinesSafe();
            RevertAll();
        }
    }

    private void StartSequence()
    {
        StopAllCoroutinesSafe();
        if (entries == null || entries.Length == 0) return;

        for (int i = 0; i < entries.Length; i++)
        {
            var e = entries[i];
            if (e == null) continue;

            if (e.arrow != null)
                arrowRoutines[i] = StartCoroutine(ShowArrowAfterDelay(e.arrow, Mathf.Max(0f, e.arrowShowDelay)));

            if (e.label != null)
                labelRoutines[i] = StartCoroutine(ShowLabelAfterDelay(e.label, e.overrideText, Mathf.Max(0f, e.labelShowDelay)));
        }
    }

    private IEnumerator ShowArrowAfterDelay(GameObject arrow, float delay)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        arrow.SetActive(true);
    }

    private IEnumerator ShowLabelAfterDelay(TMP_Text label, string overrideText, float delay)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        label.gameObject.SetActive(true);
        label.enabled = true;
        if (!string.IsNullOrEmpty(overrideText)) label.text = overrideText;
        var c = label.color; c.a = 1f; label.color = c;
        var cv = label.GetComponentInParent<Canvas>();
        if (cv != null && cv.renderMode == RenderMode.WorldSpace && cv.worldCamera == null) cv.worldCamera = Camera.main;
        label.ForceMeshUpdate(true);
    }

    private void CacheOriginals()
    {
        int n = entries != null ? entries.Length : 0;
        origArrowActive = new bool[n];
        origLabelActive = new bool[n];
        origLabelTexts = new string[n];
        arrowRoutines = new Coroutine[n];
        labelRoutines = new Coroutine[n];

        for (int i = 0; i < n; i++)
        {
            var e = entries[i];
            if (e == null) continue;

            if (e.arrow != null) origArrowActive[i] = e.arrow.activeSelf;

            if (e.label != null)
            {
                origLabelActive[i] = e.label.gameObject.activeSelf;
                origLabelTexts[i] = e.label.text;
            }
        }
    }

    private void HideAllNow()
    {
        if (entries == null) return;
        for (int i = 0; i < entries.Length; i++)
        {
            var e = entries[i];
            if (e == null) continue;
            if (e.arrow != null) e.arrow.SetActive(false);
            if (e.label != null) e.label.gameObject.SetActive(false);
        }
    }

    private void RevertAll()
    {
        if (entries == null) return;

        for (int i = 0; i < entries.Length; i++)
        {
            var e = entries[i];
            if (e == null) continue;

            if (e.arrow != null) e.arrow.SetActive(origArrowActive[i]);

            if (e.label != null)
            {
                e.label.text = origLabelTexts[i] ?? "";
                e.label.gameObject.SetActive(origLabelActive[i]);
                e.label.ForceMeshUpdate(true);
            }
        }

        if (startHidden)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                var e = entries[i];
                if (e == null) continue;
                if (e.arrow != null) e.arrow.SetActive(false);
                if (e.label != null) e.label.gameObject.SetActive(false);
            }
        }
    }

    private void StopAllCoroutinesSafe()
    {
        if (arrowRoutines != null)
        {
            for (int i = 0; i < arrowRoutines.Length; i++)
                if (arrowRoutines[i] != null) StopCoroutine(arrowRoutines[i]);
        }
        if (labelRoutines != null)
        {
            for (int i = 0; i < labelRoutines.Length; i++)
                if (labelRoutines[i] != null) StopCoroutine(labelRoutines[i]);
        }
        arrowRoutines = arrowRoutines != null ? new Coroutine[arrowRoutines.Length] : null;
        labelRoutines = labelRoutines != null ? new Coroutine[labelRoutines.Length] : null;
    }
}
