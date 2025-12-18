using System.Collections;
using UnityEngine;
using TMPro;

public class CubeColorAndLightOn : MonoBehaviour
{
    [SerializeField] private DialogueVoiceControllerBasic controller;

    [Header("Cubes")]
    [SerializeField] private Renderer[] cubes;
    [SerializeField] private Light[] pointLights;
    [SerializeField] private Material cubeSwapMaterial;
    [SerializeField] private Color cubeTargetColor = Color.red;
    [SerializeField] private float cubeLightIntensity = 5f;

    [Header("Cube Texts")]
    [SerializeField] private TMP_Text[] cubeTexts;
    [SerializeField] private Color cubeTextTargetColor = Color.white;

    [Header("Arrows")]
    [SerializeField] private Renderer[] arrows;
    [SerializeField] private Light[] arrowLights;
    [SerializeField] private Material arrowSwapMaterial;
    [SerializeField] private Color arrowTargetColor = Color.cyan;
    [SerializeField] private float arrowLightIntensity = 5f;

    [Header("Trigger")]
    [SerializeField] private int triggerDialogueIndex = 4;
    [SerializeField] private bool oneBasedIndex = false;

    [Header("Cube Timing")]
    [SerializeField] private float cubeDelayFromStart = 0.5f;
    [SerializeField] private float cubeHoldDuration = 2f;

    [Header("Arrow Timing")]
    [SerializeField] private float arrowDelayFromStart = 0.5f;
    [SerializeField] private float arrowHoldDuration = 2f;

    [Header("LED Sphere")]
    [SerializeField] private GameObject ledSphere;
    [SerializeField] private float ledDelayFromStart = 0.5f;
    [SerializeField] private float ledHoldDuration = 2f;

    [SerializeField] private bool debugApplyNow = false;

    private static readonly int PID_BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int PID_Color = Shader.PropertyToID("_Color");

    private Material[][] cubeOriginalMats;
    private Material[] cubeSolidMatPerRenderer;
    private Color[] lightOrigColor;
    private float[] lightOrigIntensity;
    private bool[] lightOrigEnabled;

    private Color[] cubeTextOrigColors;

    private Material[][] arrowOriginalMats;
    private Material[] arrowSolidMatPerRenderer;
    private Color[] arrowLightOrigColor;
    private float[] arrowLightOrigIntensity;
    private bool[] arrowLightOrigEnabled;

    private bool ledSphereOrigActive;

    private Coroutine cubeRoutine;
    private Coroutine arrowRoutine;
    private Coroutine ledRoutine;

    private void Awake()
    {
        if (debugApplyNow)
        {
            Prepare();
            cubeRoutine = StartCoroutine(ApplyThenRevertCubes());
            arrowRoutine = StartCoroutine(ApplyThenRevertArrows());
            ledRoutine = StartCoroutine(ApplyThenRevertLed());
        }
    }

    private void OnEnable()
    {
        if (controller == null) controller = GetComponent<DialogueVoiceControllerBasic>();
        if (controller != null) controller.OnDialogueStart += OnDialogueStart;
        Prepare();
    }

    private void OnDisable()
    {
        if (controller != null) controller.OnDialogueStart -= OnDialogueStart;
        if (cubeRoutine != null) StopCoroutine(cubeRoutine);
        if (arrowRoutine != null) StopCoroutine(arrowRoutine);
        if (ledRoutine != null) StopCoroutine(ledRoutine);
        RevertCubes();
        RevertArrows();
        RevertLed(true);
    }

    private void OnDialogueStart(int index)
    {
        int expected = oneBasedIndex ? triggerDialogueIndex - 1 : triggerDialogueIndex;
        if (index != expected) return;

        if (cubeRoutine != null) StopCoroutine(cubeRoutine);
        if (arrowRoutine != null) StopCoroutine(arrowRoutine);
        if (ledRoutine != null) StopCoroutine(ledRoutine);

        cubeRoutine = StartCoroutine(ApplyThenRevertCubes());
        arrowRoutine = StartCoroutine(ApplyThenRevertArrows());
        ledRoutine = StartCoroutine(ApplyThenRevertLed());
    }

    private void Prepare()
    {
        if (cubes != null && cubes.Length > 0)
        {
            cubeOriginalMats = new Material[cubes.Length][];
            cubeSolidMatPerRenderer = new Material[cubes.Length];
            for (int i = 0; i < cubes.Length; i++)
            {
                var r = cubes[i];
                if (r == null) continue;
                cubeOriginalMats[i] = r.materials;

                Material m = cubeSwapMaterial != null ? new Material(cubeSwapMaterial) : CreateSolidMaterial();
                if (m.HasProperty(PID_BaseColor)) m.SetColor(PID_BaseColor, cubeTargetColor);
                else if (m.HasProperty(PID_Color)) m.SetColor(PID_Color, cubeTargetColor);
                cubeSolidMatPerRenderer[i] = m;
            }
        }

        if (pointLights != null && pointLights.Length > 0)
        {
            lightOrigColor = new Color[pointLights.Length];
            lightOrigIntensity = new float[pointLights.Length];
            lightOrigEnabled = new bool[pointLights.Length];
            for (int i = 0; i < pointLights.Length; i++)
            {
                var l = pointLights[i];
                if (l == null) continue;
                lightOrigColor[i] = l.color;
                lightOrigIntensity[i] = l.intensity;
                lightOrigEnabled[i] = l.enabled;
            }
        }

        if (cubeTexts != null && cubeTexts.Length > 0)
        {
            cubeTextOrigColors = new Color[cubeTexts.Length];
            for (int i = 0; i < cubeTexts.Length; i++)
            {
                var t = cubeTexts[i];
                if (t == null) continue;
                cubeTextOrigColors[i] = t.color;
            }
        }

        if (arrows != null && arrows.Length > 0)
        {
            arrowOriginalMats = new Material[arrows.Length][];
            arrowSolidMatPerRenderer = new Material[arrows.Length];
            for (int i = 0; i < arrows.Length; i++)
            {
                var r = arrows[i];
                if (r == null) continue;
                arrowOriginalMats[i] = r.materials;

                Material m = arrowSwapMaterial != null ? new Material(arrowSwapMaterial) : CreateSolidMaterial();
                if (m.HasProperty(PID_BaseColor)) m.SetColor(PID_BaseColor, arrowTargetColor);
                else if (m.HasProperty(PID_Color)) m.SetColor(PID_Color, arrowTargetColor);
                arrowSolidMatPerRenderer[i] = m;
            }
        }

        if (arrowLights != null && arrowLights.Length > 0)
        {
            arrowLightOrigColor = new Color[arrowLights.Length];
            arrowLightOrigIntensity = new float[arrowLights.Length];
            arrowLightOrigEnabled = new bool[arrowLights.Length];
            for (int i = 0; i < arrowLights.Length; i++)
            {
                var l = arrowLights[i];
                if (l == null) continue;
                arrowLightOrigColor[i] = l.color;
                arrowLightOrigIntensity[i] = l.intensity;
                arrowLightOrigEnabled[i] = l.enabled;
            }
        }

        if (ledSphere != null)
        {
            ledSphereOrigActive = ledSphere.activeSelf;
            ledSphere.SetActive(false);
        }
    }

    private IEnumerator ApplyThenRevertCubes()
    {
        if (cubeDelayFromStart > 0f) yield return new WaitForSecondsRealtime(cubeDelayFromStart);
        ApplyCubes();
        if (cubeHoldDuration > 0f) yield return new WaitForSecondsRealtime(cubeHoldDuration);
        RevertCubes();
        cubeRoutine = null;
    }

    private IEnumerator ApplyThenRevertArrows()
    {
        if (arrowDelayFromStart > 0f) yield return new WaitForSecondsRealtime(arrowDelayFromStart);
        ApplyArrows();
        if (arrowHoldDuration > 0f) yield return new WaitForSecondsRealtime(arrowHoldDuration);
        RevertArrows();
        arrowRoutine = null;
    }

    private IEnumerator ApplyThenRevertLed()
    {
        if (ledSphere == null) yield break;
        if (ledDelayFromStart > 0f) yield return new WaitForSecondsRealtime(ledDelayFromStart);
        ApplyLed();
        if (ledHoldDuration > 0f) yield return new WaitForSecondsRealtime(ledHoldDuration);
        RevertLed(false);
        ledRoutine = null;
    }

    private void ApplyCubes()
    {
        if (cubes != null)
        {
            for (int i = 0; i < cubes.Length; i++)
            {
                var r = cubes[i];
                if (r == null) continue;
                var mats = r.materials;
                for (int m = 0; m < mats.Length; m++) mats[m] = cubeSolidMatPerRenderer[i];
                r.materials = mats;
            }
        }

        if (pointLights != null)
        {
            for (int i = 0; i < pointLights.Length; i++)
            {
                var l = pointLights[i];
                if (l == null) continue;
                l.color = cubeTargetColor;
                l.intensity = cubeLightIntensity;
                l.enabled = true;
            }
        }

        if (cubeTexts != null)
        {
            for (int i = 0; i < cubeTexts.Length; i++)
            {
                var t = cubeTexts[i];
                if (t == null) continue;
                t.color = cubeTextTargetColor;
            }
        }
    }

    private void RevertCubes()
    {
        if (cubes != null && cubeOriginalMats != null)
        {
            for (int i = 0; i < cubes.Length; i++)
            {
                var r = cubes[i];
                if (r == null) continue;
                if (cubeOriginalMats[i] != null) r.materials = cubeOriginalMats[i];
            }
        }

        if (pointLights != null && lightOrigColor != null)
        {
            for (int i = 0; i < pointLights.Length; i++)
            {
                var l = pointLights[i];
                if (l == null) continue;
                l.color = lightOrigColor[i];
                l.intensity = lightOrigIntensity[i];
                l.enabled = lightOrigEnabled[i];
            }
        }

        if (cubeTexts != null && cubeTextOrigColors != null)
        {
            for (int i = 0; i < cubeTexts.Length; i++)
            {
                var t = cubeTexts[i];
                if (t == null) continue;
                t.color = cubeTextOrigColors[i];
            }
        }
    }

    private void ApplyArrows()
    {
        if (arrows != null)
        {
            for (int i = 0; i < arrows.Length; i++)
            {
                var r = arrows[i];
                if (r == null) continue;
                var mats = r.materials;
                for (int m = 0; m < mats.Length; m++) mats[m] = arrowSolidMatPerRenderer[i];
                r.materials = mats;
            }
        }

        if (arrowLights != null)
        {
            for (int i = 0; i < arrowLights.Length; i++)
            {
                var l = arrowLights[i];
                if (l == null) continue;
                l.color = arrowTargetColor;
                l.intensity = arrowLightIntensity;
                l.enabled = true;
            }
        }
    }

    private void RevertArrows()
    {
        if (arrows != null && arrowOriginalMats != null)
        {
            for (int i = 0; i < arrows.Length; i++)
            {
                var r = arrows[i];
                if (r == null) continue;
                if (arrowOriginalMats[i] != null) r.materials = arrowOriginalMats[i];
            }
        }

        if (arrowLights != null && arrowLightOrigColor != null)
        {
            for (int i = 0; i < arrowLights.Length; i++)
            {
                var l = arrowLights[i];
                if (l == null) continue;
                l.color = arrowLightOrigColor[i];
                l.intensity = arrowLightOrigIntensity[i];
                l.enabled = arrowLightOrigEnabled[i];
            }
        }
    }

    private void ApplyLed()
    {
        ledSphere.SetActive(true);
    }

    private void RevertLed(bool forceImmediate)
    {
        if (ledSphere == null) return;
        if (forceImmediate)
        {
            ledSphere.SetActive(ledSphereOrigActive);
            return;
        }
        ledSphere.SetActive(false);
    }

    private Material CreateSolidMaterial()
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Unlit/Color");
        if (sh == null) sh = Shader.Find("Standard");
        return new Material(sh);
    }
}
