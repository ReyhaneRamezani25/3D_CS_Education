using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class HeadColorAndLightOnDialogue : MonoBehaviour
{
    public DialogueVoiceControllerBasic dialogue;
    public int applyDialogueIndex = 1;
    public float applyDelay = 0f;
    public int revertDialogueIndex = 2;
    public float revertDelay = 0f;
    public bool useUnscaledTime = true;

    public Renderer headRenderer;
    public Light headPointLight;

    public Color baseColor = Color.red;
    [ColorUsage(true, true)] public Color emissionColor = Color.red;

    public Color lightColor = Color.red;
    public float lightIntensity = 2f;
    public float lightRange = 10f;

    public TMP_Text tmpText;
    public Text uiText;

    Material[] originalMaterials;
    Color[] originalBaseColors;
    Color[] originalEmissionColors;

    struct LightState { public Color c; public float i; public float r; public bool e; }
    LightState originalLight;
    bool hasOriginalLight;

    Color originalTMPColor;
    bool hasTMPColor;
    Color originalUITextColor;
    bool hasUITextColor;

    void Awake()
    {
        if (headRenderer)
        {
            originalMaterials = headRenderer.materials;
            originalBaseColors = new Color[originalMaterials.Length];
            originalEmissionColors = new Color[originalMaterials.Length];
            for (int i = 0; i < originalMaterials.Length; i++)
            {
                var m = originalMaterials[i];
                if (m.HasProperty("_BaseColor")) originalBaseColors[i] = m.GetColor("_BaseColor");
                else if (m.HasProperty("_Color")) originalBaseColors[i] = m.GetColor("_Color");
                if (m.HasProperty("_EmissionColor")) originalEmissionColors[i] = m.GetColor("_EmissionColor");
                else if (m.HasProperty("_EmissiveColor")) originalEmissionColors[i] = m.GetColor("_EmissiveColor");
            }
        }
        if (headPointLight)
        {
            originalLight = new LightState { c = headPointLight.color, i = headPointLight.intensity, r = headPointLight.range, e = headPointLight.enabled };
            hasOriginalLight = true;
        }
        if (tmpText) { originalTMPColor = tmpText.color; hasTMPColor = true; }
        if (uiText) { originalUITextColor = uiText.color; hasUITextColor = true; }
    }

    void OnEnable()
    {
        if (dialogue != null) dialogue.OnDialogueStart += OnDialogueStart;
        #if UNITY_EDITOR
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        #endif
    }

    void OnDisable()
    {
        if (dialogue != null) dialogue.OnDialogueStart -= OnDialogueStart;
        #if UNITY_EDITOR
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        #endif
    }

    #if UNITY_EDITOR
    void OnPlayModeStateChanged(PlayModeStateChange s)
    {
        if (s == PlayModeStateChange.ExitingPlayMode) RevertNow();
    }
    #endif

    void OnDialogueStart(int index)
    {
        if (index == applyDialogueIndex) StartCoroutine(ApplyRoutine(applyDelay));
        if (index == revertDialogueIndex) StartCoroutine(RevertRoutine(revertDelay));
    }

    IEnumerator ApplyRoutine(float delay)
    {
        if (delay > 0f)
        {
            float t = 0f;
            while (t < delay) { t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime; yield return null; }
        }
        ApplyNow();
    }

    IEnumerator RevertRoutine(float delay)
    {
        if (delay > 0f)
        {
            float t = 0f;
            while (t < delay) { t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime; yield return null; }
        }
        RevertNow();
    }

    [ContextMenu("Apply Now")]
    public void ApplyNow()
    {
        if (headRenderer)
        {
            foreach (var mat in headRenderer.materials)
            {
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
                else if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emissionColor);
                else if (mat.HasProperty("_EmissiveColor")) mat.SetColor("_EmissiveColor", emissionColor);
            }
        }
        if (headPointLight)
        {
            if (!hasOriginalLight)
            {
                originalLight = new LightState { c = headPointLight.color, i = headPointLight.intensity, r = headPointLight.range, e = headPointLight.enabled };
                hasOriginalLight = true;
            }
            headPointLight.color = lightColor;
            headPointLight.intensity = lightIntensity;
            headPointLight.range = lightRange;
            headPointLight.enabled = true;
        }
        if (tmpText) tmpText.color = Color.white;
        if (uiText) uiText.color = Color.white;
    }

    [ContextMenu("Revert Now")]
    public void RevertNow()
    {
        if (headRenderer && originalMaterials != null)
        {
            var mats = headRenderer.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (i < originalBaseColors.Length)
                {
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", originalBaseColors[i]);
                    else if (m.HasProperty("_Color")) m.SetColor("_Color", originalBaseColors[i]);
                }
                if (i < originalEmissionColors.Length)
                {
                    if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", originalEmissionColors[i]);
                    else if (m.HasProperty("_EmissiveColor")) m.SetColor("_EmissiveColor", originalEmissionColors[i]);
                }
            }
        }
        if (hasOriginalLight && headPointLight)
        {
            headPointLight.color = originalLight.c;
            headPointLight.intensity = originalLight.i;
            headPointLight.range = originalLight.r;
            headPointLight.enabled = originalLight.e;
        }
        if (hasTMPColor && tmpText) tmpText.color = originalTMPColor;
        if (hasUITextColor && uiText) uiText.color = originalUITextColor;
    }
}
