using UnityEngine;
using TMPro;

public class DialogueHideTwoTextsOnStart : MonoBehaviour
{
    public DialogueVoiceControllerBasic dialogue;
    public int hideDialogueIndex = 1;

    public TextMeshPro tmpTextA;
    public TextMesh legacyTextA;
    public GameObject containerA;

    public TextMeshPro tmpTextB;
    public TextMesh legacyTextB;
    public GameObject containerB;

    void OnEnable()
    {
        if (dialogue != null) dialogue.OnDialogueStart += OnDialogueStart;
    }

    void OnDisable()
    {
        if (dialogue != null) dialogue.OnDialogueStart -= OnDialogueStart;
    }

    void OnDialogueStart(int index)
    {
        if (index == hideDialogueIndex)
        {
            HideA();
            HideB();
        }
    }

    void HideA()
    {
        if (containerA != null) { containerA.SetActive(false); return; }
        if (tmpTextA != null) tmpTextA.gameObject.SetActive(false);
        if (legacyTextA != null) legacyTextA.gameObject.SetActive(false);
    }

    void HideB()
    {
        if (containerB != null) { containerB.SetActive(false); return; }
        if (tmpTextB != null) tmpTextB.gameObject.SetActive(false);
        if (legacyTextB != null) legacyTextB.gameObject.SetActive(false);
    }

    [ContextMenu("Hide Now")]
    public void HideNow()
    {
        HideA();
        HideB();
    }
}
