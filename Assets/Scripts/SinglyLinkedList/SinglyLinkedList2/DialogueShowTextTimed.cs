using UnityEngine;
using TMPro;
using System.Collections;

public class DialogueShowTextTimed : MonoBehaviour
{
    [Header("Dialogue Settings")]
    public DialogueVoiceControllerBasic dialogue;
    public int triggerDialogueIndex = 1;  // شماره دیالوگی که در آن نمایش شروع می‌شود
    public int endDialogueIndex = -1;     // اگر -1 باشد، بعد از trigger+1 پنهان می‌شود

    [Header("Text Target (3D)")]
    public TextMeshPro tmpText3D;
    public TextMesh legacyTextMesh;
    public GameObject textContainer;

    [Header("Timing (seconds from dialogue start)")]
    public float showTime = 1f;
    public float hideTime = 3f;

    [Header("Visibility")]
    public bool startHidden = true;

    Coroutine _routine;

    void Awake()
    {
        if (startHidden)
            HideImmediate();
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

        if (_routine != null)
            StopCoroutine(_routine);
    }

    void OnDialogueStart(int index)
    {
        if (index == triggerDialogueIndex)
        {
            if (_routine != null)
                StopCoroutine(_routine);

            _routine = StartCoroutine(TimedShowHide());
        }

        int endTarget = endDialogueIndex >= 0 ? endDialogueIndex : (triggerDialogueIndex + 1);
        if (index == endTarget)
        {
            if (_routine != null)
                StopCoroutine(_routine);

            HideImmediate();
        }
    }

    IEnumerator TimedShowHide()
    {
        if (showTime > 0)
            yield return new WaitForSeconds(showTime);

        ShowImmediate();

        if (hideTime > showTime)
        {
            yield return new WaitForSeconds(hideTime - showTime);
            HideImmediate();
        }
    }

    void ShowImmediate()
    {
        if (textContainer != null)
        {
            textContainer.SetActive(true);
            return;
        }

        if (tmpText3D != null)
            tmpText3D.gameObject.SetActive(true);

        if (legacyTextMesh != null)
            legacyTextMesh.gameObject.SetActive(true);

        if (tmpText3D == null && legacyTextMesh == null)
            gameObject.SetActive(true);
    }

    void HideImmediate()
    {
        if (textContainer != null)
        {
            textContainer.SetActive(false);
            return;
        }

        if (tmpText3D != null)
            tmpText3D.gameObject.SetActive(false);

        if (legacyTextMesh != null)
            legacyTextMesh.gameObject.SetActive(false);

        if (tmpText3D == null && legacyTextMesh == null)
            gameObject.SetActive(false);
    }

    [ContextMenu("Show Now")]
    public void ShowNow() => ShowImmediate();

    [ContextMenu("Hide Now")]
    public void HideNow() => HideImmediate();
}
