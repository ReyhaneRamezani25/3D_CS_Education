using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectHider : MonoBehaviour
{
    [Serializable]
    public class HideEntry
    {
        public DialogueVoiceControllerBasic dialogue;
        public int triggerDialogueIndex = 1;
        public GameObject target;
        public float delay = 0f;
    }

    public List<HideEntry> entries = new List<HideEntry>();
    public bool useUnscaledTime = true;

    void OnEnable()
    {
        foreach (var entry in entries)
        {
            if (entry.dialogue != null)
                entry.dialogue.OnDialogueStart += (index) => OnDialogueStart(entry, index);
        }
    }

    void OnDisable()
    {
        foreach (var entry in entries)
        {
            if (entry.dialogue != null)
                entry.dialogue.OnDialogueStart -= (index) => OnDialogueStart(entry, index);
        }
    }

    void OnDialogueStart(HideEntry entry, int index)
    {
        if (index != entry.triggerDialogueIndex || entry.target == null) return;
        StartCoroutine(HideRoutine(entry));
    }

    IEnumerator HideRoutine(HideEntry entry)
    {
        if (entry.delay > 0)
        {
            if (useUnscaledTime)
                yield return new WaitForSecondsRealtime(entry.delay);
            else
                yield return new WaitForSeconds(entry.delay);
        }
        entry.target.SetActive(false);
    }
}
