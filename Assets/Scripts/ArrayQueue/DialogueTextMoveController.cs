using System.Collections;
using UnityEngine;

public class DialogueTextMoveController : MonoBehaviour
{
    public DialogueVoiceControllerBasic dialogue;
    public RectTransform targetText;
    public int moveDialogueIndex = 0;
    public float moveDelay = 0f;
    public float moveAmountY = 50f;

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
        if (index == moveDialogueIndex)
            StartCoroutine(MoveUp());
    }

    IEnumerator MoveUp()
    {
        if (moveDelay > 0f)
            yield return new WaitForSeconds(moveDelay);

        if (targetText)
        {
            Vector3 pos = targetText.anchoredPosition;
            pos.y += moveAmountY;
            targetText.anchoredPosition = pos;
        }
    }
}
