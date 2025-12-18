using System.Collections;
using UnityEngine;

public class DialogueTextShow : MonoBehaviour
{
    public DialogueVoiceControllerBasic dialogue;
    public int triggerDialogueIndex = 1;
    public GameObject textObject;
    public float showDelay = 0f;
    public float hideDelay = 2f;
    public bool useUnscaledTime = true;

    void Awake()
    {
        if (textObject != null)
            textObject.SetActive(false);
    }

    void OnEnable()
    {
        if (dialogue != null)
            dialogue.OnDialogueStart += OnDialogueStart;
    }

    void OnDisable()
    {
        if (dialogue != null)
            dialogue.OnDialogueStart -= OnDialogueStart;
    }

    void OnDialogueStart(int index)
    {
        if (index != triggerDialogueIndex || textObject == null) return;
        StartCoroutine(ShowAndHideRoutine());
    }

    IEnumerator ShowAndHideRoutine()
    {
        if (showDelay > 0)
        {
            if (useUnscaledTime)
                yield return new WaitForSecondsRealtime(showDelay);
            else
                yield return new WaitForSeconds(showDelay);
        }

        textObject.SetActive(true);

        if (hideDelay > 0)
        {
            if (useUnscaledTime)
                yield return new WaitForSecondsRealtime(hideDelay);
            else
                yield return new WaitForSeconds(hideDelay);
        }

        textObject.SetActive(false);
    }
}
