using System.Collections;
using UnityEngine;

public class MaterialAndLightSetterOnDialogue : MonoBehaviour
{
    public DialogueVoiceControllerBasic dialogue;
    public int dialogueIndexToApply = 1;
    public float delayFromDialogueStart = 0f;
    public bool useUnscaledTime = false;

    public Renderer targetRenderer;
    public Material targetMaterial;
    public bool applyToAllSubMaterials = true;

    public Light targetLight;
    public Color targetLightColor = Color.white;
    public float targetLightIntensity = 1f;
    public float targetLightRange = 10f;
    public bool enableLight = true;

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
        if (index == dialogueIndexToApply) StartCoroutine(ApplyAfterDelay());
    }

    IEnumerator ApplyAfterDelay()
    {
        if (delayFromDialogueStart > 0f)
        {
            if (useUnscaledTime) yield return new WaitForSecondsRealtime(delayFromDialogueStart);
            else yield return new WaitForSeconds(delayFromDialogueStart);
        }
        ApplyMaterial();
        ApplyLight();
    }

    void ApplyMaterial()
    {
        if (!targetRenderer || !targetMaterial) return;
        if (applyToAllSubMaterials)
        {
            Material[] mats = targetRenderer.materials;
            for (int i = 0; i < mats.Length; i++) mats[i] = targetMaterial;
            targetRenderer.materials = mats;
        }
        else
        {
            targetRenderer.material = targetMaterial;
        }
    }

    void ApplyLight()
    {
        if (!targetLight) return;
        targetLight.enabled = enableLight;
        targetLight.color = targetLightColor;
        targetLight.intensity = targetLightIntensity;
        targetLight.range = targetLightRange;
    }
}
