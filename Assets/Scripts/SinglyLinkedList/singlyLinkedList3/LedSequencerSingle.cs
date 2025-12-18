using System.Collections;
using UnityEngine;

public class LedSequencerSingle : MonoBehaviour
{
    public DialogueVoiceControllerBasic dialogue;
    public int triggerDialogueIndex = 1;

    public Transform led;

    public bool hideLEDAtStart = true;
    public bool useUnscaledTime = true;

    public float initialDelay = 0f;
    public float hideDelayAfterFinish = 0.5f;

    public Renderer[] group1Renderers;
    public Color group1BaseColor = Color.white;
    public Color group1EmissionColor = Color.white;
    public float group1EmissionMultiplier = 1f;
    public Light[] group1Lights;
    public Color group1LightColor = Color.white;
    public float group1LightIntensity = 1f;
    public float group1LightRange = 10f;

    public Renderer[] group2Renderers;
    public Color group2BaseColor = Color.white;
    public Color group2EmissionColor = Color.white;
    public float group2EmissionMultiplier = 1f;
    public Light[] group2Lights;
    public Color group2LightColor = Color.white;
    public float group2LightIntensity = 1f;
    public float group2LightRange = 10f;

    Coroutine _runner;
    bool _shown;

    void Awake()
    {
        if (led && hideLEDAtStart) led.gameObject.SetActive(false);
        _shown = led && led.gameObject.activeSelf;
    }

    void OnEnable()
    {
        if (dialogue != null) dialogue.OnDialogueStart += OnDialogueStart;
    }

    void OnDisable()
    {
        if (dialogue != null) dialogue.OnDialogueStart -= OnDialogueStart;
        if (_runner != null) { StopCoroutine(_runner); _runner = null; }
    }

    void OnDialogueStart(int index)
    {
        if (index != triggerDialogueIndex) return;
        if (_runner != null) { StopCoroutine(_runner); _runner = null; }
        _runner = StartCoroutine(Sequence());
    }

    IEnumerator Sequence()
    {
        if (!led) yield break;

        if (initialDelay > 0f) yield return WaitSeconds(initialDelay);

        if (!_shown)
        {
            led.gameObject.SetActive(true);
            _shown = true;
        }

        ApplyGroupColors(group1Renderers, group1BaseColor, group1EmissionColor * Mathf.Max(0f, group1EmissionMultiplier));
        ApplyGroupLights(group1Lights, group1LightColor, group1LightIntensity, group1LightRange);

        ApplyGroupColors(group2Renderers, group2BaseColor, group2EmissionColor * Mathf.Max(0f, group2EmissionMultiplier));
        ApplyGroupLights(group2Lights, group2LightColor, group2LightIntensity, group2LightRange);

        if (hideDelayAfterFinish > 0f) yield return WaitSeconds(hideDelayAfterFinish);
        led.gameObject.SetActive(false);
        _shown = false;

        _runner = null;
    }

    void ApplyGroupColors(Renderer[] rends, Color baseColor, Color emissionColor)
    {
        if (rends == null) return;
        for (int i = 0; i < rends.Length; i++)
        {
            var r = rends[i];
            if (!r) continue;

            var mats = r.materials;
            for (int m = 0; m < mats.Length; m++)
            {
                var mat = mats[m];
                if (!mat) continue;

                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
                else if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);

                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", emissionColor);
                }
                else if (mat.HasProperty("_EmissiveColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissiveColor", emissionColor);
                }
            }
        }
    }

    void ApplyGroupLights(Light[] lights, Color c, float intensity, float range)
    {
        if (lights == null) return;
        for (int i = 0; i < lights.Length; i++)
        {
            var L = lights[i];
            if (!L) continue;
            L.color = c;
            L.intensity = intensity;
            L.range = range;
            L.enabled = true;
        }
    }

    object WaitSeconds(float d)
    {
        return useUnscaledTime ? (object)new WaitForSecondsRealtime(d) : new WaitForSeconds(d);
    }

    [ContextMenu("Show Now")]
    public void ShowNow()
    {
        OnDialogueStart(triggerDialogueIndex);
    }
}
