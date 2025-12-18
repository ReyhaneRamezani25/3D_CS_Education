using System.Collections;
using UnityEngine;

public class CubeColorAndLightOnDialogue : MonoBehaviour
{
    [SerializeField] private DialogueVoiceControllerBasic controller;
    [SerializeField] private Renderer[] cubes;
    [SerializeField] private Light[] pointLights;
    [SerializeField] private int triggerDialogueIndex = 2;
    [SerializeField] private float delayFromDialogueStart = 0.5f;
    [SerializeField] private float holdDuration = 2f;
    [SerializeField] private Color targetColor = Color.red;
    [SerializeField] private float lightIntensity = 5f;

    private Color[] originalColors;
    private float[] originalLightIntensities;
    private int[] colorPropIds;
    private MaterialPropertyBlock[] mpbs;
    private bool captured;
    private Coroutine routine;

    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private void OnEnable()
    {
        if (controller == null) controller = GetComponent<DialogueVoiceControllerBasic>();
        if (controller != null) controller.OnDialogueStart += HandleStart;
    }

    private void OnDisable()
    {
        if (controller != null) controller.OnDialogueStart -= HandleStart;
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }

    private void HandleStart(int index)
    {
        if (index != triggerDialogueIndex) return;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ApplyColorThenRevert());
    }

    private IEnumerator ApplyColorThenRevert()
    {
        if (cubes == null || cubes.Length == 0) yield break;

        if (!captured)
        {
            originalColors = new Color[cubes.Length];
            originalLightIntensities = new float[cubes.Length];
            colorPropIds = new int[cubes.Length];
            mpbs = new MaterialPropertyBlock[cubes.Length];

            for (int i = 0; i < cubes.Length; i++)
            {
                var r = cubes[i];
                if (r == null) continue;

                var mat = r.sharedMaterial != null ? r.sharedMaterial : r.material;

                int prop = 0;
                if (mat != null)
                {
                    if (mat.HasProperty(BaseColorId)) prop = BaseColorId;
                    else if (mat.HasProperty(ColorId)) prop = ColorId;
                }
                colorPropIds[i] = prop;
                originalColors[i] = prop != 0 ? mat.GetColor(prop) : Color.white;

                mpbs[i] = new MaterialPropertyBlock();
                r.GetPropertyBlock(mpbs[i]);

                if (pointLights != null && i < pointLights.Length && pointLights[i] != null)
                    originalLightIntensities[i] = pointLights[i].intensity;
            }

            captured = true;
        }

        if (delayFromDialogueStart > 0f)
            yield return new WaitForSecondsRealtime(delayFromDialogueStart);

        for (int i = 0; i < cubes.Length; i++)
        {
            var r = cubes[i];
            if (r == null) continue;
            int prop = colorPropIds[i];
            if (prop == 0) continue;

            var block = mpbs[i];
            block.SetColor(prop, targetColor);
            var mats = r.sharedMaterials;
            if (mats != null && mats.Length > 1)
                for (int m = 0; m < mats.Length; m++) r.SetPropertyBlock(block, m);
            else
                r.SetPropertyBlock(block);

            if (pointLights != null && i < pointLights.Length && pointLights[i] != null)
            {
                pointLights[i].color = targetColor;
                pointLights[i].intensity = lightIntensity;
                pointLights[i].enabled = true;
            }
        }

        if (holdDuration > 0f)
            yield return new WaitForSecondsRealtime(holdDuration);

        for (int i = 0; i < cubes.Length; i++)
        {
            var r = cubes[i];
            if (r == null) continue;
            int prop = colorPropIds[i];
            if (prop == 0) continue;

            var block = mpbs[i];
            block.SetColor(prop, originalColors[i]);
            var mats = r.sharedMaterials;
            if (mats != null && mats.Length > 1)
                for (int m = 0; m < mats.Length; m++) r.SetPropertyBlock(block, m);
            else
                r.SetPropertyBlock(block);

            if (pointLights != null && i < pointLights.Length && pointLights[i] != null)
            {
                pointLights[i].color = originalColors[i];
                pointLights[i].intensity = originalLightIntensities[i];
                pointLights[i].enabled = false;
            }
        }

        routine = null;
    }
}
