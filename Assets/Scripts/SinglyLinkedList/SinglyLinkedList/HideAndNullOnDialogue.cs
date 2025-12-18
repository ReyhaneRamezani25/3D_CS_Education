using System.Collections;
using UnityEngine;
using TMPro;

public class HideAndNullOnDialogue : MonoBehaviour
{
    [SerializeField] private DialogueVoiceControllerBasic controller;
    [SerializeField] private int targetDialogueIndex = 7;
    [SerializeField] private bool oneBasedIndex = true;

    [SerializeField] private GameObject[] objectsToHide;

    [SerializeField] private TMP_Text targetText;
    [SerializeField] private float textShowDelay = 0f;
    [SerializeField] private bool hideTargetTextAtStart = true;

    [SerializeField] private TMP_Text extraText;
    [SerializeField] private float extraTextShowDelay = 0f;
    [SerializeField] private string extraTextContent = "";
    [SerializeField] private bool hideExtraTextAtStart = true;

    [SerializeField] private float objectsHideDelay = 0f;
    [SerializeField] private bool hideTextsOnDialogueEnd = true;
    [SerializeField] private bool debugApplyNow = false;

    private bool[] origActiveStates;
    private string origText;
    private bool origTextActive;

    private string extraOrigText;
    private bool extraOrigActive;

    private Coroutine hideRoutine;
    private Coroutine textRoutine;
    private Coroutine extraTextRoutine;
    private bool prepared;

    private void Awake()
    {
        Prepare();
        if (hideTargetTextAtStart && targetText != null) targetText.gameObject.SetActive(false);
        if (hideExtraTextAtStart && extraText != null) extraText.gameObject.SetActive(false);
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
        StopAll();
        RevertAll();
    }

    private void Prepare()
    {
        if (prepared) return;

        int oc = objectsToHide != null ? objectsToHide.Length : 0;
        origActiveStates = new bool[oc];
        for (int i = 0; i < oc; i++)
        {
            var go = objectsToHide[i];
            origActiveStates[i] = go != null && go.activeSelf;
        }

        if (targetText != null)
        {
            origText = targetText.text;
            origTextActive = targetText.gameObject.activeSelf;
        }
        else
        {
            origText = null;
            origTextActive = false;
        }

        if (extraText != null)
        {
            extraOrigText = extraText.text;
            extraOrigActive = extraText.gameObject.activeSelf;
        }
        else
        {
            extraOrigText = null;
            extraOrigActive = false;
        }

        prepared = true;
    }

    private void OnDialogueStart(int index)
    {
        int normalizedIndex = oneBasedIndex ? (index + 1) : index;
        if (normalizedIndex != targetDialogueIndex) return;
        StartSequence();
    }

    private void OnDialogueEnd(int index)
    {
        int normalizedIndex = oneBasedIndex ? (index + 1) : index;
        if (normalizedIndex != targetDialogueIndex) return;
        StopAll();
        RevertAll();
    }

    private void StartSequence()
    {
        StopAll();

        if (objectsToHide != null && objectsToHide.Length > 0)
            hideRoutine = StartCoroutine(HideAfterDelay(objectsHideDelay));

        if (targetText != null)
            textRoutine = StartCoroutine(ShowTextAfterDelay(targetText, "null", textShowDelay));

        if (extraText != null)
            extraTextRoutine = StartCoroutine(ShowTextAfterDelay(extraText, extraTextContent, extraTextShowDelay));
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        for (int i = 0; i < objectsToHide.Length; i++)
            if (objectsToHide[i] != null) objectsToHide[i].SetActive(false);
        hideRoutine = null;
    }

    private IEnumerator ShowTextAfterDelay(TMP_Text textObj, string content, float delay)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        textObj.gameObject.SetActive(true);
        textObj.enabled = true;
        if (!string.IsNullOrEmpty(content)) textObj.text = content;
        var c = textObj.color; c.a = 1f; textObj.color = c;
        var cv = textObj.GetComponentInParent<Canvas>();
        if (cv != null && cv.renderMode == RenderMode.WorldSpace && cv.worldCamera == null)
            cv.worldCamera = Camera.main;
        textObj.ForceMeshUpdate(true);
        if (textObj == targetText) textRoutine = null; else if (textObj == extraText) extraTextRoutine = null;
    }

    private void StopAll()
    {
        if (hideRoutine != null) StopCoroutine(hideRoutine);
        if (textRoutine != null) StopCoroutine(textRoutine);
        if (extraTextRoutine != null) StopCoroutine(extraTextRoutine);
        hideRoutine = textRoutine = extraTextRoutine = null;
    }

    private void RevertAll()
    {
        if (objectsToHide != null)
        {
            for (int i = 0; i < objectsToHide.Length; i++)
            {
                var go = objectsToHide[i];
                if (go == null) continue;
                bool orig = (origActiveStates != null && i < origActiveStates.Length) ? origActiveStates[i] : false;
                go.SetActive(orig);
            }
        }

        if (targetText != null)
        {
            targetText.text = origText ?? "";
            targetText.gameObject.SetActive(hideTextsOnDialogueEnd ? false : origTextActive);
            targetText.ForceMeshUpdate(true);
        }

        if (extraText != null)
        {
            extraText.text = extraOrigText ?? "";
            extraText.gameObject.SetActive(hideTextsOnDialogueEnd ? false : extraOrigActive);
            extraText.ForceMeshUpdate(true);
        }
    }
}
