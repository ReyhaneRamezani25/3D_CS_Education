using System.Collections;
using UnityEngine;

public class DialogueShowObject : MonoBehaviour
{
    public DialogueVoiceControllerBasic dialogue;

    public GameObject targetObject;
    public bool startHidden = true;

    public int showDialogueIndex = 1;
    public float showDelay = 0.5f;

    public bool hideAfter = false;
    public float hideDelay = 1f;

    void Awake()
    {
        if (startHidden && targetObject) 
            targetObject.SetActive(false);
    }

    void OnEnable()
    {
        if (dialogue) dialogue.OnDialogueStart += OnDialogueStart;
    }

    void OnDisable()
    {
        if (dialogue) dialogue.OnDialogueStart -= OnDialogueStart;
    }

    void OnDialogueStart(int index)
    {
        if (index == showDialogueIndex && targetObject)
            StartCoroutine(ShowRoutine());
    }

    IEnumerator ShowRoutine()
    {
        if (showDelay > 0f)
            yield return new WaitForSeconds(showDelay);

        targetObject.SetActive(true);

        if (hideAfter && hideDelay > 0f)
        {
            yield return new WaitForSeconds(hideDelay);
            targetObject.SetActive(false);
        }
    }
}
