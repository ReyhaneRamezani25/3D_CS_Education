using System.Collections;
using UnityEngine;

public class ShowOnDialogueOffset : MonoBehaviour
{
    public DialogueVoiceControllerBasic dialogue;
    public GameObject target;
    public int triggerDialogueIndex = 2;
    public int revertDialogueIndex = 4;
    public float appearDelay = 0.5f;
    public bool hideOnAwake = true;
    public bool useUnscaledTime = true;
    public bool onlyOnce = true;

    public Renderer targetRenderer;
    public Material materialOnEnd;
    public Color baseColorOnEnd = new Color(0.984f, 0.051f, 0.051f);
    public Color emissionColorOnEnd = new Color(245f / 255f, 3f / 255f, 3f / 255f);

    public Light targetLight;
    public Color lightColorOnEnd = new Color(0.968f, 0.086f, 0.141f);
    public float lightIntensityOnEnd = 2f;
    public float lightRangeOnEnd = 100f;

    bool fired;
    bool snapshotHadTarget;
    bool snapshotWasActive;

    bool snapshotHadRenderer;
    Material snapshotSingleMaterial;
    Material[] snapshotMaterialsArray;

    bool snapshotHadLight;
    struct LightState { public Color c; public float i; public float r; public bool e; public bool a; }
    LightState snapshotLight;

    static readonly int PID_BaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int PID_Color = Shader.PropertyToID("_Color");
    static readonly int PID_EmissionColor = Shader.PropertyToID("_EmissionColor");
    MaterialPropertyBlock mpb;

    int currentDialogueIndex = -1;

    void Awake()
    {
        if (hideOnAwake && target) target.SetActive(false);
        mpb = new MaterialPropertyBlock();
    }

    void OnEnable()
    {
        if (dialogue)
        {
            dialogue.OnDialogueStart += OnDialogueStart;
            dialogue.OnDialogueEnd += OnDialogueEnd;
        }
    }

    void OnDisable()
    {
        if (dialogue)
        {
            dialogue.OnDialogueStart -= OnDialogueStart;
            dialogue.OnDialogueEnd -= OnDialogueEnd;
        }
    }

    void OnDialogueStart(int index)
    {
        if (index == triggerDialogueIndex)
        {
            if (onlyOnce && fired) return;
            currentDialogueIndex = index;
            TakeSnapshot();
            StartCoroutine(ShowAfterDelay());
            return;
        }

        if (index == revertDialogueIndex + 1)
        {
            if (target) target.SetActive(false);
        }
    }

    void OnDialogueEnd(int index)
    {
        if (index != currentDialogueIndex) return;
        ApplyChangesAtEnd();
        fired = true;
    }

    void TakeSnapshot()
    {
        snapshotHadTarget = target;
        if (snapshotHadTarget) snapshotWasActive = target.activeSelf;

        snapshotHadRenderer = targetRenderer;
        if (snapshotHadRenderer)
        {
            snapshotSingleMaterial = targetRenderer.sharedMaterial;
            snapshotMaterialsArray = targetRenderer.sharedMaterials;
        }

        snapshotHadLight = targetLight;
        if (snapshotHadLight)
        {
            snapshotLight = new LightState
            {
                c = targetLight.color,
                i = targetLight.intensity,
                r = targetLight.range,
                e = targetLight.enabled,
                a = targetLight.gameObject.activeSelf
            };
        }
    }

    void ApplyChangesAtEnd()
    {
        if (targetRenderer)
        {
            if (materialOnEnd)
            {
                if (targetRenderer.sharedMaterials != null && targetRenderer.sharedMaterials.Length > 1)
                {
                    var mats = targetRenderer.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++) mats[i] = materialOnEnd;
                    targetRenderer.sharedMaterials = mats;
                }
                else
                {
                    targetRenderer.sharedMaterial = materialOnEnd;
                }
                targetRenderer.SetPropertyBlock(null);
            }
            else
            {
                var r = targetRenderer;
                r.GetPropertyBlock(mpb);
                if (r.sharedMaterial && r.sharedMaterial.HasProperty(PID_BaseColor)) mpb.SetColor(PID_BaseColor, baseColorOnEnd);
                else if (r.sharedMaterial && r.sharedMaterial.HasProperty(PID_Color)) mpb.SetColor(PID_Color, baseColorOnEnd);
                if (r.sharedMaterial && r.sharedMaterial.HasProperty(PID_EmissionColor)) mpb.SetColor(PID_EmissionColor, emissionColorOnEnd);
                r.SetPropertyBlock(mpb);
            }
        }

        if (targetLight)
        {
            targetLight.color = lightColorOnEnd;
            targetLight.intensity = lightIntensityOnEnd;
            targetLight.range = lightRangeOnEnd;
            targetLight.enabled = true;
        }
    }

    IEnumerator ShowAfterDelay()
    {
        float t = 0f;
        float d = Mathf.Max(0f, appearDelay);
        while (t < d)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }
        if (target) target.SetActive(true);
    }
}
