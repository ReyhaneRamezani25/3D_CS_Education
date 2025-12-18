using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowCubesArrowsWithLights : MonoBehaviour
{
    [SerializeField] private DialogueVoiceControllerBasic controller;

    [Header("Cubes")]
    [SerializeField] private Renderer[] cubeRenderers;
    [SerializeField] private Light[] cubeLights;

    [Header("Arrows")]
    [SerializeField] private Renderer[] arrowRenderers;

    [Header("Timing")]
    [SerializeField] private int triggerDialogueIndex = 2;
    [SerializeField] private float delayFromDialogueStart = 0.5f;
    [SerializeField] private float showDuration = 2f;

    [Header("Appearance")]
    [SerializeField] private Color targetColor = Color.cyan;
    [SerializeField] private float cubeLightIntensity = 6f;

    private Renderer[] allRenderers;
    private bool[] originalActiveStates;
    private MaterialPropertyBlock[] mpbs;
    private int[] colorPropIds;
    private Color[] originalColors;

    private List<Light[]> arrowLightsPerRenderer;
    private List<Color[]> arrowLightsOriginalColorsPerRenderer;
    private Color[] cubeLightOriginalColors;
    private float[] cubeLightOriginalIntensities;
    private bool[] cubeLightOriginalEnabled;

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
        if (routine != null) { StopCoroutine(routine); routine = null; }
    }

    private void HandleStart(int index)
    {
        if (index != triggerDialogueIndex) return;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(RunWindow());
    }

    private IEnumerator RunWindow()
    {
        BuildCachesIfNeeded();

        if (delayFromDialogueStart > 0f)
            yield return new WaitForSecondsRealtime(delayFromDialogueStart);

        for (int i = 0; i < allRenderers.Length; i++)
        {
            var r = allRenderers[i];
            if (r == null) continue;
            r.gameObject.SetActive(true);
            int prop = colorPropIds[i];
            if (prop != 0)
            {
                var block = mpbs[i];
                block.SetColor(prop, targetColor);
                var mats = r.sharedMaterials;
                if (mats != null && mats.Length > 1)
                    for (int m = 0; m < mats.Length; m++) r.SetPropertyBlock(block, m);
                else
                    r.SetPropertyBlock(block);
            }
        }

        for (int i = 0; i < cubeLights.Length; i++)
        {
            var l = cubeLights[i];
            if (l == null) continue;
            l.color = targetColor;
            l.intensity = cubeLightIntensity;
            l.enabled = true;
        }

        if (arrowRenderers != null && arrowRenderers.Length > 0)
        {
            for (int i = 0; i < arrowRenderers.Length; i++)
            {
                var lights = arrowLightsPerRenderer[i];
                if (lights == null) continue;
                for (int k = 0; k < lights.Length; k++)
                {
                    var l = lights[k];
                    if (l == null) continue;
                    l.color = targetColor;
                }
            }
        }

        if (showDuration > 0f)
            yield return new WaitForSecondsRealtime(showDuration);

        for (int i = 0; i < allRenderers.Length; i++)
        {
            var r = allRenderers[i];
            if (r == null) continue;
            int prop = colorPropIds[i];
            if (prop != 0)
            {
                var block = mpbs[i];
                block.SetColor(prop, originalColors[i]);
                var mats = r.sharedMaterials;
                if (mats != null && mats.Length > 1)
                    for (int m = 0; m < mats.Length; m++) r.SetPropertyBlock(block, m);
                else
                    r.SetPropertyBlock(block);
            }
            r.gameObject.SetActive(originalActiveStates[i]);
        }

        for (int i = 0; i < cubeLights.Length; i++)
        {
            var l = cubeLights[i];
            if (l == null) continue;
            l.color = cubeLightOriginalColors[i];
            l.intensity = cubeLightOriginalIntensities[i];
            l.enabled = cubeLightOriginalEnabled[i];
        }

        if (arrowRenderers != null && arrowRenderers.Length > 0)
        {
            for (int i = 0; i < arrowRenderers.Length; i++)
            {
                var lights = arrowLightsPerRenderer[i];
                var origs = arrowLightsOriginalColorsPerRenderer[i];
                if (lights == null) continue;
                for (int k = 0; k < lights.Length; k++)
                {
                    var l = lights[k];
                    if (l == null) continue;
                    l.color = origs[k];
                }
            }
        }

        routine = null;
    }

    private void BuildCachesIfNeeded()
    {
        if (captured) return;

        int rc = (cubeRenderers?.Length ?? 0) + (arrowRenderers?.Length ?? 0);
        allRenderers = new Renderer[rc];

        int idx = 0;
        if (cubeRenderers != null) for (int i = 0; i < cubeRenderers.Length; i++) allRenderers[idx++] = cubeRenderers[i];
        if (arrowRenderers != null) for (int i = 0; i < arrowRenderers.Length; i++) allRenderers[idx++] = arrowRenderers[i];

        originalActiveStates = new bool[allRenderers.Length];
        mpbs = new MaterialPropertyBlock[allRenderers.Length];
        colorPropIds = new int[allRenderers.Length];
        originalColors = new Color[allRenderers.Length];

        for (int i = 0; i < allRenderers.Length; i++)
        {
            var r = allRenderers[i];
            if (r == null) continue;
            originalActiveStates[i] = r.gameObject.activeSelf;
            var mat = r.sharedMaterial != null ? r.sharedMaterial : r.material;
            int prop = 0;
            if (mat != null)
            {
                if (mat.HasProperty(BaseColorId)) prop = BaseColorId;
                else if (mat.HasProperty(ColorId)) prop = ColorId;
            }
            colorPropIds[i] = prop;
            originalColors[i] = prop != 0 ? mat.GetColor(prop) : Color.white;
            var block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            mpbs[i] = block;
        }

        arrowLightsPerRenderer = new List<Light[]>();
        arrowLightsOriginalColorsPerRenderer = new List<Color[]>();
        if (arrowRenderers != null && arrowRenderers.Length > 0)
        {
            for (int i = 0; i < arrowRenderers.Length; i++)
            {
                var r = arrowRenderers[i];
                if (r == null)
                {
                    arrowLightsPerRenderer.Add(null);
                    arrowLightsOriginalColorsPerRenderer.Add(null);
                    continue;
                }
                var lights = r.GetComponentsInChildren<Light>(true);
                arrowLightsPerRenderer.Add(lights);
                if (lights != null && lights.Length > 0)
                {
                    var origs = new Color[lights.Length];
                    for (int k = 0; k < lights.Length; k++)
                        origs[k] = lights[k] != null ? lights[k].color : Color.white;
                    arrowLightsOriginalColorsPerRenderer.Add(origs);
                }
                else
                {
                    arrowLightsOriginalColorsPerRenderer.Add(null);
                }
            }
        }

        if (cubeLights != null && cubeLights.Length > 0)
        {
            cubeLightOriginalColors = new Color[cubeLights.Length];
            cubeLightOriginalIntensities = new float[cubeLights.Length];
            cubeLightOriginalEnabled = new bool[cubeLights.Length];
            for (int i = 0; i < cubeLights.Length; i++)
            {
                var l = cubeLights[i];
                if (l == null) continue;
                cubeLightOriginalColors[i] = l.color;
                cubeLightOriginalIntensities[i] = l.intensity;
                cubeLightOriginalEnabled[i] = l.enabled;
            }
        }

        captured = true;
    }
}
