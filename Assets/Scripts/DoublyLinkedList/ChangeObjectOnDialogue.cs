using System.Collections;
using UnityEngine;

public class ObjectAndLightColorOnDialogue : MonoBehaviour
{
    public DialogueVoiceControllerBasic dialogue;

    public Renderer targetRenderer;
    public Light targetLight;

    public int triggerDialogueIndex = 2;
    public float triggerDelay = 0.5f;
    public Material triggerMaterial;
    public Color triggerLightColor = Color.red;

    public int revertDialogueIndex = 4;
    public float revertDelay = 0.5f;
    public Color revertLightColor = Color.white;

    Material[] originalMaterials;
    bool hadRenderer;

    Color originalLightColor;
    bool hadLight;

    void OnEnable()
    {
        if (dialogue) dialogue.OnDialogueStart += OnDialogue;
    }

    void OnDisable()
    {
        if (dialogue) dialogue.OnDialogueStart -= OnDialogue;
    }

    void OnDialogue(int index)
    {
        if (index == triggerDialogueIndex)
        {
            SaveSnapshot();
            StartCoroutine(TriggerChange());
        }

        if (index == revertDialogueIndex)
        {
            StartCoroutine(RevertChange());
        }
    }

    void SaveSnapshot()
    {
        if (targetRenderer)
        {
            hadRenderer = true;
            originalMaterials = targetRenderer.sharedMaterials;
        }

        if (targetLight)
        {
            hadLight = true;
            originalLightColor = targetLight.color;
        }
    }

    IEnumerator TriggerChange()
    {
        yield return new WaitForSeconds(triggerDelay);

        if (hadRenderer && triggerMaterial)
        {
            var mats = targetRenderer.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
                mats[i] = triggerMaterial;
            targetRenderer.sharedMaterials = mats;
        }

        if (hadLight)
        {
            targetLight.color = triggerLightColor;
        }
    }

    IEnumerator RevertChange()
    {
        yield return new WaitForSeconds(revertDelay);

        if (hadRenderer)
        {
            targetRenderer.sharedMaterials = originalMaterials;
        }

        if (hadLight)
        {
            targetLight.color = revertLightColor;
        }
    }
}
